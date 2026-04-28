using NAudio.Wave;

namespace AudioClassificationAPI.Services
{
    public class AudioProcessingService
    {
        public float[] ReadAudioFile(string filePath)
        {
            // 🔒 Validate input
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is null or empty.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Audio file not found: {filePath}");

            var extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".wav" && extension != ".mp3")
                throw new NotSupportedException("Only .wav and .mp3 files are supported.");

            try
            {
                using var audioFile = new AudioFileReader(filePath);

                var samples = new List<float>();
                var buffer = new float[1024];
                int read;

                while ((read = audioFile.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        samples.Add(buffer[i]);
                    }
                }

                if (samples.Count == 0)
                    throw new InvalidDataException("Audio file contains no data.");

                var trimmedSamples = TrimSilence(samples.ToArray());
                return Normalize(trimmedSamples);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing audio file: {filePath}", ex);
            }
        }

        private float[] TrimSilence(float[] samples, float threshold = 0.05f)
        {
            int start = 0;
            while (start < samples.Length && Math.Abs(samples[start]) < threshold) start++;

            int end = samples.Length - 1;
            while (end > start && Math.Abs(samples[end]) < threshold) end--;

            if (start >= end) return samples; // entire file is silent or too short to trim

            return samples.Skip(start).Take(end - start + 1).ToArray();
        }

        private float[] Normalize(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                throw new ArgumentException("Samples cannot be null or empty.");

            float max = samples.Select(x => Math.Abs(x)).Max();

            if (max == 0)
                return samples; // silent audio

            return samples.Select(x => x / max).ToArray();
        }
    }
}