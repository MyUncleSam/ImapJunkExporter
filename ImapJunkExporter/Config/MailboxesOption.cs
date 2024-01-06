using System.ComponentModel.DataAnnotations;

namespace ImapJunkExporter.Config
{
    public class MailboxesOption
    {
        [Required]
        public string ImapHost { get; init; }

        [Required]
        public int ImapPort { get; init; }

        [Required]
        public bool ImapUseSsl { get; init; }

        [Required]
        public string ImapUsername { get; init; }

        [Required]
        public string ImapPassword { get; init; }

        [Required]
        public string TargetLocalFolder { get; init; }

        [Required]
        public bool IgnoreSpamMessages { get; set; }

        [Required]
        public string TargetFilenamePrefix { get; set; }
    }
}
