using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Localisation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit
{
    public partial class OrderToggleButton : ClickableContainer, IHasTooltip
    {
        private Sprite topFirstSprite = null!;
        private Sprite bottomFirstSprite = null!;

        private Sprite middleFirstSprite = null!;
        private Sprite middleSecondSprite = null!;
        private Sprite middleMixedSprite = null!;

        private bool isTopFirst;
        private bool middleMode;
        private bool middleMixed;

        public bool IsTopFirst
        {
            get => isTopFirst;
            set
            {
                if (isTopFirst == value) return;

                isTopFirst = value;
                if (IsLoaded) updateVisibility();
            }
        }
        
        public bool MiddleMode
        {
            get => middleMode;
            set
            {
                if (middleMode == value) return;

                middleMode = value;
                if (IsLoaded) updateVisibility();
            }
        }
        
        public bool MiddleMixed
        {
            get => middleMixed;
            set
            {
                if (middleMixed == value) return;

                middleMixed = value;
                if (IsLoaded) updateVisibility();
            }
        }

        public Action? OnToggle;

        [Resolved] private Editor editor { get; set; } = null!;

        public OrderToggleButton()
        {
            Size = new Vector2(36);
        }

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            var ruleset = editor.Ruleset.Value.CreateInstance();
            var textureStore = new TextureStore(renderer, new TextureLoaderStore(ruleset.CreateResourceStore()), false);

            AddRangeInternal(new Drawable[]
            {
                topFirstSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = textureStore.Get("Textures/order-1-2-top"),
                    FillMode = FillMode.Fit,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = Colour4.White,
                    Blending = BlendingParameters.Inherit,
                },
                bottomFirstSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = textureStore.Get("Textures/order-1-2-bottom"),
                    FillMode = FillMode.Fit,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = Colour4.White,
                    Blending = BlendingParameters.Inherit,
                },
                middleFirstSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = textureStore.Get("Textures/order-1-2-middle-first"),
                    FillMode = FillMode.Fit,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = Colour4.White,
                    Blending = BlendingParameters.Inherit,
                },
                middleSecondSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = textureStore.Get("Textures/order-1-2-middle-second"),
                    FillMode = FillMode.Fit,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = Colour4.White,
                    Blending = BlendingParameters.Inherit,
                },
                middleMixedSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = textureStore.Get("Textures/order-1-2-middle-mixed"),
                    FillMode = FillMode.Fit,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = Colour4.White,
                    Blending = BlendingParameters.Inherit,
                },
            });

            Action = () => OnToggle?.Invoke();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateVisibility();
        }

        private void updateVisibility()
        {
            if (middleMode)
            {
                if (middleMixed)
                {
                    middleMixedSprite.Alpha = 1f;
                    middleFirstSprite.Alpha = 0f;
                    middleSecondSprite.Alpha = 0f;
                }
                else
                {
                    middleFirstSprite.Alpha = isTopFirst ? 1f : 0f;
                    middleSecondSprite.Alpha = isTopFirst ? 0f : 1f;
                    middleMixedSprite.Alpha = 0f;
                }
                
                
                topFirstSprite.Alpha = 0f;
                bottomFirstSprite.Alpha = 0f;
            }
            else
            {
                topFirstSprite.Alpha = isTopFirst ? 1f : 0f;
                bottomFirstSprite.Alpha = isTopFirst ? 0f : 1f;

                middleFirstSprite.Alpha = 0f;
                middleSecondSprite.Alpha = 0f;
                middleMixedSprite.Alpha = 0f;
            }


        }

        protected override bool OnHover(HoverEvent e)
        {
            this.FadeTo(0.8f, 100);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            this.FadeTo(1f, 100);
            base.OnHoverLost(e);
        }

        private string getToolTipText()
        {
            if (middleMode)
            {
                if (middleMixed)
                {
                    return "Pink note is in the middle - click to fix";
                }
                
                if (isTopFirst)
                {
                    return "Pink note is first - click to swap";
                }
                else
                {
                    return "Normal notes are first - click to swap";
                }
            }
            else
            {
                if (isTopFirst)
                {
                    return "Column 3 (green) is first - click to swap";
                }
                else
                {
                    return "Column 4 (blue) is first - click to swap";
                }
            }
        }

        public LocalisableString TooltipText => getToolTipText();
    }
}
