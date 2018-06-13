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
    public class DistributionMonitor : RecordsMonitor<DistributionRecord>
    {
        private readonly string _connString;

        public DistributionMonitor(string connString, IDocumentStorage documentStorage, Func<IWfeClient> clientFactory, ILogger logger)
            : base(documentStorage, clientFactory, logger)
        {
            _connString = connString;
        }

        protected async override Task<IEnumerable<DistributionRecord>> GetRecords()
        {
            using (var connection = new SqlConnection(_connString))
            {
                var p = new DynamicParameters();
                p.Add("@MIN_DATE", DateTime.Now.AddDays(LastDays * -1), DbType.DateTime);
                p.Add("@TOP", MaxResults, DbType.Int32);

                var command = new CommandDefinition("[DocumentDistribution_Read]", p, null, null,
                    CommandType.StoredProcedure);
                return await connection.QueryAsync<DistributionRecord>(command);
            }
        }

        protected override bool TryAddInfo(DistributionRecord record, dynamic cd)
        {
            var distributions = (cd["distributions"] as IList<object>) ?? new List<object>();
            var count = distributions.Count;
            foreach (var doc in record.Documents)
            {
                var newEntry = new Dictionary<string, object>
                {
                    {"distributedOn", record.DistributedOn},
                    {"committeeId", record.CommitteeId},
                    {"version", record.Version},
                    {"docId", doc.Id},
                    {"lang", doc.Language},
                    {"url", doc.Url},
                    {"procedure", record.Procedure},
                    {"description", record.Description}
                };

                var discard = false;
                distributions.ToList().ForEach(x =>
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

                distributions.Add(newEntry);
            }

            return count != distributions.Count;
        }

        static string GetId(IDictionary<string, object> dict)
        {
            object version;
            object docId;
            object procedure;
            object committee;
            object lang;

            dict.TryGetValue("version", out version);
            dict.TryGetValue("docId", out docId);
            dict.TryGetValue("procedure", out procedure);
            dict.TryGetValue("committeeId", out committee);
            dict.TryGetValue("lang", out lang);

            return string.Format("{0}_{1}_{2}_{3}_{4}", version, docId, procedure, committee, lang);
        }
    }
}