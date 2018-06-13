using System.Collections.Generic;

namespace Idb.Sec.Convergence.Daemon.Records
{
    public abstract class DocumentRecord
    {
        public string CommitteeId { get; set; }
        public string Pipeline { get; set; }
        public string DocId { get; set; }
        public string InstanceId { get; set; }
        public string WorkflowId { get; set; }
        public IEnumerable<Document> Documents { get; set; }
    }
}