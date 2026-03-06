// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace osu.Game.Rulesets.UMania
{
    public partial class URulesetIcon : Sprite
    {
        private readonly Ruleset ruleset;

        public URulesetIcon(Ruleset ruleset)
        {
            this.ruleset = ruleset;

            Margin = new MarginPadding { Top = 3 };
        }

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            var textureStore = new TextureStore(renderer, new TextureLoaderStore(ruleset.CreateResourceStore()), false);

            Texture = textureStore.Get("Textures/double-icon");

        }
    }
}
