using System;
using System.Linq;
using System.Collections.Generic;

namespace AudioClassificationAPI.Features
{
    public static class MFCC
    {
        public static float[] CalculateMFCC(float[] samples, int sampleRate = 16000, int numCoeffs = 13)
        {
            int frameSize = 1024;
            int hopSize = 512;
            int numFilters = 26;

            // 🔥 Ensure minimum length
            if (samples.Length < frameSize)
            {
                samples = samples.Concat(new float[frameSize]).Take(frameSize).ToArray();
            }

            var mfccList = new List<float[]>();

            // 🔥 MULTI-FRAME PROCESSING (IMPORTANT FIX)
            for (int i = 0; i < samples.Length - frameSize; i += hopSize)
            {
                var frame = samples.Skip(i).Take(frameSize).ToArray();

                // 1️⃣ Hamming Window
                for (int j = 0; j < frame.Length; j++)
                {
                    frame[j] *= (float)(0.54 - 0.46 * Math.Cos(2 * Math.PI * j / (frame.Length - 1)));
                }

                // 2️⃣ FFT
                double[] real = frame.Select(x => (double)x).ToArray();
                double[] imag = new double[frameSize];
                FFT(real, imag);

                // 3️⃣ Power Spectrum
                double[] power = new double[frameSize / 2];
                for (int j = 0; j < power.Length; j++)
                {
                    power[j] = real[j] * real[j] + imag[j] * imag[j];
                }

                // 4️⃣ Mel Filter Banks
                double[] mel = ApplyMelFilterBanks(power, numFilters);

                // 5️⃣ Log Scaling + Clamp (🔥 stability fix)
                for (int j = 0; j < mel.Length; j++)
                {
                    mel[j] = Math.Log(mel[j] + 1e-6);
                    mel[j] = Math.Clamp(mel[j], -50, 50); // prevent extreme values
                }

                // 6️⃣ DCT
                float[] mfcc = ApplyDCT(mel, numCoeffs);

                mfccList.Add(mfcc);
            }

            // 🔥 AGGREGATION (average of all frames)
            float[] finalMfcc = new float[numCoeffs];

            foreach (var mfcc in mfccList)
            {
                for (int i = 0; i < numCoeffs; i++)
                {
                    finalMfcc[i] += mfcc[i];
                }
            }

            for (int i = 0; i < numCoeffs; i++)
            {
                finalMfcc[i] /= mfccList.Count;
            }

            return finalMfcc;
        }

        // 🔹 Simplified Mel Filter Bank
        private static double[] ApplyMelFilterBanks(double[] power, int numFilters)
        {
            int n = power.Length;
            double[] mel = new double[numFilters];

            for (int i = 0; i < numFilters; i++)
            {
                int start = i * n / numFilters;
                int end = (i + 1) * n / numFilters;

                for (int j = start; j < end; j++)
                {
                    mel[i] += power[j];
                }
            }

            return mel;
        }

        // 🔹 DCT
        private static float[] ApplyDCT(double[] input, int numCoeffs)
        {
            int n = input.Length;
            float[] result = new float[numCoeffs];

            for (int k = 0; k < numCoeffs; k++)
            {
                double sum = 0;
                for (int i = 0; i < n; i++)
                {
                    sum += input[i] * Math.Cos(Math.PI * k * (2 * i + 1) / (2 * n));
                }
                result[k] = (float)sum;
            }

            return result;
        }

        // 🔹 FFT
        public static void FFT(double[] real, double[] imag)
        {
            int n = real.Length;
            if (n <= 1) return;

            double[] evenReal = new double[n / 2];
            double[] evenImag = new double[n / 2];
            double[] oddReal = new double[n / 2];
            double[] oddImag = new double[n / 2];

            for (int i = 0; i < n / 2; i++)
            {
                evenReal[i] = real[2 * i];
                evenImag[i] = imag[2 * i];
                oddReal[i] = real[2 * i + 1];
                oddImag[i] = imag[2 * i + 1];
            }

            FFT(evenReal, evenImag);
            FFT(oddReal, oddImag);

            for (int k = 0; k < n / 2; k++)
            {
                double angle = -2 * Math.PI * k / n;
                double cos = Math.Cos(angle);
                double sin = Math.Sin(angle);

                double tReal = cos * oddReal[k] - sin * oddImag[k];
                double tImag = sin * oddReal[k] + cos * oddImag[k];

                real[k] = evenReal[k] + tReal;
                imag[k] = evenImag[k] + tImag;
                real[k + n / 2] = evenReal[k] - tReal;
                imag[k + n / 2] = evenImag[k] - tImag;
            }
        }
    }
}