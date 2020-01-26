using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using Newtonsoft.Json;

using harambe_trader.Services;

namespace harambe_trader.Strategy {
    public class Parser {
        private Database _database;
        private Dictionary<string, ISelector> _selectors = new Dictionary<string, ISelector>();
        private Dictionary<string, IDistributor> _distributors = new Dictionary<string, IDistributor>();

        public Parser(Database database) {
            _database = database;
        }

        public void AddSelector(string name, ISelector selector) {
            _selectors[name] = selector;
        }

        public void AddDistributor(string name, IDistributor distributor) {
            _distributors[name] = distributor;
        }

        private async Task ResolveReferencedTypes(Type configType, dynamic root) {
            foreach (var m in configType.GetMembers()) {
                if (m.MemberType == MemberTypes.Property) {
                    var pi = ((PropertyInfo)m);
                    var v = pi.GetValue(root);

                    if (v != null) {
                        if (pi.PropertyType.GetInterface(typeof(IReferencedType).Name) != null) {
                            var referencedType = v as IReferencedType;
                            await referencedType.ResolveReferences(_database);
                        }

                        ResolveReferencedTypes(v.GetType(), v);
                    }
                }
            }
        }

        private async Task ExecuteSelector(
            string strategyName,
            SelectorParams selectorParams,
            ISelector selector,
            dynamic config
        ) {
            List<string> symbols = await selector.GetSymbols(config);

            if (selectorParams.count < 0) {
                symbols = symbols.TakeLast(selectorParams.count).ToList();
            } else {
                symbols = symbols.Take(selectorParams.count).ToList();
            }

            Dictionary<string, dynamic> outputs = new Dictionary<string, dynamic>() {
                {"symbols", symbols}
            };

            // TODO: cache outputs in parser and get from cache instead of DB
            await _database.SaveOutput(strategyName, "selector", outputs);
        }

        public async Task ExecuteDistributor(IDistributor distributor, dynamic config) {
            await distributor.ReconcilePool(config);
        }

        public async Task ProcessStrategyFile(string filename) {
            var tradingStrategy = new Deserializer()
                .Deserialize<TradingStrategy>(File.OpenText(filename));

            foreach (var strategy in tradingStrategy.strategy) {
                if (strategy.selector != null) {
                    var configAsJSONString = JsonConvert.SerializeObject(strategy.selector.config);
                    // TODO: way to avoid converting node to string and then back to real type
                    var selector = _selectors[strategy.selector.type];
                    var config = JsonConvert.DeserializeObject(
                        configAsJSONString,
                        selector.GetConfigType()
                    );

                    await ResolveReferencedTypes(selector.GetConfigType(), config);
                    await ExecuteSelector(strategy.name, strategy.selector, selector, config);
                }

                if (strategy.distributor != null) {
                    var configAsJSONString = JsonConvert.SerializeObject(strategy.distributor.config);
                    // TODO: way to avoid converting node to string and then back to real type
                    var distributor = _distributors[strategy.distributor.type];
                    var config = JsonConvert.DeserializeObject(
                        configAsJSONString,
                        distributor.GetConfigType()
                    );

                    await ResolveReferencedTypes(distributor.GetConfigType(), config);
                    await ExecuteDistributor(distributor, config);
                }
            }
        }
    }

    public interface ISelector {
        Type GetConfigType();
        Task<List<string>> GetSymbols(dynamic config);
    }

    public interface IDistributor {
        Type GetConfigType();
        Task ReconcilePool(dynamic config);
    }

    public class TradingStrategy {
        public List<Strategy> strategy { get; set; }
    }

    public class Strategy {
        public string name { get; set; }
        public string schedule { get; set; }
        public DistributorParams distributor { get; set; }
        public SelectorParams selector { get; set; }
    }

    public class DistributorParams {
        public string type { get; set; }
        public dynamic config { get; set; }
    }

    public class SelectorParams {
        public string type { get; set; }
        public int count { get; set; } = 0;
        public dynamic config { get; set; }
    }
}
