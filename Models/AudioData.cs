using Microsoft.ML.Data;

namespace AudioClassificationAPI.Models
{
    public class AudioData
    {
        [LoadColumn(0)]
        public string? Label { get; set; }

        [LoadColumn(1)] public float RMS { get; set; }
        [LoadColumn(2)] public float ZCR { get; set; }
        [LoadColumn(3)] public float Avg { get; set; }
        [LoadColumn(4)] public float Min { get; set; }
        [LoadColumn(5)] public float Max { get; set; }
        [LoadColumn(6)] public float Var { get; set; }
        [LoadColumn(7)] public float S1 { get; set; }
        [LoadColumn(8)] public float S2 { get; set; }
        [LoadColumn(9)] public float S3 { get; set; }
        [LoadColumn(10)] public float S4 { get; set; }

        // 13 MFCC Coefficients
        [LoadColumn(11)] public float M1 { get; set; }
        [LoadColumn(12)] public float M2 { get; set; }
        [LoadColumn(13)] public float M3 { get; set; }
        [LoadColumn(14)] public float M4 { get; set; }
        [LoadColumn(15)] public float M5 { get; set; }
        [LoadColumn(16)] public float M6 { get; set; }
        [LoadColumn(17)] public float M7 { get; set; }
        [LoadColumn(18)] public float M8 { get; set; }
        [LoadColumn(19)] public float M9 { get; set; }
        [LoadColumn(20)] public float M10 { get; set; }
        [LoadColumn(21)] public float M11 { get; set; }
        [LoadColumn(22)] public float M12 { get; set; }
        [LoadColumn(23)] public float M13 { get; set; }
    }
}
