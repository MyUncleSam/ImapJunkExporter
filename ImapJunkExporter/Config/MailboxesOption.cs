using System.ComponentModel.DataAnnotations;

namespace ImapJunkExporter.Config
{
    public class MailboxesOption
    {
        [Required]
        public required string ImapHost { get; init; }

        [Required]
        public int ImapPort { get; init; }

        [Required]
        public bool ImapUseSsl { get; init; }

        [Required]
        public required string ImapUsername { get; init; }

        [Required]
        public required string ImapPassword { get; init; }

        [Required]
        public required string ImapLearnedFolderName { get; set; }

        [Required]
        public required string TargetLocalFolder { get; init; }

        [Required]
        public bool IgnoreSpamMessages { get; init; }

        [Required]
        public required string TargetFilenamePrefix { get; init; }
    }
}
