using System;

namespace Idb.Sec.Convergence.Daemon
{
    public class DistributionRecord
    {
        public string Version { get; set; }
        public string VersionId { get; set; }
        public string Pipeline { get; set; }
        public string Committee { get; set; }
        public int CommitteeId { get; set; }
        public string WorkflowId { get; set; }
        public string InstanceId { get; set; }
        public string Procedure { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime DistributedOn { get; set; }
    }
}