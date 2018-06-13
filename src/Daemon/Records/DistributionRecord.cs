using System;

namespace Idb.Sec.Convergence.Daemon.Records
{
    public class DistributionRecord : DocumentRecord
    {
        public string Version { get; set; }
        public string Procedure { get; set; }
        public string Description { get; set; }
        public DateTime DistributedOn { get; set; }
    }
}