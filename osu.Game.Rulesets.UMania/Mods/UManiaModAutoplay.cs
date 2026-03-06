// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.UMania.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UMania.Beatmaps;

namespace osu.Game.Rulesets.UMania.Mods
{
    public class UManiaModAutoplay : ModAutoplay
    {
        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new ModReplayData(new ManiaAutoGenerator((ManiaBeatmap)beatmap).Generate(),
                new ModCreatedUser { Username = "sample" });
    }
}
