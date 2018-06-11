﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Idb.CommonServices.Util.Diagnostic;
using Idb.CommonServices.Util.Tasks;
using Sec.Wfe.RestClient;

namespace Idb.Sec.Convergence.Daemon
{
    public class DistributionMonitorWorker : Worker
    {
        private readonly string _connString;
        private readonly Func<IWfeClient> _clientFactory;

        public int LastDays { get; set; }
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
                p.Add("@MIN_DATE", DateTime.Now.AddDays(LastDays * -1), DbType.DateTime);
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
                        Logger.Debug(string.Format("Processing pipeline '{0}' for committee '{1}' on workflow '{2}'", x.Pipeline, x.CommitteeId, x.WorkflowId));
                        var instance = await client.GetWorkflowInstanceAsync(x.WorkflowId);
                        currentState = instance.CurrentState;
                        if (currentState != InitialDistrState && currentState != LastDistrState)
                        {
                            Logger.Debug(string.Format("Skipping pipeline '{0}' for committee '{1}' on workflow '{2}'", x.Pipeline, x.CommitteeId, x.WorkflowId));
                            continue;
                        }
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
            var newEntry = new Dictionary<string, object>
            {
                { "distributedOn", record.DistributedOn },
                { "committeeId", record.CommitteeId },
                { "version", record.Version },
                { "versionId", record.VersionId },
                { "procedure", record.Procedure }
            };

            var distributions = cd["distributions"] as IList<object>;
            if (distributions == null || !Helpers.DoesPropertyExist(cd, "distributions"))
            {
                cd["distributions"] = new List<object> { newEntry };
                return true;
            }

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
                return false;
            }

            distributions.Add(newEntry);

            return true;
        }

        static string GetId(IDictionary<string, object> dict)
        {
            return string.Format("{0}_{1}_{2}_{3}", dict["version"], dict["versionId"], dict["procedure"], dict["committeeId"]);
        }
    }
}