using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osuTK;
using UnbeatableStandaloneEditor.Components;
using OsuSpriteText = osu.Game.Graphics.Sprites.OsuSpriteText;

namespace UnbeatableStandaloneEditor.Settings;

public partial class EditorSettingsHeader : Container
{
    private readonly LocalisableString heading = "Settings";
    private readonly LocalisableString subheading = "Adjust how the editor behaves.";

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider)
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;

        Children = new Drawable[]
        {
            new OsuTextFlowContainer
            {
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Padding = new MarginPadding
                {
                    Left = SettingsPanel.CONTENT_PADDING.Left,
                    Right = SettingsPanel.CONTENT_PADDING.Right,
                    Vertical = 20,
                }
            }.With(flow =>
            {
                flow.AddText(heading, header => header.Font = OsuFont.Default.With(size: 40));
                flow.NewLine();
                flow.AddText(subheading, subheading => subheading.Font = OsuFont.Default.With(size: 14));
            }),
        };
    }
}

public partial class ExpandableCredits : FillFlowContainer
{
    private bool expanded;

    public ExpandableCredits()
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
        Direction = FillDirection.Vertical;
        Spacing = new Vector2(0, 5);
    }

    private OsuTextFlowContainer creditsTextFlow = null!;

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider)
    {
        var button = new BlankButton()
        {
            Width = 100,
            Height = 28,
            Text = "Credits",
            Anchor = Anchor.TopCentre,
            Origin = Anchor.TopCentre,
            Colour = colourProvider.Background5,
            BackgroundColour = colourProvider.Background4,
            Scale = new Vector2(0.75f),
            Margin = new MarginPadding() { Top = 8 },
            Action = toggleExpand,
        };

        creditsTextFlow = new OsuTextFlowContainer(t =>
        {
            t.Font = OsuFont.GetFont(size: 11, weight: FontWeight.Regular);
        })
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Text = "Created by Erik / ErikGXDev, with the help of the UNBEATABLE Modding and Charting Community, open source and free, under MIT license.\nThis editor uses osu!framework, osu! and other open-source libraries. Not affiliated with D-CELL, Playstack, or ppy. See the included CREDITS file for more information.\nAudio Engine: FMOD Studio by Firelight Technologies Pty Ltd. / BASS",
            Colour = colourProvider.Content1.Opacity(0.7f),
            Padding = new MarginPadding { Vertical = 8, Horizontal = 16},
            Alpha = 0,
        };

        Children = new Drawable[]
        {
            new OsuSpriteText
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Text = "Unbeatable Standalone Editor",
                Font = OsuFont.GetFont(size: 18, weight: FontWeight.Bold),
            },
            new OsuSpriteText
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Text = $"v{AppVersion.Current}",
                Font = OsuFont.GetFont(size: 16),
            },
            button,
            creditsTextFlow,
            dummy = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 0,
            }
        };

    }

    private Container dummy;

    private void toggleExpand()
    {
        expanded = !expanded;

        creditsTextFlow.FadeTo(expanded ? 1 : 0, 200, Easing.OutQuint);

        // Scroll to bottom to show the expanded credits
        if (expanded)
        {
            ScheduleAfterChildren(() =>
            {
                var panel = this.FindClosestParent<EditorSettingsOverlay>();
                panel?.SectionsContainer.ScrollTo(dummy);
            });
        }
    }
}

public partial class EditorSettingsFooter : FillFlowContainer
{
    [BackgroundDependencyLoader]
    private void load()
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
        Direction = FillDirection.Vertical;
        Children = new Drawable[]
        {
            new ExpandableCredits
            {
                RelativeSizeAxes = Axes.X,
                Margin = new MarginPadding() { Vertical = 10 }
            },
        };
    }
}
