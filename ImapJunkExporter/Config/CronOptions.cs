using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImapJunkExporter.Config
{
    public record CronOptions
    {
        public bool RunOnce { get; set; }

        public string Schedule { get; set; }
    }
}
