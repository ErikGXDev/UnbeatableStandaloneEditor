// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Audio;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.UMania.Edit.Blueprints;

namespace osu.Game.Rulesets.UMania.Edit.Composition
{
    public class UbHoldNoteCompositionTool : UbBaseCompositionTool
    {
        public UbHoldNoteCompositionTool(string name, UbIconType icon, List<int> columns, List<string> hitSamples, string bank = HitSampleInfo.BANK_NORMAL)
            : base(name, icon, columns, hitSamples, bank)
        {
        }

        //public override Drawable CreateIcon() => new BeatmapStatisticIcon(BeatmapStatisticsIconType.Sliders);

        public override HitObjectPlacementBlueprint CreatePlacementBlueprint() => new UbHoldNotePlacementBlueprint(Columns, HitSamples, Bank);
    }
}
