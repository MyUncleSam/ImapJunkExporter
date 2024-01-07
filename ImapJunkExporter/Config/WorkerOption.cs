using System.ComponentModel.DataAnnotations;

namespace ImapJunkExporter.Config
{
    public record WorkerOption
    {
        [Required]
        public bool ProtocolEmlBaseInformation { get; init; } = true;
    }
}
