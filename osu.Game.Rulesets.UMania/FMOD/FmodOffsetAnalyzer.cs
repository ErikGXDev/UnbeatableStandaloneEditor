using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Audio.Track;
using osu.Framework.Logging;

namespace osu.Game.Rulesets.UMania.FMOD
{
    public class OffsetAnalysisResult
    {
        public readonly bool Success;

        public readonly double OffsetMs;

        public readonly int BassOnsetMs;
        public readonly int FmodOnsetMs;

        public readonly float[] BassEnvelope;
        public readonly float[] FmodEnvelope;

        public readonly string Summary;

        private OffsetAnalysisResult(bool success, double offsetMs, int bassOnsetMs, int fmodOnsetMs,
            float[] bassEnvelope, float[] fmodEnvelope, string summary)
        {
            Success = success;
            OffsetMs = offsetMs;
            BassOnsetMs = bassOnsetMs;
            FmodOnsetMs = fmodOnsetMs;
            BassEnvelope = bassEnvelope;
            FmodEnvelope = fmodEnvelope;
            Summary = summary;
        }

        public static OffsetAnalysisResult Failure(string summary) =>
            new(false, 0, -1, -1, Array.Empty<float>(), Array.Empty<float>(), summary);

        public static OffsetAnalysisResult Failure(string summary, float[] bassEnvelope, float[] fmodEnvelope) =>
            new(false, 0, -1, -1, bassEnvelope, fmodEnvelope, summary);

        public static OffsetAnalysisResult Succeeded(double offsetMilliseconds, int bassOnsetMilliseconds, int fmodOnsetMilliseconds, string summary) =>
            new(true, offsetMilliseconds, bassOnsetMilliseconds, fmodOnsetMilliseconds, Array.Empty<float>(),
                Array.Empty<float>(), summary);

        public static OffsetAnalysisResult Succeeded(double offsetMilliseconds, int bassOnsetMilliseconds,
            int fmodOnsetMilliseconds, float[] bassEnvelope, float[] fmodEnvelope, string summary) =>
            new(true, offsetMilliseconds, bassOnsetMilliseconds, fmodOnsetMilliseconds, bassEnvelope, fmodEnvelope,
                summary);
    }

    
    public static class FmodOffsetAnalyzer
    {
        // honestly overkill for the song beginning
        // makes it not load the whole song
        private const int analysis_window_ms = 20000;

        // Fail-safe and export offset input limit
        private const int max_shift_ms = 1000;

        private const float peak_threshold = 0.03f;

        public static Task<OffsetAnalysisResult> AnalyseAsync(string audioPath, CancellationToken cancellationToken = default) =>
            // Task factory because Waveform.GetPoints also uses Task.Run
            Task.Factory.StartNew(
                () => analyse(audioPath, cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

        private static OffsetAnalysisResult analyse(string audioPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(audioPath))
                return OffsetAnalysisResult.Failure("No audio file is set for this beatmap.");

            // BASS
            float[]? bassEnvelope;

            try
            {
                using var waveform = new Waveform(File.OpenRead(audioPath));

                var points = waveform.GetPoints();

                bassEnvelope = bassToFloats(points);
            }
            catch (Exception e)
            {
                Logger.Error(e, "BASS decode failed");
                return OffsetAnalysisResult.Failure("Could not decode BASS waveform.");
            }

            if (bassEnvelope == null)
            {
                return OffsetAnalysisResult.Failure("BASS Envelope empty");
            }

            // FMOD
            if (!FmodSoundDecoder.TryDecodeData(audioPath, analysis_window_ms, out float[]? fmodSamples, out int fmodFrequency))
                return OffsetAnalysisResult.Failure("Could not decode the audio with FMOD.");

            if (fmodSamples == null || fmodFrequency <= 0)
                return OffsetAnalysisResult.Failure("FMOD did not give any data.");

            cancellationToken.ThrowIfCancellationRequested();

            float[] fmodEnvelope = fmodToFloats(fmodSamples, fmodFrequency);
            
            // limit bass as well
            int maxPoints = analysis_window_ms;
            if (bassEnvelope.Length > maxPoints)
                bassEnvelope = bassEnvelope.Take(maxPoints).ToArray();

            // equal volume
            normalizeEnvelope(bassEnvelope);
            normalizeEnvelope(fmodEnvelope);

            return analyseByOnset(bassEnvelope, fmodEnvelope);
        }

        // FMOD is quieter than BASS
        private static void normalizeEnvelope(float[] envelope)
        {
            float peak = envelope.Max();
            if (peak > 0.0001f)
            {
                float scale = 1f / peak;
                for (int i = 0; i < envelope.Length; i++)
                    envelope[i] *= scale;
            }
        }
        
        private static float[]? bassToFloats(Waveform.Point[] points)
        {
            if (points.Length == 0)
                return null;

            var envelope = new float[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                envelope[i] = Math.Min(1, Math.Max(points[i].AmplitudeLeft, points[i].AmplitudeRight));
            }

            return envelope;
        }
        
        // turn fmod samples into points (per ms), like BASS
        private static float[] fmodToFloats(float[] samples, int frequency)
        {
            int samplesPerPoint = Math.Max(1, frequency / 1000); // per ms
            int pointCount = Math.Max(1, samples.Length / samplesPerPoint);

            var envelope = new float[pointCount];

            for (int point = 0; point < pointCount; point++)
            {
                int start = point * samplesPerPoint;
                int end = Math.Min(start + samplesPerPoint, samples.Length);

                float peak = 0;

                // Always find strongest point
                for (int i = start; i < end; i++)
                {
                    float magnitude = Math.Abs(samples[i]);

                    if (magnitude > peak)
                        peak = magnitude;
                }

                envelope[point] = Math.Min(1, peak);
            }

            return envelope;
        }

        private static int findFirstOnset(float[] envelope)
        {
            if (envelope.Length == 0)
                return -1;

            // rising edge detection
            for (int i = 1; i < envelope.Length; i++)
            {
                if (envelope[i] >= peak_threshold && envelope[i] > envelope[i - 1])
                    return i;
            }

            // fallback
            for (int i = 0; i < envelope.Length; i++)
            {
                if (envelope[i] >= peak_threshold)
                    return i;
            }

            return -1;
        }

        private static OffsetAnalysisResult analyseByOnset(float[] bass, float[] fmod)
        {
            int bassOnset = findFirstOnset(bass);
            int fmodOnset = findFirstOnset(fmod);

            if (bassOnset < 0 || fmodOnset < 0)
            {
                string name = bassOnset < 0 && fmodOnset < 0 ? "either decoder"
                    : bassOnset < 0 ? "BASS" : "FMOD";

                return OffsetAnalysisResult.Failure($"Could not find the music start for {name}", bass, fmod);
            }

            double offset = fmodOnset - bassOnset;

            if (Math.Abs(offset) > max_shift_ms)
                return OffsetAnalysisResult.Failure($"Huge ahh delay of {offset:F0} ms, something is probably wrong...", bass, fmod);

            return OffsetAnalysisResult.Succeeded(offset, bassOnset, fmodOnset, bass, fmod,
                $"First audio at {bassOnset}ms (BASS) and {fmodOnset}ms (FMOD).");
        }
    }
}
