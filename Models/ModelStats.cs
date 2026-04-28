namespace AudioClassificationAPI.Models
{
    public class ModelStats
    {
        public int TotalSamples { get; set; }
        public string ModelStatus { get; set; } = "Inactive";
        public string LastTrained { get; set; } = "Never";
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double Accuracy { get; set; }
    }
}
