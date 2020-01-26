using System.Text;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;

using Newtonsoft.Json;

namespace harambe_trader.Services {
    public class BrokerService {
        HttpClient _client = new HttpClient();

        // TODO: app config
        public BrokerService(string endpoint, string apiKeyID, string apiKeySecret) {
            _client.BaseAddress = new Uri(endpoint);

            _client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", apiKeyID);
            _client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", apiKeySecret);

            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
        }

        public async Task<bool> IsMarketOpen() {
            var isOpen = false;
            HttpResponseMessage response = await _client.GetAsync("v2/clock");

            if (!response.IsSuccessStatusCode) {
                Console.WriteLine($"Error checking market is open: {response.StatusCode}");
                Console.WriteLine($"Message: {await response.Content.ReadAsStringAsync()}");
                // TODO: error handling
            } else {
                var clock = await response.Content.ReadAsAsync<Clock>();
                isOpen = clock.is_open;
            }

            return isOpen;
        }

        public async Task<List<Position>> GetPositions() {
            List<Position> positions = null;

            HttpResponseMessage response = await _client.GetAsync("v2/positions");

            if (!response.IsSuccessStatusCode) {
                Console.WriteLine($"Error checking positions: {response.StatusCode}");
                Console.WriteLine($"Message: {await response.Content.ReadAsStringAsync()}");
                // TODO: error handling
            } else {
                positions = await response.Content.ReadAsAsync<List<Position>>();
            }

            return positions;
        }

        public async Task<double> GetCash() {
            double cash = 0;

            HttpResponseMessage response = await _client.GetAsync("v2/account");

            if (!response.IsSuccessStatusCode) {
                Console.WriteLine($"Error checking market is open: {response.StatusCode}");
                Console.WriteLine($"Message: {await response.Content.ReadAsStringAsync()}");
                // TODO: error handling
            } else {
                var account = await response.Content.ReadAsAsync<Account>();
                cash = Double.Parse(account.cash);
            }

            return cash;
        }

        public async Task PlaceOrder(string symbol, int numShares) {
            var order = new Order() {
                symbol = symbol,
                qty = Math.Abs(numShares),
                side = numShares < 0 ? "sell" : "buy"
            };

            Console.WriteLine($"order: {order.symbol} {order.qty} {order.side}");

            var req = await _client.PostAsync(
                "v2/orders",
                new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json")
            );

            // TODO: error handling?
            req.EnsureSuccessStatusCode();
        } 
    }

    public class Clock {
        public bool is_open { get; set; }
    }

    public class Account {
        public string cash { get; set; } =  "0.00";
    }

    public class Position {
        public string symbol { get; set; }
        public int qty { get; set; }
        public string side { get; set; }
        public double market_value { get; set; }
    }

    public class Order {
        public string symbol { get; set; }
        public int qty { get; set; }
        public string side { get; set; }
        public string type { get; set; } = "market"; // TODO: allow for limit, stop, stop_limit types
        public string time_in_force { get; set; } = "gtc"; // TODO: allow for other order types
    }
}
