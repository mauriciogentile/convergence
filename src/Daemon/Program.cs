using System;
using System.Collections.Generic;
using System.Configuration;
using Idb.CommonServices.Util.Diagnostic;
using Idb.CommonServices.Util.Tasks;
using Idb.Wfe.RestClient;
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
            var minDate = DateTime.Parse(ConfigurationManager.AppSettings["DISTRIBUTION_MIN_DATE"]);
            var top = int.Parse(ConfigurationManager.AppSettings["DISTRIBUTION_MAX_RESULTS"]);
            var ditributionSleep = int.Parse(ConfigurationManager.AppSettings["DISTRIBUTION_SLEEP_IN_MIN"]);
            var connString = ConfigurationManager.ConnectionStrings["Agenda"].ConnectionString;
            var wfeApiUrl = ConfigurationManager.ConnectionStrings["WfeApiUrl"].ConnectionString;
            var username = ConfigurationManager.AppSettings["WfeApiUsername"];
            var password = ConfigurationManager.AppSettings["WfeApiPassword"];

            var factory = new Func<IWfeClient>(() =>
            {
                var client = new Client(wfeApiUrl);
                client.Login(username, password);
                return client;
            });

            var monitorWorker = new DistributionMonitorWorker(minDate, top, connString,
                TimeSpan.FromMinutes(ditributionSleep), factory, logger);

            return new List<IWorker>(new IWorker[] { monitorWorker });
        }
    }
}
