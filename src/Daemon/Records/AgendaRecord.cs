using System;

namespace Idb.Sec.Convergence.Daemon.Records
{
    public class AgendaRecord : DocumentRecord
    {
        public DateTime DistributionDate { get; set; }
        public DateTime CirculationStartDate { get; set; }
        public DateTime CirculationEndDate { get; set; }
        public DateTime MeetingDateTime { get; set; }
    }
}