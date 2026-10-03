using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
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

public partial class EditorSettingsFooter : FillFlowContainer
{
    [BackgroundDependencyLoader]
    private void load()
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
        Direction = FillDirection.Vertical;
        Padding = new MarginPadding
        {
            Top = 20,
            Bottom = 30,
            Left = SettingsPanel.CONTENT_PADDING.Left,
            Right = SettingsPanel.CONTENT_PADDING.Right,
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
        };
    }
}
