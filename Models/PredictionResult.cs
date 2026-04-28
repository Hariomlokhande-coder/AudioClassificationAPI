using Microsoft.ML.Data;

namespace AudioClassificationAPI.Models
{
    public class PredictionResult
    {
        [ColumnName("PredictedLabel")]
        public string? PredictedLabel { get; set; }

        // Confidence scores for each class
        public float[]? Score { get; set; }
    }
}