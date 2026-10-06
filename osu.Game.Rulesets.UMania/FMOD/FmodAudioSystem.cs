using System;
using osu.Framework.Logging;
using osu.Game.Rulesets.UMania.FMOD.Wrapper;

namespace osu.Game.Rulesets.UMania.FMOD
{
    // Thought process:
    // The best way to be as equally wrong as FMOD is to compare
    // BASS's waveform to FMOD's waveform and find the offset between them.
    // assuming FMOD's offset is not magically different between 
    // different audio formats, this should help to create a perfect
    // chart offset automatically.
    
    // singleton that manages initializing FMOD
    public static class FmodAudioSystem
    {
        private static readonly object system_lock = new object();

        private static Wrapper.System system;

        private static bool initialised;
        
        // unbelievably fancy piece of architecture
        public static TResult? WithSystem<TResult>(Func<Wrapper.System, TResult> action)
        {
            // prevent multiple access
            lock (system_lock)
            {
                if (!initialised)
                {
                    if (!tryInitialise(out Wrapper.System created))
                        return default;

                    system = created;
                }

                return action(system);
            }
        }

        public static void Dispose()
        {
            lock (system_lock)
            {
                if (!initialised)
                    return;

                try
                {
                    system.close();
                    system.release();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "FMOD system shutdown failed");
                }

                system = default;
                initialised = false;
            }
        }

        private static bool tryInitialise(out Wrapper.System created)
        {
            created = default;

            RESULT result;
            Wrapper.System s;

            try
            {
                result = Factory.System_Create(out s);
            }
            catch (Exception e)
            {
                Logger.Log($"FMOD native library could not be loaded: {e.Message}");
                return false;
            }

            if (result != RESULT.OK)
            {
                Logger.Log($"FMOD System_Create failed: {result}");
                return false;
            }

            result = s.setOutput(OUTPUTTYPE.NOSOUND);

            if (result != RESULT.OK)
            {
                Logger.Log($"FMOD setOutput(NOSOUND) failed: {result}");
                s.release();
                return false;
            }

            result = s.init(64, INITFLAGS.NORMAL, IntPtr.Zero);

            if (result != RESULT.OK)
            {
                Logger.Log($"FMOD init failed: {result}");
                s.release();
                return false;
            }

            s.setSoftwareFormat((int)SPEAKERMODE.STEREO, 0, 0);

            s.getVersion(out uint version);
            Logger.Log($"FMOD initialised for offset analysis ({version})");

            created = s;
            initialised = true;
            return true;
        }
    }
}