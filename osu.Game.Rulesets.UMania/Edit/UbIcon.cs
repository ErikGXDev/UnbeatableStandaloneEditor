// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Game.Extensions;
using osu.Game.Rulesets;
using osu.Game.Rulesets.UMania;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit
{
    public partial class UbIcon : Sprite
    {
        private static readonly Dictionary<UbIconType, Texture> cachedTextures = new Dictionary<UbIconType, Texture>();

        private static TextureStore? sharedTextureStore;
        private static IResourceStore<byte[]>? sharedResources;

        private readonly UbIconType iconType;

        public bool ForShow = false;

        public UbIcon(UbIconType iconType)
        {
            this.iconType = iconType;
        }

        [Resolved]
        private Editor editor { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(GameHost host, IRenderer renderer)
        {
            string iconName = "Textures/" + iconType.ToString().ToKebabCase();

            lock (cachedTextures)
            {
                if (!cachedTextures.TryGetValue(iconType, out var texture))
                {
                    if (sharedTextureStore == null)
                    {
                        sharedResources = editor.Ruleset.Value.CreateInstance().CreateResourceStore();
                        sharedTextureStore = new TextureStore(renderer, host.CreateTextureLoaderStore(sharedResources), false);
                    }

                    texture = sharedTextureStore.Get(iconName)!;
                    cachedTextures[iconType] = texture;
                }

                Texture = texture;
            }

            Colour = Colour4.White;
            Blending = BlendingParameters.Inherit;
        }

        public override bool UpdateSubTree()
        {
            if (ForShow) return base.UpdateSubTree();

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
