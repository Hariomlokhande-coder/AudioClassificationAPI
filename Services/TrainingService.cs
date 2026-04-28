using Microsoft.ML;
using AudioClassificationAPI.Models;
using AudioClassificationAPI.Features;
using System.Text;

namespace AudioClassificationAPI.Services
{
    public class TrainingService
    {
        private readonly MLContext _mlContext;
        private readonly AudioProcessingService _audioService;
        private readonly FeatureExtractor _featureExtractor;

        public TrainingService(AudioProcessingService audioService, FeatureExtractor featureExtractor)
        {
            _mlContext = new MLContext(seed: 1);
            _audioService = audioService;
            _featureExtractor = featureExtractor;
        }

        public string GenerateDatasetFromRaw(string rawDataPath, string outputCsvPath)
        {
            if (!Directory.Exists(rawDataPath))
                throw new DirectoryNotFoundException("Raw data directory not found.");

            Console.WriteLine("🚀 Starting Ingestion process...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var csvContent = new StringBuilder();
            csvContent.AppendLine("Label,RMS,ZCR,Avg,Min,Max,Var,S1,S2,S3,S4,M1,M2,M3,M4,M5,M6,M7,M8,M9,M10,M11,M12,M13");

            int fileCount = 0;
            var subDirectories = Directory.GetDirectories(rawDataPath);

            foreach (var dir in subDirectories)
            {
                string label = Path.GetFileName(dir);
                var files = Directory.GetFiles(dir, "*.*")
                    .Where(f => f.EndsWith(".wav") || f.EndsWith(".mp3"));

                foreach (var file in files)
                {
                    try
                    {
                        var samples = _audioService.ReadAudioFile(file);
                        var features = _featureExtractor.ExtractFeatures(samples);

                        var row = $"{label},{features[0]:F4},{features[1]:F4},{features[2]:F4},{features[3]:F4},{features[4]:F4},{features[5]:F4},{features[6]:F4},{features[7]:F4},{features[8]:F4},{features[9]:F4}," +
                                  string.Join(",", features.Skip(10).Take(13).Select(f => f.ToString("F4")));
                        csvContent.AppendLine(row);
                        fileCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error processing {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }

            File.WriteAllText(outputCsvPath, csvContent.ToString());
            stopwatch.Stop();

            string resultMessage = $"✅ Dataset generated with {fileCount} samples. Time taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds.";
            Console.WriteLine(resultMessage);
            return resultMessage;
        }

        public string TrainModel(string dataPath, string modelPath)
        {
            // Validate paths
            if (string.IsNullOrWhiteSpace(dataPath) || !File.Exists(dataPath))
                throw new FileNotFoundException("Dataset file not found.");

            if (string.IsNullOrWhiteSpace(modelPath))
                throw new ArgumentException("Model path is invalid.");

            Console.WriteLine("Starting Model Training...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Load data
                var data = _mlContext.Data.LoadFromTextFile<AudioData>(
                    path: dataPath,
                    hasHeader: true,
                    separatorChar: ',');

                if (data == null)
                    throw new Exception("Failed to load dataset.");

                // Split data
                var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

                // Pipeline
                var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
                    .Append(_mlContext.Transforms.Concatenate("Features", 
                        "RMS", "ZCR", "Avg", "Min", "Max", "Var", "S1", "S2", "S3", "S4",
                        "M1", "M2", "M3", "M4", "M5", "M6", "M7", "M8", "M9", "M10", "M11", "M12", "M13"))
                    .Append(_mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy())
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                //  Train
                var model = pipeline.Fit(split.TrainSet);

                //Evaluate
                var predictions = model.Transform(split.TestSet);
                var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

                if (metrics.MacroAccuracy < 0.5)
                {
                    throw new Exception($"Model accuracy too low: {metrics.MacroAccuracy}");
                }

                //Ensure output directory exists
                var dir = Path.GetDirectoryName(modelPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);

                // Save model
                _mlContext.Model.Save(model, data.Schema, modelPath);

                //Save Metadata (Accuracy, Precision, Recall)
                var metadataPath = Path.Combine(Path.GetDirectoryName(modelPath)!, "metadata.json");
                var metadata = $"{{\"Accuracy\": {metrics.MacroAccuracy:F4}, \"Precision\": {metrics.MacroAccuracy:F4}, \"Recall\": {metrics.MicroAccuracy:F4}, \"LastTrained\": \"{DateTime.Now:MMM dd, HH:mm}\"}}";
                File.WriteAllText(metadataPath, metadata);

                stopwatch.Stop();
                string resultMessage = $"🏆 Model trained. Accuracy: {metrics.MacroAccuracy:P1}. Time: {stopwatch.Elapsed.TotalSeconds:F1}s";
                Console.WriteLine(resultMessage);
                return resultMessage;
            }
            catch (Exception ex)
            {
                throw new Exception("Training failed.", ex);
            }
        }
    }
}