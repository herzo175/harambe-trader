using Grpc.Core;

using Endpoints.Harambe6;

namespace harambe_trader.Services {
    public class PredictorService {
        private string _endpoint;

        public PredictorService(string endpoint) {
            _endpoint = endpoint;
        }

        private Predictor.PredictorClient _makeClient() {
            var channel = new Channel(_endpoint, ChannelCredentials.Insecure);
            return new Predictor.PredictorClient(channel);
        }

        public BacktestReply Backtest(BacktestRequest request) {
            return _makeClient().Backtest(request);
        }

        public PredictionReply Predict(PredictionRequest request) {
            return _makeClient().Predict(request);
        }
    }


}
