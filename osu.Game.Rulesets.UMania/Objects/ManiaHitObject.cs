// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UMania.Edit;
using osu.Game.Rulesets.UMania.Scoring;

namespace osu.Game.Rulesets.UMania.Objects
{
    public class ManiaHitObject : HitObject, IHasColumn, IHasXPosition
    {
        private HitObjectProperty<int> column;

        public Bindable<int> ColumnBindable => column.Bindable;

        public virtual int Column
        {
            get => column.Value;
            set => column.Value = value;
        }

        protected override HitWindows CreateHitWindows() => new ManiaHitWindows();

        #region LegacyBeatmapEncoder

        float IHasXPosition.X
        {
            get => Column;
            set => Column = (int)value;
        }

        #endregion
    }
}
