// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Rulesets.UMania.Edit.Blueprints
{
    public partial class UbNotePlacementBlueprint : NotePlacementBlueprint
    {
        private readonly List<string> hitSampleInfos;
        private readonly List<int> columns;
        private readonly string mainBank;

        public UbNotePlacementBlueprint(List<int> columns, List<string> hitSampleInfos, string mainBank)
        {
            this.hitSampleInfos = hitSampleInfos;
            this.columns = columns;
            this.mainBank = mainBank;
        }

        [Resolved]
        private UnbeatableHitObjectComposer composer { get; set; } = null!;

        protected override bool IsValidForPlacement => base.IsValidForPlacement &&
                                                       (composer.SettingShowAllowedColumns.Value ==
                                                           TernaryState.False || columns.Contains(HitObject.Column));

        public override void EndPlacement(bool commit)
        {
            base.EndPlacement(commit);
            var noteHelper = new UbNoteBuilderHelper(composer, HitObject);
            noteHelper.ApplyEverything(hitSampleInfos, mainBank);
        }
    }
}
