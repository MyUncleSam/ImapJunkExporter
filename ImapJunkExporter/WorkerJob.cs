using ImapJunkExporter.Config;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ImapJunkExporter
{
    internal class WorkerJob : IWorkerJob
    {
        private IEnumerable<MailboxesOption> mailboxes { get; init; }

        private ILogger<WorkerJob> log { get; init; }

        private WorkerOption config { get; init; }

        public WorkerJob(ILogger<WorkerJob> log, IEnumerable<MailboxesOption> mailboxes, WorkerOption config)
        {
            this.mailboxes = mailboxes;
            this.log = log;
            this.config = config;
        }


        public void Run()
        {
            foreach (var mailbox in mailboxes)
            {
                log.LogInformation("Processing {Mailbox}", mailbox.ImapUsername);

                try
                {
                    using (var client = new ImapClient())
                    {
                        client.Connect(mailbox.ImapHost, mailbox.ImapPort, mailbox.ImapUseSsl);
                        client.Authenticate(mailbox.ImapUsername, System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(mailbox.ImapPassword)));
                        client.Inbox.Open(FolderAccess.ReadOnly);

                        var junkFolder = client.GetFolder(SpecialFolder.Junk);
                        junkFolder.Open(FolderAccess.ReadOnly);

                        foreach (var msgMeta in junkFolder.Fetch(0, -1, MessageSummaryItems.UniqueId))
                        {
                            var fileName = $"{mailbox.TargetFilenamePrefix}{msgMeta.UniqueId}.eml";
                            var exportFile = Path.Combine(mailbox.TargetLocalFolder, fileName);

                            if (File.Exists(exportFile))
                            {
                                log.LogInformation("\tfile already exist - skipping file ({fileName})", exportFile);
                                continue;
                            }

                            var msg = junkFolder.GetMessage(msgMeta.UniqueId);

                            if(mailbox.IgnoreSpamMessages && IsSpamMessage(msg))
                            {
                                log.LogInformation("Message already flagged as spam - skipping : {UniqueId}", msgMeta.UniqueId);
                                continue;
                            }

                            if (config.ProtocolEmlBaseInformation)
                            {
                                log.LogInformation("\texported {UniqueId}: {Subject}", msgMeta.UniqueId, msg.Subject);
                            }

                            msg.WriteTo(exportFile);
                        }

                        junkFolder.Close();
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "unable to process {Mailbox}", mailbox.ImapUsername);
                }
                finally
                {
                    log.LogInformation("Done processing {Mailbox}", mailbox.ImapUsername);
                }
            }
        }

        private bool IsSpamMessage(MimeMessage msg)
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

            if(!spamFlagsWithValues.Any())
            {
                return false;
            }

            return spamFlagsWithValues
                .All(a => SpamFlagToBool(a));
        }

        private bool SpamFlagToBool(string text)
        {
            List<string> trueValues = new List<string>()
            {
                "yes", "true", "1"
            };

            return trueValues.Contains(text.ToLower());
        }
    }
}
