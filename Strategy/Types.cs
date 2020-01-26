using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;

using harambe_trader.Services;



namespace harambe_trader.Strategy {
    public class Pool : IReferencedType {
        public Reference reference { get; set; }
        public HashSet<string> symbols { get; set; }

        public async Task ResolveReferences(Database database) {
            if (reference != null) {
                var output = JsonConvert
                    .DeserializeObject<Dictionary<string, dynamic>>(
                        await database.GetOutput(reference.step, reference.section)
                    );

                this.symbols = output["symbols"].ToObject<HashSet<string>>();
            }
        }
    }

    public class Reference {
        public string step { get; set; }
        public string section { get; set; }
    }

    public interface IReferencedType {
        Task ResolveReferences(Database database);
    }
}
