using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using harambe_trader.Services;
using Endpoints.Harambe6;

using Newtonsoft.Json;

namespace harambe_trader.Strategy {
    public class HighestPrediction : ISelector {

        private PredictorService _predictorService;
        private DatasourceService _datasourceService;

        public HighestPrediction(
            PredictorService predictorService,
            DatasourceService datasourceService
        ) {
            // _config = JsonConvert.DeserializeObject<HighestPredictionConfig>(config);
            _predictorService = predictorService;
            _datasourceService = datasourceService;
        }

        public Type GetConfigType() {
            return typeof(HighestPredictionConfig);
        }

        public async Task<List<string>> GetSymbols(dynamic config) {
            var highestPredictionConfig = config as HighestPredictionConfig;
            var symbolDifferences = new List<(string, double)>();

            foreach (var symbol in highestPredictionConfig.pool.symbols) {
                var req = new PredictionRequest{
                    Symbol = symbol,
                    TrendLength = highestPredictionConfig.trend_length
                };

                try {
                    var res = _predictorService.Predict(req);
                    var quote = await _datasourceService.GetQuote(symbol);

                    Console.WriteLine($"{symbol}: {res.ValDenorm} vs {quote.latestPrice}");

                    symbolDifferences.Add((symbol, (res.ValDenorm - quote.latestPrice) / quote.latestPrice));
                } catch (Exception e) {
                    Console.WriteLine($"Error getting symbol data: {e}");
                }
            }

            symbolDifferences.Sort((r1, r2) => r1.Item2.CompareTo(r2.Item2));

            // Best performing at start
            return symbolDifferences
                .Where(
                    diff => 
                        diff.Item2 >= highestPredictionConfig.filters.percent_change_floor &&
                        diff.Item2 <= highestPredictionConfig.filters.percent_change_ceiling
                )
                .Select((r) => r.Item1)
                .Reverse()
                .ToList();
        }
    }

    public class HighestPredictionConfig {
        public Pool pool { get; set; }
        public int trend_length { get; set; }
        public Filters filters { get; set; }

        public class Filters {
            public double percent_change_floor { get; set; } = 0;
            public double percent_change_ceiling { get; set; } = Double.PositiveInfinity;
        }
    }
}
