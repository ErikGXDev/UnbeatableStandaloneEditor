// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
// Based on osu.Game.Graphics.UserInterfaceV2.FormButton, extended to optionally host a
// second (nested) action button on the right.

using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.UMania.Edit.Setup
{
    public partial class UbFormButton : CompositeDrawable
    {
        
        public LocalisableString Caption { get; init; }

        public LocalisableString ButtonText { get; set; }

        public IconUsage ButtonIcon { get; init; } = FontAwesome.Solid.ChevronRight;

        public LocalisableString SecondButtonText { get; set; }

        public Action? Action { get; set; }

        public Action? SecondAction { get; set; }

        private bool secondButtonVisible;

        public bool SecondButtonVisible
        {
            get => secondButtonVisible;
            set
            {
                if (secondButtonVisible == value)
                    return;

                secondButtonVisible = value;
                updateLayout();
            }
        }

        public readonly BindableBool Enabled = new BindableBool(true);

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private FormControlBackground background = null!;
        private OsuTextFlowContainer text = null!;
        private Button button = null!;
        private Button secondButton = null!;
        private GridContainer buttonRow = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            button = new Button
            {
                Action = () => Action?.Invoke(),
                Text = ButtonText,
                Icon = ButtonIcon,
                RelativeSizeAxes = Axes.X,
                Enabled = { BindTarget = Enabled },
            };

            secondButton = new Button
            {
                Action = () => SecondAction?.Invoke(),
                Text = SecondButtonText,
                RelativeSizeAxes = Axes.X,
                Enabled = { BindTarget = Enabled },
            };

            buttonRow = new GridContainer
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, 0.5f), new Dimension(GridSizeMode.Relative, 0.5f) },
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                Content = new[]
                {
                    new Drawable[] { button, secondButton },
                },
            };

            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 5,
                CornerExponent = 2.5f,
                Children = new Drawable[]
                {
                    background = new FormControlBackground(),
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding
                        {
                            Left = 9,
                            Right = 5,
                            Vertical = 5,
                        },
                        Children = new Drawable[]
                        {
                            text = new OsuTextFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = Caption,
                            },
                            buttonRow,
                        },
                    },
                }
            };

            updateLayout();
        }

        private void updateLayout()
        {
            bool hasSecond = SecondAction != null && secondButtonVisible;

            secondButton.Alpha = hasSecond ? 1f : 0f;
            button.Text = hasSecond ? "From start" : ButtonText;

            if (ButtonText == default && !hasSecond)
            {
                text.Padding = new MarginPadding { Right = 100 };
                buttonRow.RelativeSizeAxes = Axes.None;
                buttonRow.Width = 90;
                button.RelativeSizeAxes = Axes.None;
                button.Width = 90;
            }
            else
            {
                text.Width = 0.55f;
                text.Padding = new MarginPadding { Right = 10 };

                buttonRow.RelativeSizeAxes = Axes.X;
                button.RelativeSizeAxes = Axes.X;
                buttonRow.Width = 0.45f;

                if (hasSecond)
                {
                    buttonRow.ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, 0.5f), new Dimension(GridSizeMode.Relative, 0.5f) };
                    button.Width = 1f;
                    button.Padding = new MarginPadding { Right = 3 };
                    secondButton.Width = 1f;
                    secondButton.Padding = new MarginPadding { Left = 3 };
                    secondButton.Alpha = 1f;
                }
                else
                {
                    buttonRow.ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, 1f) };
                    button.Width = 1f;
                    button.Padding = new MarginPadding();
                    secondButton.Width = 0f;
                    secondButton.Alpha = 0f;
                }
            }
        }
        
        public void SetSecondButtonText(LocalisableString text) => secondButton.Text = text;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Enabled.BindValueChanged(_ => updateState(), true);
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateState();
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);
            updateState();
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (Enabled.Value)
            {
                background.Flash();
                button.TriggerClick();
            }

            return true;
        }

        private void updateState()
        {
            text.Colour = Enabled.Value ? colourProvider.Content1 : colourProvider.Background1;

            if (!Enabled.Value)
                background.VisualStyle = VisualStyle.Disabled;
            else if (IsHovered)
                background.VisualStyle = VisualStyle.Hovered;
            else
                background.VisualStyle = VisualStyle.Normal;

            // TODO: Support BackgroundColour?
        }

        public partial class Button : OsuButton
        {
            private TrianglesV2? triangles { get; set; }

            protected override float HoverLayerFinalAlpha => 0;

            private Color4? triangleGradientSecondColour;

            public override Color4 BackgroundColour
            {
                get => base.BackgroundColour;
                set
                {
                    base.BackgroundColour = value;
                    triangleGradientSecondColour = BackgroundColour.Lighten(0.2f);
                    updateColours();
                }
            }

            public IconUsage Icon { get; init; }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider overlayColourProvider)
            {
                DefaultBackgroundColour = overlayColourProvider.Colour3;
                triangleGradientSecondColour ??= DefaultBackgroundColour.Lighten(0.2f);

                if (Text == default)
                {
                    Add(new SpriteIcon
                    {
                        Icon = Icon,
                        Size = new Vector2(16),
                        Shadow = true,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    });
                }
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Content.CornerRadius = 4;

                Add(triangles = new TrianglesV2
                {
                    Thickness = 0.02f,
                    SpawnRatio = 0.6f,
                    RelativeSizeAxes = Axes.Both,
                    Depth = float.MaxValue,
                });

                updateColours();
            }

            private void updateColours()
            {
                if (triangles == null)
                    return;

                Debug.Assert(triangleGradientSecondColour != null);

                triangles.Colour = ColourInfo.GradientVertical(triangleGradientSecondColour.Value, BackgroundColour);
            }

            protected override bool OnHover(HoverEvent e)
            {
                Debug.Assert(triangleGradientSecondColour != null);

                Background.FadeColour(triangleGradientSecondColour.Value, 300, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                Background.FadeColour(BackgroundColour, 300, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
