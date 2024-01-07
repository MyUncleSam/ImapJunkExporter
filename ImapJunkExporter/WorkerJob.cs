using ImapJunkExporter.Config;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ImapJunkExporter
{
    internal class WorkerJob : IWorkerJob
    {
        private IEnumerable<MailboxesOption> Mailboxes { get; init; }

        private ILogger<WorkerJob> Log { get; init; }

        private WorkerOption Config { get; init; }

        public WorkerJob(ILogger<WorkerJob> log, IEnumerable<MailboxesOption> mailboxes, WorkerOption config)
        {
            this.Mailboxes = mailboxes;
            this.Log = log;
            this.Config = config;
        }


        public async Task Run()
        {
            foreach (var mailbox in Mailboxes)
            {
                Log.LogInformation("Processing {Mailbox}", mailbox.ImapUsername);

                try
                {
                    using var client = new ImapClient();

                    await client.ConnectAsync(mailbox.ImapHost, mailbox.ImapPort, mailbox.ImapUseSsl);
                    await client.AuthenticateAsync(mailbox.ImapUsername, System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(mailbox.ImapPassword)));
                    await client.Inbox.OpenAsync(FolderAccess.ReadOnly);

                    var junkFolder = client.GetFolder(SpecialFolder.Junk);
                    IMailFolder learnedFolder;

                    if (!(await junkFolder.GetSubfoldersAsync()).Any(a => a.Name == mailbox.ImapLearnedFolderName))
                    {
                        Log.LogInformation("Junk subfolder for learned mails missing, creating subfolder '{LearnedFolderName}' and subscribe to it.", mailbox.ImapLearnedFolderName);
                        learnedFolder = await junkFolder.CreateAsync(mailbox.ImapLearnedFolderName, true);
                        await learnedFolder.SubscribeAsync();
                    }
                    else
                    {
                        learnedFolder = await junkFolder.GetSubfolderAsync(mailbox.ImapLearnedFolderName);
                    }

                    await learnedFolder.OpenAsync(FolderAccess.ReadWrite);
                    await junkFolder.OpenAsync(FolderAccess.ReadWrite);

                    foreach (var msgMeta in await junkFolder.FetchAsync(0, -1, MessageSummaryItems.UniqueId))
                    {
                        var fileName = $"{mailbox.TargetFilenamePrefix}{msgMeta.UniqueId}.eml";
                        var exportFile = Path.Combine(mailbox.TargetLocalFolder, fileName);

                        if (File.Exists(exportFile))
                        {
                            Log.LogInformation("\tfile already exist - skipping file ({fileName})", exportFile);
                            await junkFolder.MoveToAsync(msgMeta.UniqueId, learnedFolder);
                            continue;
                        }

                        var msg = await junkFolder.GetMessageAsync(msgMeta.UniqueId);

                        if (mailbox.IgnoreSpamMessages && IsSpamMessage(msg))
                        {
                            Log.LogInformation("Message already flagged as spam - skipping : {UniqueId}", msgMeta.UniqueId);
                            continue;
                        }

                        if (Config.ProtocolEmlBaseInformation)
                        {
                            Log.LogInformation("\texported {UniqueId}: {Subject}", msgMeta.UniqueId, msg.Subject);
                        }

                        await msg.WriteToAsync(exportFile);
                        await junkFolder.MoveToAsync(msgMeta.UniqueId, learnedFolder);
                    }

                    await junkFolder.CloseAsync();
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, "unable to process {Mailbox}", mailbox.ImapUsername);
                }
                finally
                {
                    Log.LogInformation("Done processing {Mailbox}", mailbox.ImapUsername);
                }
            }
        }

        private static bool IsSpamMessage(MimeMessage msg)
        {
            // get all values of known spam flags
            var spamFlags = new List<string>()
            {
                msg
                    .Headers
                    .FirstOrDefault(fd => fd.Field.Equals("X-Spam-Flag", StringComparison.OrdinalIgnoreCase))
                    ?.Value ?? string.Empty,
                msg
                    .Headers
                    .FirstOrDefault(fd => fd.Field.Equals("X-Spam", StringComparison.OrdinalIgnoreCase))
                    ?.Value ?? string.Empty
            };

            var spamFlagsWithValues = spamFlags
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .ToList();

            if(spamFlagsWithValues.Count == 0)
            {
                return false;
            }

            return spamFlagsWithValues
                .All(a => SpamFlagToBool(a));
        }

        private static bool SpamFlagToBool(string text)
        {
            List<string> trueValues =
            [
                "yes", "true", "1"
            ];

            return trueValues.Contains(text.ToLower());
        }
    }
}
