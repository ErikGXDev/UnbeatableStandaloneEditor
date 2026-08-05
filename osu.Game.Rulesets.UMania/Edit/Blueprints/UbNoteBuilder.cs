// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Extensions;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit.Components.TernaryButtons;

namespace osu.Game.Rulesets.UMania.Edit.Blueprints
{
    public class UbNoteBuilder
    {
        public HitObject HitObject;
        
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
            this.HitObject = hitObject;
        }
        
        public void ChangeHitObject(HitObject newHitObject)
        {
            HitObject = newHitObject;
        }
        
        public bool HasHitObject => HitObject != null;

        static bool isModActive(DrawableTernaryButton modButton)
        {
            return modButton.Current.Value == TernaryState.True && modButton.Enabled.Value;
        }

        public void ApplySamples(List<string> samples)
        {
            var hitSamples = new List<HitSampleInfo>();

            hitSamples = HitObject.Samples.ToList();

            foreach (string sample in samples)
            {
                // EditorAutoBank must be false so the encoder preserves the explicit bank
                HitSampleInfo sampleInfo = HitObject.CreateHitSampleInfo(sample).With(newEditorAutoBank: false);
                hitSamples.Add(sampleInfo);
            }

            HitObject.Samples = hitSamples;
        }

        public void ApplyModifierSample(DrawableTernaryButton modButton, string sample)
        {
            if (isModActive(modButton))
            {
                HitSampleInfo sampleInfo = HitObject.CreateHitSampleInfo(sample).With(newVolume: 100, newEditorAutoBank: false);
                HitObject.Samples.Add(sampleInfo);
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

            bool hasAdditionSample = HitObject.Samples.Any(s => s.Name != HitSampleInfo.HIT_NORMAL);
            if (!hasAdditionSample)
                HitObject.Samples.Add(HitObject.CreateHitSampleInfo(HitSampleInfo.HIT_FLOURISH).With(newVolume: 100, newEditorAutoBank: false));

            ApplyAdditionBank(HitSampleInfo.BANK_NORMAL);
        }

        public void ApplyMainBank(string bank)
        {
            var normalSample = HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);

            if (normalSample == null)
            {
                HitObject.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_NORMAL, bank, string.Empty, 100, false));
                return;
            }

            var index = HitObject.Samples.IndexOf(normalSample);

            HitObject.Samples[index] = new HitSampleInfo(normalSample.Name,
                bank,
                normalSample.Suffix,
                100,
                false);
        }

        public void ApplyAdditionBank(string bank)
        {
            var additionSamples = HitObject.Samples.Where(s => s.Name != HitSampleInfo.HIT_NORMAL);

            foreach (var additionSample in additionSamples.ToList())
            {
                var index = HitObject.Samples.IndexOf(additionSample);

                HitObject.Samples[index] = new HitSampleInfo(additionSample.Name,
                    bank,
                    additionSample.Suffix,
                    additionSample.Volume,
                    false);
            }
        }

       
        public void SetNormalBankIndex(int bankIndex)
        {
            var normalSample = HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);
            if (normalSample == null) return;

            var index = HitObject.Samples.IndexOf(normalSample);
            
            var existingAddIndex = (normalSample as ConvertHitObjectParser.LegacyHitSampleInfo)?.RawLegacyAddBankIndex;
            
            HitObject.Samples[index] = SetLegacyBankIndex(normalSample, legacyBankIndex: bankIndex, legacyAddBankIndex: existingAddIndex);
        }

        
        public void SetAdditionBankIndex(int bankIndex)
        {
            var normalSample = HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);
            
            if (normalSample != null)
            {
                var normalIndex = HitObject.Samples.IndexOf(normalSample);
                HitObject.Samples[normalIndex] = SetLegacyBankIndex(normalSample, legacyBankIndex: (normalSample as ConvertHitObjectParser.LegacyHitSampleInfo)?.RawLegacyBankIndex, legacyAddBankIndex: bankIndex);
            }
            
            
        }

        
        public void SetCustomSampleBank(int customBankIndex)
        {
            var normalSample = HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);
            if (normalSample == null) return;

            var index = HitObject.Samples.IndexOf(normalSample);
            string? newSuffix = customBankIndex >= 2 ? customBankIndex.ToString() : null;
            bool newUseBeatmapSamples = customBankIndex >= 1;

            HitObject.Samples[index] = SetLegacyBankIndex(
                normalSample.With(newSuffix: newSuffix, newUseBeatmapSamples: newUseBeatmapSamples),
                legacyBankIndex: (normalSample as ConvertHitObjectParser.LegacyHitSampleInfo)?.RawLegacyBankIndex,
                legacyAddBankIndex: (normalSample as ConvertHitObjectParser.LegacyHitSampleInfo)?.RawLegacyAddBankIndex,
                customSampleBank: customBankIndex);
        }

        
        public void SetVolume(int volume)
        {
            var normalSample = HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);
            if (normalSample == null) return;

            var index = HitObject.Samples.IndexOf(normalSample);
            HitObject.Samples[index] = SetLegacyBankIndex(
                normalSample.With(newVolume: volume),
                legacyBankIndex: (normalSample as ConvertHitObjectParser.LegacyHitSampleInfo)?.RawLegacyBankIndex,
                legacyAddBankIndex: (normalSample as ConvertHitObjectParser.LegacyHitSampleInfo)?.RawLegacyAddBankIndex,
                customSampleBank: (normalSample as ConvertHitObjectParser.LegacyHitSampleInfo)?.CustomSampleBank);
            
            var otherSamples = HitObject.Samples.Where(s => s.Name != HitSampleInfo.HIT_NORMAL).ToList();
            foreach (var otherSample in otherSamples)
            {
                var otherIndex = HitObject.Samples.IndexOf(otherSample);
                HitObject.Samples[otherIndex] = SetLegacyBankIndex(
                    otherSample.With(newVolume: volume),
                    legacyBankIndex: (otherSample as ConvertHitObjectParser.LegacyHitSampleInfo)?.RawLegacyBankIndex,
                    legacyAddBankIndex: (otherSample as ConvertHitObjectParser.LegacyHitSampleInfo)?.RawLegacyAddBankIndex,
                    customSampleBank: (otherSample as ConvertHitObjectParser.LegacyHitSampleInfo)?.CustomSampleBank);
            }
        }

        
        public int GetNormalBankIndex()
        {
            return (HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL) as ConvertHitObjectParser.LegacyHitSampleInfo)?.RawLegacyBankIndex ?? 0;
        }

       
        public int GetAdditionBankIndex()
        {
            var normalSample = HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL) as ConvertHitObjectParser.LegacyHitSampleInfo;
            
            if (normalSample != null && normalSample.RawLegacyAddBankIndex.HasValue)
                return normalSample.RawLegacyAddBankIndex.Value;
            
            return (HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL) as ConvertHitObjectParser.LegacyHitSampleInfo)?.RawLegacyAddBankIndex ?? 0;
        }

      
        public int GetCustomSampleBank()
        {
            return (HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL) as ConvertHitObjectParser.LegacyHitSampleInfo)?.CustomSampleBank ?? 0;
        }

        
        public int GetVolume()
        {
            return HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL)?.Volume ?? 100;
        }

        private static ConvertHitObjectParser.LegacyHitSampleInfo SetLegacyBankIndex(
            HitSampleInfo sample, int? legacyBankIndex = null, int? legacyAddBankIndex = null, int? customSampleBank = null)
        {
            if (sample is ConvertHitObjectParser.LegacyHitSampleInfo legacy)
            {
                return new ConvertHitObjectParser.LegacyHitSampleInfo(
                    legacy.Name, legacy.Bank, legacy.Volume, legacy.EditorAutoBank,
                    customSampleBank ?? legacy.CustomSampleBank, legacy.IsLayered,
                    rawLegacyBankIndex: legacyBankIndex,
                    rawLegacyAddBankIndex: legacyAddBankIndex);
            }

            return new ConvertHitObjectParser.LegacyHitSampleInfo(
                sample.Name, sample.Bank, sample.Volume, sample.EditorAutoBank,
                customSampleBank: customSampleBank ?? 0, isLayered: false,
                rawLegacyBankIndex: legacyBankIndex,
                rawLegacyAddBankIndex: legacyAddBankIndex);
        }
        
        public void Recompute(List<string> baseSamples, string baseBank)
        {
            HitObject.Samples.Clear();
            ApplySamples(baseSamples);
            ApplyMainBank(baseBank);
        }

        public HitSampleInfo GetMainSample()
        {
            var normalSample = HitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);

            return normalSample ?? new HitSampleInfo(HitSampleInfo.HIT_NORMAL, "normal", string.Empty, 100);
        }
        
        public HitSampleInfo GetAdditionSample()
        {
            var normalSample = HitObject.Samples.FirstOrDefault(s => s.Name != HitSampleInfo.HIT_NORMAL);

            return normalSample ?? new HitSampleInfo(HitSampleInfo.HIT_NORMAL, "normal", string.Empty, 100);
        }

        public bool HasSample(string sample)
        {
            return HitObject.Samples.Any(s => s.Name == sample);
        }

        public bool HasMainBank(string bank)
        {
            return HitObject.Samples.Any(s => s.Name == HitSampleInfo.HIT_NORMAL && s.Bank == bank);
        }

        public bool HasAdditionBank(string bank)
        {
            return HitObject.Samples.Any(s => s.Name != HitSampleInfo.HIT_NORMAL && s.Bank == bank);
        }

        public UbIconType InferObjectTypeIcon()
        {
            if (HitObject is ManiaHitObject maniaHitObject)
            {
                int column = maniaHitObject.Column;

                if (HasMainBank(HitSampleInfo.BANK_DRUM))
                {
                    return UbIconType.Brawl;
                }

                if (HitObject is HeadNote or HoldNote)
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

                if (HitObject is Note)
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

            if (HitObject is ManiaHitObject maniaHitObject)
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
