using System.Net.Http.Headers;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace harambe_trader.Services {
    public class DatasourceService {
        private HttpClient _client = new HttpClient();
        private string _token;
        private string _endpoint;

        public DatasourceService(string endpoint, string token) {
            _token = token;
            _endpoint = endpoint;

            // _client.BaseAddress = new Uri(endpoint); // For some reason this doesn't work
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
        }

        public async Task<Quote> GetQuote(string symbol) {
            var res = await _client
                .GetAsync($"{_endpoint}/stock/{symbol}/quote?token={_token}");

            return await res
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsAsync<Quote>();
        }
    }

    public class Quote {
        public double latestPrice { get; set; }
    }
}
