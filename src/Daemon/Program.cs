using System;
using System.Collections.Generic;
using System.Configuration;
using Idb.CommonServices.Util.Diagnostic;
using Idb.CommonServices.Util.Tasks;
using Idb.Sec.Convergence.Daemon.Workers;
using Sec.Wfe.RestClient;
using Topshelf;

namespace Idb.Sec.Convergence.Daemon
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new Logger();

            HostFactory.Run(x =>
            {
                x.Service<ContainerWorker>(s =>
                {
                    s.ConstructUsing(name => new ContainerWorker(GetWorkers(logger), logger));
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                    s.WhenContinued(tc => tc.Start());
                    s.WhenPaused(tc => tc.Stop());
                });

                x.SetDescription("Convergence Daemon Service monitors distributed records and updates workflows accordingly");
                x.SetDisplayName("Convergence Daemon Service");
                x.SetServiceName("sec_convergence_daemon");
                x.RunAsLocalSystem();
            });
        }

        static IEnumerable<IWorker> GetWorkers(ILogger logger)
        {
            var ezShareAccessCode = ConfigurationManager.AppSettings["EzShareAccessCode"];
            var lastDays = int.Parse(ConfigurationManager.AppSettings["DISTRIBUTION_LAST_DAYS"]);
            var top = int.Parse(ConfigurationManager.AppSettings["DISTRIBUTION_MAX_RESULTS"]);
            var ditributionSleep = int.Parse(ConfigurationManager.AppSettings["DISTRIBUTION_SLEEP_IN_MIN"]);
            var state1 = ConfigurationManager.AppSettings["DISTRIBUTION_INITIAL_STATE"];
            var action1 = ConfigurationManager.AppSettings["DISTRIBUTION_INITIAL_ACTION"];
            var state2 = ConfigurationManager.AppSettings["DISTRIBUTION_LAST_STATE"];
            var action2 = ConfigurationManager.AppSettings["DISTRIBUTION_LAST_ACTION"];
            var connString = ConfigurationManager.ConnectionStrings["Agenda"].ConnectionString;
            var wfeApiUrl = ConfigurationManager.ConnectionStrings["WfeApiUrl"].ConnectionString;
            var clientId = ConfigurationManager.AppSettings["WFE.ApiClientId"];
            var clientSecret = ConfigurationManager.AppSettings["WFE.ApiClientSecret"];
            var username = ConfigurationManager.AppSettings["WFE.ApiUsername"];
            var password = ConfigurationManager.AppSettings["WFE.ApiPassword"];

            var factory = new Func<IWfeClient>(() => new Client(wfeApiUrl).Login(clientId, clientSecret, username, password));
            var docStorage = new DocumentStorage(ezShareAccessCode);

            var distrMonitor = new DistributionMonitor(connString, docStorage, factory, logger)
            {
                LastDays = lastDays,
                SleepPeriod = TimeSpan.FromMinutes(ditributionSleep),
                MaxResults = top,
                InitialDistrAction = action1,
                InitialDistrState = state1,
                LastDistrAction = action2,
                LastDistrState = state2
            };

            var agendaMonitor = new AgendaMonitor(connString, docStorage, factory, logger)
            {
                LastDays = lastDays,
                SleepPeriod = TimeSpan.FromMinutes(ditributionSleep),
                MaxResults = top,
                InitialDistrAction = action1,
                InitialDistrState = state1,
                LastDistrAction = action2,
                LastDistrState = state2
            };

            return new List<IWorker>(new IWorker[] { distrMonitor, agendaMonitor });
        }
    }
}
