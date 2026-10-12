using osu.Game.Rulesets.UMania.FMOD;

namespace UnbeatableStandaloneEditor.Import;

public class ImportOffsetTester
{
    public static int GetOffset(string audioFilePath)
    {
        var offset = FmodOffsetAnalyzer.AnalyseAsync(audioFilePath).GetAwaiter().GetResult().OffsetMs;
        var biased = FmodOffsetAnalyzer.AnalyzedOffsetToBiased(offset);
        return biased;
    }
}
