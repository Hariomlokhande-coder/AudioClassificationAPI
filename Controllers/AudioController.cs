using Microsoft.AspNetCore.Mvc;
using AudioClassificationAPI.Services;
using AudioClassificationAPI.Features;
using Microsoft.AspNetCore.Authorization;

namespace AudioClassificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AudioController : ControllerBase
    {
        private readonly TrainingService _trainingService;
        private readonly PredictionService _predictionService;
        private readonly AudioProcessingService _audioProcessingService;
        private readonly FeatureExtractor _featureExtractor;

        private readonly string _dataPath = "Data/Processed/dataset.csv";
        private readonly string _modelPath = "Output/model.zip";
        private readonly string _rawDataPath = "Data/Raw";

        public AudioController(
            TrainingService trainingService,
            PredictionService predictionService,
            AudioProcessingService audioProcessingService,
            FeatureExtractor featureExtractor)
        {
            _trainingService = trainingService;
            _predictionService = predictionService;
            _audioProcessingService = audioProcessingService;
            _featureExtractor = featureExtractor;
        }

        [HttpPost("ingest")]
        public IActionResult Ingest()
        {
            try
            {
                var result = _trainingService.GenerateDatasetFromRaw(_rawDataPath, _dataPath);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("train")]
        public IActionResult Train()
        {
            try
            {
                var result = _trainingService.TrainModel(_dataPath, _modelPath);
                _predictionService.ResetEngine();
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("predict")]
        public IActionResult Predict(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            string extension = Path.GetExtension(file.FileName);
            string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");

            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                var samples = _audioProcessingService.ReadAudioFile(tempPath);
                var features = _featureExtractor.ExtractFeatures(samples);
                _predictionService.EnsureModelLoaded(_modelPath);
                var result = _predictionService.Predict(features);

                return Ok(new
                {
                    prediction = result.PredictedLabel,
                    confidence = result.Score?.Max()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            var stats = new Models.ModelStats();
            
            // Count samples
            if (System.IO.File.Exists(_dataPath))
            {
                var lines = System.IO.File.ReadAllLines(_dataPath);
                stats.TotalSamples = lines.Length > 1 ? lines.Length - 1 : 0;
            }

            // Load Metadata (Accuracy, Precision, Recall)
            var metadataPath = Path.Combine(Path.GetDirectoryName(_modelPath)!, "metadata.json");
            if (System.IO.File.Exists(metadataPath))
            {
                try {
                    var json = System.IO.File.ReadAllText(metadataPath);
                    if (json.Contains("\"Precision\":")) {
                        var parts = json.Split(',');
                        stats.Accuracy = double.Parse(parts[0].Split(':')[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                        stats.Precision = double.Parse(parts[1].Split(':')[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                        stats.Recall = double.Parse(parts[2].Split(':')[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    }
                } catch {}
            }

            // Check model status
            if (System.IO.File.Exists(_modelPath))
            {
                var info = new FileInfo(_modelPath);
                stats.ModelStatus = stats.Accuracy > 0 ? $"{stats.Accuracy:P1} Accurate" : "Active";
                stats.LastTrained = info.LastWriteTime.ToString("MMM dd, HH:mm");
            }

            return Ok(stats);
        }
    }
}