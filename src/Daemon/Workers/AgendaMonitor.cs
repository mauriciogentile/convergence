using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Idb.CommonServices.Util.Diagnostic;
using Idb.Sec.Convergence.Daemon.Records;
using Sec.Wfe.RestClient;

namespace Idb.Sec.Convergence.Daemon.Workers
{
    public class AgendaMonitor : RecordsMonitor<AgendaRecord>
    {
        private readonly string _connString;

        public AgendaMonitor(string connString, IDocumentStorage documentStorage, Func<IWfeClient> clientFactory, ILogger logger)
            : base(documentStorage, clientFactory, logger)
        {
            _connString = connString;
        }

        protected async override Task<IEnumerable<AgendaRecord>> GetRecords()
        {
            using (var connection = new SqlConnection(_connString))
            {
                var p = new DynamicParameters();
                p.Add("@MIN_DATE", DateTime.Now.AddDays(LastDays * -1), DbType.DateTime);
                p.Add("@TOP", MaxResults, DbType.Int32);

                var command = new CommandDefinition("[DocumentMeetings_Read]", p, null, null,
                    CommandType.StoredProcedure);
                return await connection.QueryAsync<AgendaRecord>(command);
            }
        }

        protected override bool TryAddInfo(AgendaRecord record, dynamic cd)
        {
            var meetings = (cd["meetings"] as IList<object>) ?? new List<object>();
            var count = meetings.Count;
            foreach (var doc in record.Documents)
            {
                var newEntry = new Dictionary<string, object>
                {
                    {"circulationStartDate", record.CirculationStartDate},
                    {"circulationEndDate", record.CirculationEndDate},
                    {"pipeline", record.Pipeline},
                    {"distributionDate", record.DistributionDate},
                    {"meetingDateTime", record.MeetingDateTime},
                    {"docId", doc.Id},
                    {"lang", doc.Language},
                    {"url", doc.Url}
                };

                var discard = false;
                meetings.ToList().ForEach(x =>
                {
                    var dict = x as Dictionary<string, object>;
                    if (dict == null) return;
                    var id1 = GetId(dict);
                    var id2 = GetId(newEntry);
                    if (id1 == id2)
                    {
                        discard = true;
                    }
                });

                if (discard)
                {
                    continue;
                }

                meetings.Add(newEntry);
            }

            return count != meetings.Count;
        }

        static string GetId(IDictionary<string, object> dict)
        {
            object meetingDateTime;
            object docId;

            dict.TryGetValue("meetingDateTime", out meetingDateTime);
            dict.TryGetValue("docId", out docId);

            return string.Format("{0}_{1}", meetingDateTime, docId);
        }
    }
}