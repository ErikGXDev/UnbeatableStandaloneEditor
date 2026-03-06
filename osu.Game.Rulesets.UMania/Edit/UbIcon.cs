// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Extensions;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit
{
    public partial class UbIcon : Sprite
    {
        private readonly UbIconType iconType;

        public UbIcon(UbIconType iconType)
        {
            this.iconType = iconType;
        }

        [Resolved]
        private Editor editor { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(TextureStore textureStore, IRenderer renderer)
        {
            string iconName = "Textures/" + iconType.ToString().ToKebabCase();

            var ruleset = editor!.Ruleset.Value.CreateInstance();
            Texture =
                new TextureStore(renderer, new TextureLoaderStore(ruleset.CreateResourceStore()), false).Get(
                    iconName);

            Colour = Colour4.White;
            Blending = BlendingParameters.Inherit;
        }

        public override bool UpdateSubTree()
        {
            Colour = Colour4.White;
            Size = new Vector2(25);
            Blending = BlendingParameters.Inherit;
            X = 7;
            return base.UpdateSubTree();
        }
    }

    public enum UbIconType
    {
        Note,
        Hold,
        Dodge,
        Double,
        Freestyle,
        Spam,
        Brawl,

        Flip,
        Zoom,

        ModInvisible,
        ModFlying,
        ModSwapImmediate,

        ModCopFinish,
        ModCop1,
        ModCop2,
        ModCop3,
        ModCop4,
        ModCopHeavy
    }
}
