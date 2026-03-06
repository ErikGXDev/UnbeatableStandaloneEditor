// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using HarmonyLib;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Configuration;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring.Legacy;
using osu.Game.Rulesets.UMania.Beatmaps;
using osu.Game.Rulesets.UMania.Mods;
using osu.Game.Rulesets.UMania.UI;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UMania.Configuration;
using osu.Game.Rulesets.UMania.Difficulty;
using osu.Game.Rulesets.UMania.Edit;
using osu.Game.Rulesets.UMania.Edit.Setup;
using osu.Game.Rulesets.UMania.Skinning.Argon;
using osu.Game.Screens.Edit.Setup;
using osu.Game.Skinning;
using osu.Game.Rulesets.UMania.Patches;

namespace osu.Game.Rulesets.UMania
{
    public class UManiaRuleset : Ruleset, ILegacyRuleset
    {
        public const int MAX_STAGE_KEYS = 10;
        public const string SHORT_NAME = "mania";


        private static bool hasPatched;
        public UManiaRuleset()
        {
            try
            {

                if (!hasPatched)
                {
                    // Patch the ManiaBeatmapConverter to use UManiaRuleset instead of ManiaRuleset
                    var harmony = new Harmony("umania.ruleset.patch");
                    harmony.PatchAll();
                    hasPatched = true;
                    Logger.Log("+++ Successfully applied Harmony patches for UManiaRuleset - " + nameof(OnlinePatch));
                }
            }
            catch (Exception e)
            {
                Logger.Log("!!! Failed to apply Harmony patches for UManiaRuleset: " + e);
            }
        }

        public override string Description => "unbeatable";

        public override DrawableRuleset CreateDrawableRulesetWith(IBeatmap beatmap, IReadOnlyList<Mod> mods = null) =>
            new DrawableManiaRuleset(this, beatmap, mods);

        public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap) =>
            new ManiaBeatmapConverter(beatmap, this);

        public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) =>
            new UManiaDifficultyCalculator(RulesetInfo, beatmap);

        public override IRulesetConfigManager CreateConfig(SettingsStore? settings) => new ManiaRulesetConfigManager(settings, RulesetInfo);

        public override ISkin? CreateSkinTransformer(ISkin skin, IBeatmap beatmap)
        {
            return new ManiaArgonSkinTransformer(skin, beatmap);
        }

        public override IEnumerable<Mod> GetModsFor(ModType type)
        {
            switch (type)
            {
                case ModType.Automation:
                    return new[] { new UManiaModAutoplay() };

                default:
                    return Array.Empty<Mod>();
            }
        }

        public override string ShortName => "umania";

        public override IEnumerable<KeyBinding> GetDefaultKeyBindings(int variant = 0) => new SingleStageVariantGenerator(variant).GenerateMappings();


        public override Drawable CreateIcon() => new URulesetIcon(this);

        public override RulesetSettingsSubsection CreateSettings() => new ManiaSettingsSubsection(this);

        public override IBeatmapProcessor? CreateBeatmapProcessor(IBeatmap beatmap) => new UbProcessor(beatmap);

        // Editor setup
        public override IBeatmapVerifier? CreateBeatmapVerifier() => new ManiaBeatmapVerifier();

        public override IEnumerable<Drawable> CreateEditorSetupSections() =>
        [
            new MetadataSection(),
            new ManiaDifficultySection(),
            new ResourcesSection(),
            new DesignSection(),
            new UbExportSection() // Custom Unbeatable section for some custom features and ui
        ];

        public override HitObjectComposer CreateHitObjectComposer() => new UnbeatableHitObjectComposer(this);

        // Legacy support

        public ILegacyScoreSimulator CreateLegacyScoreSimulator() => new ManiaLegacyScoreSimulator();
        public int LegacyID => 5;


        // Leave this line intact. It will bake the correct version into the ruleset on each build/release.
        public override string RulesetAPIVersionSupported => CURRENT_RULESET_API_VERSION;
    }
}
