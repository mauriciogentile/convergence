using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
            var distrLastDays = int.Parse(ConfigurationManager.AppSettings["DISTRIBUTION_LAST_DAYS"]);
            var distrTop = int.Parse(ConfigurationManager.AppSettings["DISTRIBUTION_MAX_RESULTS"]);
            var ditrSleep = int.Parse(ConfigurationManager.AppSettings["DISTRIBUTION_SLEEP_IN_MIN"]);
            var distrValidStatesActions = GetStatesActions(ConfigurationManager.AppSettings["DISTRIBUTION_VALID_STATES_ACTIONS"]);

            var agendaLastDays = int.Parse(ConfigurationManager.AppSettings["AGENDA_LAST_DAYS"]);
            var agendaTop = int.Parse(ConfigurationManager.AppSettings["AGENDA_MAX_RESULTS"]);
            var agendaSleep = int.Parse(ConfigurationManager.AppSettings["AGENDA_SLEEP_IN_MIN"]);
            var agendaValidStatesActions = GetStatesActions(ConfigurationManager.AppSettings["AGENDA_VALID_STATES_ACTIONS"]);

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
                LastDays = distrLastDays,
                SleepPeriod = TimeSpan.FromMinutes(ditrSleep),
                MaxResults = distrTop,
                ValidStateActions = distrValidStatesActions
            };

            var agendaMonitor = new AgendaMonitor(connString, docStorage, factory, logger)
            {
                LastDays = agendaLastDays,
                SleepPeriod = TimeSpan.FromMinutes(agendaSleep),
                MaxResults = agendaTop,
                ValidStateActions = agendaValidStatesActions
            };

            return new List<IWorker>(new IWorker[] { distrMonitor, agendaMonitor });
        }

        static Dictionary<string, string> GetStatesActions(string config)
        {
            var a = config.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return a.Select(x => x.Split(new[] { '|' })).ToDictionary(x => x[0], y => y[1]);
        }
    }
}
