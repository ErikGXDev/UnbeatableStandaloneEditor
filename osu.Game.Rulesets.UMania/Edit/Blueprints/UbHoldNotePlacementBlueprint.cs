// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;

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

        [Resolved]
        private UnbeatableHitObjectComposer? composer { get; set; }

        protected override bool IsValidForPlacement => base.IsValidForPlacement && columns.Contains(HitObject.Column);

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
