using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using harambe_trader.Services;

namespace harambe_trader.Strategy {

    public class EqualDistributor : IDistributor {
        private BrokerService _broker;
        private DatasourceService _datasource;

        public EqualDistributor(BrokerService broker, DatasourceService datasource) {
            _broker = broker;
            _datasource = datasource;
        }

        public Type GetConfigType() {
            return typeof(EqualDistributorConfig);
        }

        public async Task ReconcilePool(dynamic config) {
            if (!(await _broker.IsMarketOpen())) {
                Console.WriteLine("Market is not open, not trading");
                return;
            }

            var equalDistributorConfig = config as EqualDistributorConfig;

            var buyingPower = await _broker.GetCash();
            var combinedSymbols = new HashSet<string>();
            var currentPositions = new Dictionary<string, Position>();

            combinedSymbols.UnionWith(equalDistributorConfig.pool.symbols);

            var positons = await _broker.GetPositions();

            // buyingpower: cash + total long positions
            // buyingpower (in short): cash + total short positions
            positons
                .Where(p => p.side == equalDistributorConfig.side)
                .ToList()
                .ForEach(p => {
                    buyingPower += Math.Abs(p.market_value);
                    combinedSymbols.Add(p.symbol);
                    currentPositions.Add(p.symbol, p);
                });

            var targetSymbolValue = buyingPower / equalDistributorConfig.pool.symbols.Count;

            Console.WriteLine($"buying power: {buyingPower}");

            foreach (var symbol in combinedSymbols) {
                Console.WriteLine($"combined symbol: {symbol}");
            }

            foreach (var symbol in equalDistributorConfig.pool.symbols) {
                Console.WriteLine($"pool symbol: {symbol}");
            }

            // TODO: just return symbols and shares?
            // Make portfolio match pool
            foreach (var symbol in combinedSymbols) {
                if (equalDistributorConfig.pool.symbols.Contains(symbol)) {
                    var quote = await _datasource.GetQuote(symbol);
                    var shares = Math.Floor(targetSymbolValue / quote.latestPrice);

                    if (currentPositions.ContainsKey(symbol) && equalDistributorConfig.pool.symbols.Contains(symbol)) {
                        // Has symbol but may need to adjust
                        var diff = shares - currentPositions[symbol].qty;

                        if (diff != 0) {
                            await _broker.PlaceOrder(symbol, Convert.ToInt32(diff));
                        }
                    } else {
                        // Does not have symbol already, buy or sell shares
                        await _broker.PlaceOrder(symbol, Convert.ToInt32(shares));
                    }
                } else {
                    // Has symbol but not in target pool, get to zero shares
                    var shares = -(currentPositions[symbol].qty);
                    await _broker.PlaceOrder(symbol, Convert.ToInt32(shares));
                }
            }
        }
    }

    public class EqualDistributorConfig {
        public Pool pool { get; set; }
        public string side { get; set; }
    }
}
