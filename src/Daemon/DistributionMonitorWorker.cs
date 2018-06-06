using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Idb.CommonServices.Util.Diagnostic;
using Idb.CommonServices.Util.Tasks;
using Idb.Wfe.RestClient;

namespace Idb.Sec.Convergence.Daemon
{
    public class DistributionMonitorWorker : Worker
    {
        private readonly string _connString;
        private readonly Func<IWfeClient> _clientFactory;

        public DateTime MinDateToMonitor { get; set; }
        public int MaxResults { get; set; }
        public string InitialDistrState { get; set; }
        public string InitialDistrAction { get; set; }
        public string LastDistrState { get; set; }
        public string LastDistrAction { get; set; }

        public DistributionMonitorWorker(string connString, Func<IWfeClient> clientFactory, ILogger logger)
            : base(logger)
        {
            _connString = connString;
            _clientFactory = clientFactory;
        }

        protected override async Task DoWork()
        {
            using (var connection = new SqlConnection(_connString))
            {
                var p = new DynamicParameters();
                p.Add("@MIN_DATE", MinDateToMonitor, DbType.DateTime);
                p.Add("@TOP", MaxResults, DbType.Int32);

                var command = new CommandDefinition("[DocumentDistribution_Read]", p, null, null, CommandType.StoredProcedure);
                var records = await connection.QueryAsync<DistributionRecord>(command);
                var client = _clientFactory();
                foreach (var x in records)
                {
                    string action = null;
                    string currentState = null;
                    try
                    {
                        var instance = await client.GetWorkflowInstanceAsync(x.WorkflowId);
                        currentState = instance.CurrentState;
                        if (currentState != InitialDistrState && currentState != LastDistrState) continue;
                        var cd = await client.GetCustomDataAsync(x.WorkflowId);
                        if (!TryAddDistribution(x, cd)) continue;
                        await client.SetCustomDataAsync(x.WorkflowId, cd);
                        action = instance.CurrentState == InitialDistrState ? InitialDistrAction : LastDistrAction;
                        await client.ExecuteActionAsync(x.WorkflowId, action, instance.CurrentState);
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(string.Format("Error executing action '{0}/{1}' on instance '{2}'", currentState, action, x.InstanceId), exception);
                    }
                }
            }
        }

        static bool TryAddDistribution(DistributionRecord record, dynamic cd)
        {
            var newEntry = new
            {
                distributedOn = record.DistributedOn,
                committeeId = record.CommitteeId,
                version = record.Version,
                versionId = record.VersionId,
                procedure = record.Procedure
            };
            if (!Helpers.DoesPropertyExist(cd, "distributions"))
            {
                cd["distributions"] = new[] { newEntry };
                return true;
            }

            var list = cd["distributions"] as IList<object>;
            if (list != null)
            {
                var discard = false;
                list.ToList().ForEach(x =>
                {
                    var dict = x as Dictionary<string, object>;
                    var exists = dict != null &&
                        Equals(dict["version"], newEntry.version) &&
                        Equals(dict["versionId"], newEntry.versionId) &&
                        Equals(dict["procedure"], newEntry.procedure) &&
                        Equals(dict["committeeId"], newEntry.committeeId);

                    if (exists)
                    {
                        discard = true;
                    }
                });
                if (discard)
                {
                    return false;
                }
                list.Add(newEntry);
            }
            else
            {
                cd["distributions"] = new[] { newEntry };
            }
            return true;
        }
    }
}