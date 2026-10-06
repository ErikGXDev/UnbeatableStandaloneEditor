using System;
using System.Buffers.Binary;
using System.IO;
using osu.Framework.Logging;
using osu.Game.Rulesets.UMania.FMOD.Wrapper;

namespace osu.Game.Rulesets.UMania.FMOD
{
    
    
    public static class FmodSoundDecoder
    {
        // decode fmod data to samples
        public static bool TryDecodeData(string path, int maxMilliseconds, out float[]? samples, out int frequency)
        {
            samples = null;
            frequency = 0;

            if (!File.Exists(path))
            {
                Logger.Log($"audio file not found at {path}");
                return false;
            }
            
            float[]? decoded = null;
            int decodedFrequency = 0;

            bool success = FmodAudioSystem.WithSystem(
                system => tryDecode(system, path, maxMilliseconds, out decoded, out decodedFrequency)) == true;

            samples = decoded;
            frequency = decodedFrequency;

            return success && samples != null;
        }

        // get mono samples from the data
        private static bool tryDecode(Wrapper.System system, string path, int maxMilliseconds, out float[]? samples, out int frequency)
        {
            samples = null;
            frequency = 0;

            Sound sound = default;

            try
            {
                // note: CreateSample or CreateCompressedSample seemed to have no effect on offsets.
                RESULT result = system.createSound(path, MODE.OPENONLY, out sound);

                if (result != RESULT.OK)
                {
                    Logger.Log($"FMOD analysis: createSound failed ({result}).");
                    return false;
                }

                if (sound.getFormat(out _, out SOUND_FORMAT format, out int channels, out int bits) != RESULT.OK)
                    return false;

                if (sound.getDefaults(out float defaultFrequency, out _) != RESULT.OK)
                    return false;

                if (sound.getLength(out uint pcmBytes, TIMEUNIT.PCMBYTES) != RESULT.OK || pcmBytes == 0)
                    return false;
                
                int bytes = bits / 8;

                if (defaultFrequency <= 0 || channels <= 0 || bytes <= 0)
                    return false;

                frequency = (int)Math.Round(defaultFrequency);

                // cap at max window
                int byteCount = (int)Math.Min(pcmBytes, (uint)(defaultFrequency * (maxMilliseconds / 1000.0) * channels * bytes));

                if (byteCount <= 0)
                    return false;

                var buffer = new byte[byteCount];

                RESULT readResult = sound.readData(buffer, out uint bytesRead);

                if (readResult != RESULT.OK && bytesRead == 0)
                {
                    Logger.Log($"FMOD analysis: could not read decoded data ({readResult}).");
                    return false;
                }

                samples = dataToFloats(buffer.AsSpan(0, (int)Math.Min(bytesRead, (uint)byteCount)), format, bytes, channels);
                return samples != null;
            }
            catch (Exception e)
            {
                Logger.Error(e, "FMOD analysis: decode failed");
                samples = null;
                return false;
            }
            finally
            {
                sound.release();
            }
        }
        
        private static float[]? dataToFloats(ReadOnlySpan<byte> data, SOUND_FORMAT format, int bytes, int channels)
        {
            int sampleCount = data.Length / bytes; // bytes per sample

            if (sampleCount < channels)
                return null;

            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
                samples[i] = sampleToFloat(data.Slice(i * bytes, bytes), format);

            if (channels == 1)
                return samples;

            int frameCount = sampleCount / channels; // samples per channel

            for (int frame = 0; frame < frameCount; frame++)
            {
                int offset = frame * channels;
                float sum = 0;

                for (int c = 0; c < channels; c++)
                    sum += samples[offset + c];

                samples[frame] = sum / channels; // average channels to mono
            }

            return samples.AsSpan(0, frameCount).ToArray();
        }
        
        // fancy math I got to google
        private static float sampleToFloat(ReadOnlySpan<byte> sample, SOUND_FORMAT format) => format switch
        {
            SOUND_FORMAT.PCM8 => (sample[0] - 128) / 128f,
            SOUND_FORMAT.PCM24 => normalise24(sample),
            SOUND_FORMAT.PCM32 => BinaryPrimitives.ReadInt32LittleEndian(sample) / 2147483648f,
            SOUND_FORMAT.PCMFLOAT => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(sample)),
            _ => BinaryPrimitives.ReadInt16LittleEndian(sample) / 32768f,
        };
        
        private static float normalise24(ReadOnlySpan<byte> sample)
        {
            int value = sample[0] | (sample[1] << 8) | (sample[2] << 16);

            if ((value & 0x800000) != 0)
                value -= 0x1000000;

            return value / 8388608f;
        }
    }
}
