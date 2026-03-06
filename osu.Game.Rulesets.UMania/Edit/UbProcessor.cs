using System.Linq;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.UMania.Edit.Blueprints;

namespace osu.Game.Rulesets.UMania.Edit;

public class UbProcessor : BeatmapProcessor
{
    public UbProcessor(IBeatmap beatmap)
        : base(beatmap)
    {
    }

    public override void PreProcess()
    {
        foreach (var hitObject in Beatmap.HitObjects)
        {
            foreach (var sample in hitObject.Samples.ToList())
            {
                /*Logger.Log("Sample name: " + sample.Name + ", bank: " + sample.Bank + ", suffix: " + sample.Suffix);
                if (sample.Name == HitSampleInfo.HIT_NORMAL && sample.Bank == HitSampleInfo.BANK_DRUM)
                {
                    var index = hitObject.Samples.IndexOf(sample);
                    hitObject.Samples[index] = new HitSampleInfo(sample.Name, HitSampleInfo.BANK_STRONG, sample.Suffix, sample.Volume, false);
                }*/
            }
        }

        base.PreProcess();

    }

    public override void PostProcess()
    {
        base.PostProcess();
    }
}
