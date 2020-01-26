using System;
using System.Configuration;

namespace harambe_trader
{
    public class Program
    {
        public static void Main(string[] args) {
            ExpandAllEnvironmentVariables();

            var broker = new Services.BrokerService(
                ConfigurationManager.AppSettings["brokerendpoint"],
                ConfigurationManager.AppSettings["brokerapikeyid"],
                ConfigurationManager.AppSettings["brokerapikeysecret"]
            );

            var datasource = new Services.DatasourceService(
                ConfigurationManager.AppSettings["datasourceendpoint"],
                ConfigurationManager.AppSettings["datasourcetoken"]
            );

            var pgClient = new Services.PostgresDatabaseClient(
                ConfigurationManager.AppSettings["databaseconnectionstring"]
            );

            var parser = new Strategy.Parser(
                new Services.Database(pgClient)
            );

            parser.AddSelector(
                "n_highest_prediction", new Strategy.HighestPrediction(
                    new Services.PredictorService(ConfigurationManager.AppSettings["predictorendpoint"]),
                    datasource
                )
            );

            parser.AddDistributor(
                "equal", new Strategy.EqualDistributor(
                    broker, datasource
                )
            );

            var processStrategyFileTask = parser.ProcessStrategyFile("strategy.yml");
            processStrategyFileTask.Wait();
        }

        public static void ExpandAllEnvironmentVariables() {
            foreach (var key in ConfigurationManager.AppSettings.AllKeys) {
                var expanded = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings[key]);
                ConfigurationManager.AppSettings[key] = expanded;
            }
        }
    }
}
