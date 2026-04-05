using System;
using System.Collections.Generic;
using System.Threading;
using MongoDB.Bson;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.IO.Serialization;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.UMania.Objects;

namespace osu.Game.Rulesets.UMania.Beatmaps;

public class PassBeatmapConverter : BeatmapConverter<HitObject>
{
    public PassBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
        : base(beatmap, ruleset)
    {
    }

    public override bool CanConvert() => true;

    protected override IEnumerable<HitObject> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken)
    {
        return [original];
    }

    public new Beatmap<HitObject> ConvertBeatmap(IBeatmap beatmap, CancellationToken token)
    {
        var convertBeatmap = base.ConvertBeatmap(beatmap, token);

        // Clone hitobjects to avoid mutating the original beatmap
        var clonedHitObjects = new List<HitObject>(convertBeatmap.HitObjects.Count);
        
        foreach (var hitObject in convertBeatmap.HitObjects)
        {
            HitObject cloned;
            
            // Clone based on concrete type
            if (hitObject is Note note)
            {
                cloned = new Note
                {
                    StartTime = note.StartTime,
                    Samples = cloneSamples(note.Samples),
                    Column = note.Column
                };
            }
            else if (hitObject is HoldNote holdNote)
            {
                cloned = new HoldNote
                {
                    StartTime = holdNote.StartTime,
                    Duration = holdNote.Duration,
                    Column = holdNote.Column,
                    Samples = cloneSamples(holdNote.Samples),
                    NodeSamples = holdNote.NodeSamples
                };
            }
            else if (hitObject is ManiaHitObject maniaHitObject)
            {
                cloned = new ManiaHitObject
                {
                    StartTime = maniaHitObject.StartTime,
                    Column = maniaHitObject.Column,
                    Samples = cloneSamples(maniaHitObject.Samples)
                };
            }
            else
            {
                // Fallback for unknown types - just use original
                cloned = hitObject;
            }
            
            // Convert X position if needed
            if (cloned is IHasXPosition xPosition && xPosition.X > 6)
            {
                xPosition.X = GetColumn(xPosition.X);
            }
            
            clonedHitObjects.Add(cloned);
        }

        convertBeatmap.HitObjects = clonedHitObjects;
        return convertBeatmap;
    }

    private List<HitSampleInfo> cloneSamples(IList<HitSampleInfo> samples)
    {
        var cloned = new List<HitSampleInfo>(samples.Count);
        foreach (var sample in samples)
        {
            cloned.Add(new HitSampleInfo(sample.Name, sample.Bank, sample.Suffix, sample.Volume));
        }
        return cloned;
    }

    protected int GetColumn(float position)
    {

        float localXDivisor = 512f / 6;
        return Math.Clamp((int)MathF.Floor(position / localXDivisor), 0, 6 - 1);
    }
}
