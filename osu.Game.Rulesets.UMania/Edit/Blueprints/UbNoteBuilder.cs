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
    public class UbNoteBuilder
    {
        private HitObject hitObject;
        
        public static readonly Dictionary<UbIconType, List<string>> BaseSamples = new Dictionary<UbIconType, List<string>>
        {
            { UbIconType.Note, new List<string>() },
            { UbIconType.Hold, new List<string>() },
            { UbIconType.Dodge, new List<string> { HitSampleInfo.HIT_WHISTLE } },
            { UbIconType.Double, new List<string> { HitSampleInfo.HIT_WHISTLE } },
            { UbIconType.Freestyle, new List<string>() },
            { UbIconType.Spam, new List<string> { HitSampleInfo.HIT_FINISH } },
            { UbIconType.Flip, new List<string>() },
            { UbIconType.Zoom, new List<string> { HitSampleInfo.HIT_WHISTLE } },
            { UbIconType.Brawl, new List<string>() },
        };

        public UbNoteBuilder(HitObject hitObject)
        {
            this.hitObject = hitObject;
        }

        static bool isModActive(DrawableTernaryButton modButton)
        {
            return modButton.Current.Value == TernaryState.True && modButton.Enabled.Value;
        }

        public void ApplySamples(List<string> samples)
        {
            var hitSamples = new List<HitSampleInfo>();

            hitSamples = hitObject.Samples.ToList();

            foreach (string sample in samples)
            {
                // EditorAutoBank must be false so the encoder preserves the explicit bank
                HitSampleInfo sampleInfo = hitObject.CreateHitSampleInfo(sample).With(newEditorAutoBank: false);
                hitSamples.Add(sampleInfo);
            }

            hitObject.Samples = hitSamples;
        }

        public void ApplyModifierSample(DrawableTernaryButton modButton, string sample)
        {
            if (isModActive(modButton))
            {
                HitSampleInfo sampleInfo = hitObject.CreateHitSampleInfo(sample).With(newVolume: 100, newEditorAutoBank: false);
                hitObject.Samples.Add(sampleInfo);
            }
        }

        public void ApplyModifierMainBank(DrawableTernaryButton modButton, string bank)
        {
            if (isModActive(modButton))
            {
                ApplyMainBank(bank);
            }
        }

        public void ApplyModifierAdditionBank(DrawableTernaryButton modButton, string bank)
        {
            if (isModActive(modButton))
            {
                ApplyAdditionBank(bank);
            }
        }

        // Add another hit sample here, otherwise heavy brawl cant be added on cop 1
        public void ApplyHeavyBrawl(DrawableTernaryButton modButton)
        {
            if (!isModActive(modButton))
                return;

            bool hasAdditionSample = hitObject.Samples.Any(s => s.Name != HitSampleInfo.HIT_NORMAL);
            if (!hasAdditionSample)
                hitObject.Samples.Add(hitObject.CreateHitSampleInfo(HitSampleInfo.HIT_FLOURISH).With(newVolume: 100, newEditorAutoBank: false));

            ApplyAdditionBank(HitSampleInfo.BANK_NORMAL);
        }

        public void ApplyMainBank(string bank)
        {
            var normalSample = hitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);

            if (normalSample == null)
            {
                hitObject.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_NORMAL, bank, string.Empty, 100, false));
                return;
            }

            var index = hitObject.Samples.IndexOf(normalSample);

            hitObject.Samples[index] = new HitSampleInfo(normalSample.Name,
                bank,
                normalSample.Suffix,
                100,
                false);
        }

        public void ApplyAdditionBank(string bank)
        {
            var additionSamples = hitObject.Samples.Where(s => s.Name != HitSampleInfo.HIT_NORMAL);

            foreach (var additionSample in additionSamples.ToList())
            {
                var index = hitObject.Samples.IndexOf(additionSample);

                hitObject.Samples[index] = new HitSampleInfo(additionSample.Name,
                    bank,
                    additionSample.Suffix,
                    additionSample.Volume,
                    false);
            }


        }
        
        public void Recompute(List<string> baseSamples, string baseBank)
        {
            hitObject.Samples.Clear();
            ApplySamples(baseSamples);
            ApplyMainBank(baseBank);
        }

        public HitSampleInfo GetMainSample()
        {
            var normalSample = hitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);

            return normalSample ?? new HitSampleInfo(HitSampleInfo.HIT_NORMAL, "normal", string.Empty, 100);
        }
        
        public HitSampleInfo GetAdditionSample()
        {
            var normalSample = hitObject.Samples.FirstOrDefault(s => s.Name != HitSampleInfo.HIT_NORMAL);

            return normalSample ?? new HitSampleInfo(HitSampleInfo.HIT_NORMAL, "normal", string.Empty, 100);
        }

        public bool HasSample(string sample)
        {
            return hitObject.Samples.Any(s => s.Name == sample);
        }

        public bool HasMainBank(string bank)
        {
            return hitObject.Samples.Any(s => s.Name == HitSampleInfo.HIT_NORMAL && s.Bank == bank);
        }

        public bool HasAdditionBank(string bank)
        {
            return hitObject.Samples.Any(s => s.Name != HitSampleInfo.HIT_NORMAL && s.Bank == bank);
        }

        public UbIconType InferObjectTypeIcon()
        {
            if (hitObject is ManiaHitObject maniaHitObject)
            {
                int column = maniaHitObject.Column;

                if (HasMainBank(HitSampleInfo.BANK_DRUM))
                {
                    return UbIconType.Brawl;
                }

                if (hitObject is HeadNote or HoldNote)
                {
                    if (column == 5)
                    {
                        return UbIconType.Spam;
                    }

                    /*
                    hitObject.Samples.ForEach(s =>
                        Logger.Log($"Sample: {s.Name}, Bank: {s.Bank}, Suffix: {s.Suffix}, Volume: {s.Volume}"));
                        */

                    if (HasSample(HitSampleInfo.HIT_WHISTLE))
                    {
                        return UbIconType.Double;
                    }

                    return UbIconType.Hold;
                }

                if (hitObject is Note)
                {
                    if (column == 5)
                    {
                        return UbIconType.Freestyle;
                    }

                    if (column == 4)
                    {
                        if (HasSample(HitSampleInfo.HIT_WHISTLE))
                        {
                            return UbIconType.Zoom;
                        }

                        return UbIconType.Flip;
                    }

                    if (HasSample(HitSampleInfo.HIT_WHISTLE))
                    {
                        return UbIconType.Dodge;
                    }

                    return UbIconType.Note;
                }
            }

            return UbIconType.Note;
        }

        public List<UbIconType> InferObjectModifierIcons()
        {
            var icons = new List<UbIconType>();

            if (hitObject is ManiaHitObject maniaHitObject)
            {
                int column = maniaHitObject.Column;

                if (HasMainBank(HitSampleInfo.BANK_DRUM))
                {
                    // Cop

                    if (HasSample(HitSampleInfo.HIT_WHISTLE) && HasSample(HitSampleInfo.HIT_CLAP))
                    {
                        icons.Add(UbIconType.ModCop4);
                    }
                    else if (HasSample(HitSampleInfo.HIT_CLAP))
                    {
                        icons.Add(UbIconType.ModCop3);
                    }
                    else if (HasSample(HitSampleInfo.HIT_WHISTLE))
                    {
                        icons.Add(UbIconType.ModCop2);
                    }
                    else
                    {
                        icons.Add(UbIconType.ModCop1);
                    }

                    if (HasAdditionBank(HitSampleInfo.BANK_NORMAL))
                    {
                        icons.Add(UbIconType.ModCopHeavy);
                    }

                    if (HasSample(HitSampleInfo.HIT_FINISH))
                    {
                        icons.Add(UbIconType.ModCopFinish);
                    }

                    return icons; // Exit earlier, dont add other icons
                }

                if (HasSample(HitSampleInfo.HIT_CLAP))
                {
                    if (column == 4)
                    {
                        icons.Add(UbIconType.ModSwapImmediate);
                    }
                    else
                    {
                        icons.Add(UbIconType.ModInvisible);
                    }
                }

                if (GetMainSample().Bank == HitSampleInfo.BANK_SOFT)
                {
                    icons.Add(UbIconType.ModFlying);
                }
            }

            return icons;
        }
    }
}
