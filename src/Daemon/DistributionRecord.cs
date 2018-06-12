using System;

namespace Idb.Sec.Convergence.Daemon
{
    public class DistributionRecord
    {
        public string DocId { get; set; }
        public string Version { get; set; }
        public string Pipeline { get; set; }
        public int CommitteeId { get; set; }
        public string WorkflowId { get; set; }
        public string InstanceId { get; set; }
        public string Procedure { get; set; }
        public string Description { get; set; }
        public DateTime DistributedOn { get; set; }
    }
}