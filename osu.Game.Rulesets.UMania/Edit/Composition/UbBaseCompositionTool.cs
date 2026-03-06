// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Edit.Tools;

namespace osu.Game.Rulesets.UMania.Edit.Composition
{
    public abstract partial class UbBaseCompositionTool : CompositionTool
    {
        public readonly List<string> HitSamples;
        public readonly List<int> Columns;
        public readonly string Bank;
        public readonly UbIconType Icon;

        public UbBaseCompositionTool(string name, UbIconType icon, List<int> columns, List<string> hitSamples, string bank)
            : base(name)
        {
            this.HitSamples = hitSamples;
            this.Columns = columns;
            this.Bank = bank;
            this.Icon = icon;
        }

        public override Drawable CreateIcon() => new UbIcon(Icon);
    }
}
