// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring.Legacy;
using osu.Game.Rulesets.UMania.Beatmaps;

namespace osu.Game.Rulesets.UMania.Difficulty
{
    internal class ManiaLegacyScoreSimulator : ILegacyScoreSimulator
    {
        public LegacyScoreAttributes Simulate(IWorkingBeatmap workingBeatmap, IBeatmap playableBeatmap)
        {
            return new LegacyScoreAttributes
            {
                ComboScore = 1000000,
                MaxCombo = 0 // Max combo is mod-dependent, so any value here is insufficient.
            };
        }

        public double GetLegacyScoreMultiplier(IReadOnlyList<Mod> mods, LegacyBeatmapConversionDifficultyInfo difficulty)
        {
            bool scoreV2 = mods.Any(m => m is ModScoreV2);

            double multiplier = 1.0;



            if (new UManiaRuleset().RulesetInfo.Equals(difficulty.SourceRuleset))
                return multiplier;

            // Apply key mod multipliers.
            int originalColumns = ManiaBeatmapConverter.GetColumnCount(difficulty);
            int actualColumns = ManiaBeatmapConverter.GetColumnCount(difficulty, mods);

            if (actualColumns > originalColumns)
                multiplier *= 0.9;
            else if (actualColumns < originalColumns)
                multiplier *= 0.9 - 0.04 * (originalColumns - actualColumns);

            return multiplier;
        }
    }
}
