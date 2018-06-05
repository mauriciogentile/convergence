using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Idb.CommonServices.Util.Diagnostic;
using Idb.CommonServices.Util.Tasks;
using Idb.Wfe.RestClient;

namespace Idb.Sec.Convergence.Daemon
{
    public class DistributionMonitorWorker : Worker
    {
        private readonly DateTime _minDate;
        private readonly int _top;
        private readonly string _connString;
        private readonly Func<IWfeClient> _clientFactory;

        public DistributionMonitorWorker(DateTime minDate, int top, string connString, TimeSpan sleepPeriod,
            Func<IWfeClient> clientFactory, ILogger logger)
            : base(logger)
        {
            SleepPeriod = sleepPeriod;
            _minDate = minDate;
            _top = top;
            _connString = connString;
            _clientFactory = clientFactory;
        }

        protected override async Task DoWork()
        {
            using (var connection = new SqlConnection(_connString))
            {
                var p = new DynamicParameters();
                p.Add("@MIN_DATE", _minDate, DbType.DateTime);
                p.Add("@TOP", _top, DbType.Int32);

                var command = new CommandDefinition("[DocumentDistribution_Read]", p, null, null, CommandType.StoredProcedure);
                var records = await connection.QueryAsync<DistributionRecord>(command);
                var client = _clientFactory();
                foreach (var x in records)
                {
                    const string action = "Submit for translation";
                    const string state = "Created";
                    try
                    {
                        await client.ExecuteActionAsync(x.WorkflowId, action, state);
                        var cd = await client.GetCustomDataAsync(x.WorkflowId);
                        var newEntry = new
                        {
                            distributedOn = x.DistributedOn,
                            committeeId = x.CommitteeId,
                            pipeline = x.Pipeline,
                            version = x.Version,
                            versionId = x.VersionId,
                            procedure = x.Procedure
                        };
                        if (Helpers.DoesPropertyExist(cd, "distributions"))
                        {
                            //cd.distribution;
                            var list = cd["distributions"] as IList;
                            if (list != null)
                                list.Add(newEntry);
                        }
                        else
                        {
                            cd["distributions"] = new[] { newEntry };
                        }
                        await client.SetCustomDataAsync(x.WorkflowId, cd);
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(string.Format("Error executing action '{0}/{1}' on instance '{2}'", state, action, x.InstanceId), exception);
                    }
                }
            }
        }
    }
}