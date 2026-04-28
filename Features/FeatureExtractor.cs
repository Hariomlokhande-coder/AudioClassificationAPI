using System;
using System.Linq;
using System.Collections.Generic;

namespace AudioClassificationAPI.Features
{
    public class FeatureExtractor
    {
        public float[] ExtractFeatures(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                throw new ArgumentException("Samples cannot be null or empty.");

            // 🔥 FIX 1: Ensure fixed length (IMPORTANT)
            int targetLength = 16000; // 1 sec @ 16kHz
            samples = samples
                .Take(targetLength)
                .Concat(new float[targetLength])
                .Take(targetLength)
                .ToArray();

            var features = new List<float>();

            // 1-3. Basic Stats
            features.Add(CalculateRMS(samples));
            features.Add(CalculateZeroCrossingRate(samples));
            features.Add(samples.Average());

            // 4-6. Additional Stats
            features.Add(samples.Min());
            features.Add(samples.Max());
            features.Add(CalculateVariance(samples));

            // 🔥 FIX 2: Stable segmentation (equal chunks)
            int segmentSize = samples.Length / 4;

            for (int i = 0; i < 4; i++)
            {
                var segment = samples
                    .Skip(i * segmentSize)
                    .Take(segmentSize)
                    .ToArray();

                features.Add(CalculateRMS(segment));
            }

            // 🔥 FIX 3: MFCC (already upgraded)
            var mfccCoeffs = MFCC.CalculateMFCC(samples);
            features.AddRange(mfccCoeffs);

            // 🔥 FIX 4: Final safety (NaN / Infinity handling)
            for (int i = 0; i < features.Count; i++)
            {
                if (float.IsNaN(features[i]) || float.IsInfinity(features[i]))
                    features[i] = 0f;
            }

            return features.ToArray(); // Total 23 features
        }

        private float CalculateRMS(float[] samples)
        {
            if (samples.Length == 0) return 0;

            double sum = 0;
            foreach (var sample in samples)
                sum += sample * sample;

            return (float)Math.Sqrt(sum / samples.Length);
        }

        private float CalculateZeroCrossingRate(float[] samples)
        {
            if (samples.Length < 2) return 0;

            int zeroCrossings = 0;
            for (int i = 1; i < samples.Length; i++)
            {
                if ((samples[i - 1] >= 0 && samples[i] < 0) ||
                    (samples[i - 1] < 0 && samples[i] >= 0))
                {
                    zeroCrossings++;
                }
            }

            return (float)zeroCrossings / samples.Length;
        }

        private float CalculateVariance(float[] samples)
        {
            if (samples.Length == 0) return 0;

            float avg = samples.Average();
            return samples.Average(v => (v - avg) * (v - avg));
        }
    }
}