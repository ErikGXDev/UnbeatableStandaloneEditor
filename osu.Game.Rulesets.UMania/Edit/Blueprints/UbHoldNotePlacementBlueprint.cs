// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Rulesets.UMania.Edit.Blueprints
{
    public partial class UbHoldNotePlacementBlueprint : HoldNotePlacementBlueprint
    {
        private readonly List<string> hitSampleInfos;
        private readonly List<int> columns;
        private readonly string mainBank;

        public UbHoldNotePlacementBlueprint(List<int> columns, List<string> hitSampleInfos, string mainBank)
        {
            this.hitSampleInfos = hitSampleInfos;
            this.columns = columns;
            this.mainBank = mainBank;
        }
        
        [BackgroundDependencyLoader]
        private void load()
        {
            if (composer.Is4Key && columns.Contains(2) && columns.Contains(3))
            {
                columns.Add(0);
                columns.Add(1);
            }
        }

        [Resolved]
        private UnbeatableHitObjectComposer composer { get; set; } = null!;

        protected override bool IsValidForPlacement => base.IsValidForPlacement &&
                                                       (composer.SettingShowAllowedColumns.Value ==
                                                           TernaryState.False || columns.Contains(HitObject.Column));

        public override void EndPlacement(bool commit)
        {
            base.EndPlacement(commit);

            if (composer == null)
                return;

            var noteHelper = new UbNoteBuilderHelper(composer, HitObject);
            noteHelper.ApplyEverything(hitSampleInfos, mainBank);
        }
    }
}
