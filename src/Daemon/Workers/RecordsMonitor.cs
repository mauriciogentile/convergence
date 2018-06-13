using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Idb.CommonServices.Util.Diagnostic;
using Idb.CommonServices.Util.Tasks;
using Idb.Sec.Convergence.Daemon.Records;
using Sec.Wfe.RestClient;

namespace Idb.Sec.Convergence.Daemon.Workers
{
    public abstract class RecordsMonitor<T> : Worker where T : DocumentRecord
    {
        private readonly IDocumentStorage _documentStorage;
        private readonly Func<IWfeClient> _clientFactory;

        public int LastDays { get; set; }
        public int MaxResults { get; set; }
        public string InitialDistrState { get; set; }
        public string InitialDistrAction { get; set; }
        public string LastDistrState { get; set; }
        public string LastDistrAction { get; set; }

        protected RecordsMonitor(IDocumentStorage documentStorage, Func<IWfeClient> clientFactory,
            ILogger logger)
            : base(logger)
        {
            _documentStorage = documentStorage;
            _clientFactory = clientFactory;
        }

        protected override async Task DoWork()
        {
            var records = await GetRecords();
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
                    x.Documents = await _documentStorage.SearchByCodeAsync(x.DocId);
                    if (!TryAddInfo(x, cd)) continue;
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

        protected abstract Task<IEnumerable<T>> GetRecords();
        protected abstract bool TryAddInfo(T record, dynamic cd);
    }
}