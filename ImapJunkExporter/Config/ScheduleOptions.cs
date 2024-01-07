using System.ComponentModel.DataAnnotations;

namespace ImapJunkExporter.Config
{
    public record ScheduleOptions
    {
        [Required]
        public required string Cron { get; init; } = "0 0/15 * ? * * *";

        [Required]
        public bool RunOnce { get; init; } = false;
    }
}
