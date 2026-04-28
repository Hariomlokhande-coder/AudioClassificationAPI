using Microsoft.ML;
using AudioClassificationAPI.Models;

namespace AudioClassificationAPI.Services
{
    public class PredictionService
    {
        private readonly MLContext _mlContext;
        private ITransformer? _model;
        private PredictionEngine<AudioData, PredictionResult>? _predictionEngine;
        private readonly object _lock = new object();

        public PredictionService()
        {
            _mlContext = new MLContext();
        }

        public void EnsureModelLoaded(string modelPath)
        {
            if (_predictionEngine != null) return;

            lock (_lock)
            {
                if (_predictionEngine != null) return;

                if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
                    throw new FileNotFoundException("Model file not found. Please train the model first.");

                try
                {
                    using var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _model = _mlContext.Model.Load(stream, out _);
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<AudioData, PredictionResult>(_model);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error loading model.", ex);
                }
            }
        }

        public PredictionResult Predict(float[] features)
        {
            if (_predictionEngine == null)
                throw new InvalidOperationException("Model is not loaded. Call EnsureModelLoaded first.");

            if (features.Length < 23)
                throw new ArgumentException("23 features (including MFCC) are required for prediction.");

            lock (_lock) // PredictionEngine is not thread-safe
            {
                var input = new AudioData
                {
                    RMS = features[0], ZCR = features[1], Avg = features[2],
                    Min = features[3], Max = features[4], Var = features[5],
                    S1 = features[6], S2 = features[7], S3 = features[8], S4 = features[9],
                    
                    M1 = features[10], M2 = features[11], M3 = features[12], M4 = features[13],
                    M5 = features[14], M6 = features[15], M7 = features[16], M8 = features[17],
                    M9 = features[18], M10 = features[19], M11 = features[20], M12 = features[21],
                    M13 = features[22],
                    Label = ""
                };

                return _predictionEngine.Predict(input);
            }
        }

        // Method to reset engine after retraining
        public void ResetEngine()
        {
            lock (_lock)
            {
                _predictionEngine = null;
                _model = null;
            }
        }
    }
}