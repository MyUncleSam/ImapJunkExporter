using System.ComponentModel.DataAnnotations;

namespace ImapJunkExporter.Config
{
    public record CronOptions
    {
        [Required]
        public required string Schedule { get; init; }
    }
}
