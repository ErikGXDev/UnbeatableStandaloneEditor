// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Extensions;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit.Components.TernaryButtons;

namespace osu.Game.Rulesets.UMania.Edit.Blueprints
{
    public class UbNoteBuilderHelper : UbNoteBuilder
    {
        private UnbeatableHitObjectComposer composer;
        private HitObject hitObject;

        public UbNoteBuilderHelper(UnbeatableHitObjectComposer composer, HitObject hitObject) : base(hitObject)
        {
            this.composer = composer;
            this.hitObject = hitObject;
        }

        static bool isModActive(DrawableTernaryButton modButton)
        {
            return modButton.Current.Value == TernaryState.True && modButton.Enabled.Value;
        }

        public void ApplyEverything(List<string> hitSampleInfos, string mainBank)
        {
            ApplySamples(hitSampleInfos);
            ApplyMainBank(mainBank);
            ApplyModifierLayer();
        }
        
        public void RecomputeFromCurrentState()
        {
            var icon = InferObjectTypeIcon();
            if (!BaseSamples.TryGetValue(icon, out var baseSamples))
                baseSamples = new List<string>();

            Recompute(baseSamples, HitSampleInfo.BANK_NORMAL);
            ApplyModifierLayer();
        }

        public void ApplyModifierLayer()
        {
            if (isModActive(composer.ModCopButton))
            {
                ApplyModifierMainBank(composer.ModCopButton, HitSampleInfo.BANK_DRUM);

                ApplyModifierSample(composer.ModCopFinishButton, HitSampleInfo.HIT_FINISH);
                ApplyModifierSample(composer.ModCop2Button, HitSampleInfo.HIT_WHISTLE);
                ApplyModifierSample(composer.ModCop3Button, HitSampleInfo.HIT_CLAP);
                ApplyModifierSample(composer.ModCop4Button, HitSampleInfo.HIT_WHISTLE);
                ApplyModifierSample(composer.ModCop4Button, HitSampleInfo.HIT_CLAP);

                ApplyHeavyBrawl(composer.ModCopHeavyButton);
            }
            else
            {
                ApplyModifierSample(composer.ModInvisibleButton, HitSampleInfo.HIT_CLAP);
                ApplyModifierMainBank(composer.ModFlyingButton, HitSampleInfo.BANK_SOFT);
                ApplyModifierSample(composer.ModSwapImmediateButton, HitSampleInfo.HIT_CLAP);
            }

            foreach (var nest in hitObject.NestedHitObjects)
            {
                nest.Samples = hitObject.Samples;
            }
        }

    }
}
