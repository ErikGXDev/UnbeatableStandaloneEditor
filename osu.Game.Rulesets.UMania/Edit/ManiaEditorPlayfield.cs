// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.UMania.Beatmaps;
using osu.Game.Rulesets.UMania.UI;

namespace osu.Game.Rulesets.UMania.Edit
{
    public partial class ManiaEditorPlayfield : ManiaPlayfield
    {
        public ManiaEditorPlayfield(List<StageDefinition> stages)
            : base(stages)
        {
        }

        protected override Stage CreateStage(int firstColumnIndex, StageDefinition stageDefinition, ref ManiaAction columnAction)
            => new EditorStage(firstColumnIndex, stageDefinition, ref columnAction);
    }
}
