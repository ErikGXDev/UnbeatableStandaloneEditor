using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Game;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Containers.Markdown;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;

namespace UnbeatableStandaloneEditor.Update;

public partial class UpdatePopup : OsuFocusedOverlayContainer
{
    [Cached] private OverlayColourProvider colours = new(OverlayColourScheme.Aquamarine);

    private Container contentContainer = null!;
    private OsuMarkdownContainer updateInfoText = null!;
    private OsuSpriteText updateStatusText = null!;
    private OsuSpriteText updateHeaderText = null!;

    protected override bool BlockNonPositionalInput { get; } = true;

    protected override bool BlockPositionalInput { get; } = true;

    [BackgroundDependencyLoader]
    private void load()
    {
        RelativeSizeAxes = Axes.Both;

        Children =
        [
            new Box { RelativeSizeAxes = Axes.Both, Colour = colours.Background5.Darken(0.5f), Alpha = 0.7f },
            contentContainer = new Container {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 0.5f,
                Masking = true,
                CornerRadius = 10,
                Children = [
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colours.Background4,
                    },
                    new FillFlowContainer()
                    {
                        Padding = new MarginPadding(20),
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new(0, 15),
                        Children =
                        [
                            updateHeaderText = new OsuSpriteText()
                            {
                                Text = "New Update Available!",
                                Font = OsuFont.Default.With(size: 30, weight: FontWeight.Bold),
                                Colour = colours.Content1
                            },
                            new Container()
                            {
                                Masking = true,
                                CornerRadius = 10,
                                AutoSizeAxes = Axes.Y,
                                RelativeSizeAxes = Axes.X,
                                Children = [
                                    new Box()
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colours.Background3,
                                    },
                                    new OsuScrollContainer()
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 300,
                                        Padding = new MarginPadding(10),
                                        Children = [
                                            updateInfoText = new OsuMarkdownContainer()
                                            {
                                                Margin = new MarginPadding() { Top = 10, Bottom = 10 },
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Text = "Loading changelog...",
                                                Colour = colours.Content1
                                            },
                                        ]
                                    },
                                    ]
                            },
                            new Container()
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = [
                                    updateStatusText = new OsuSpriteText()
                                    {
                                        Margin = new MarginPadding() { Bottom = 10, Top = 10 },
                                        Text = "Downloading...",
                                        Font = OsuFont.Default.With(size: 16),
                                        Anchor = Anchor.BottomCentre,
                                        Origin = Anchor.BottomCentre,
                                        Colour = colours.Content2
                                    }
                                ]
                            }
                        ]
                    },


                ]
            }
        ];

        using (BeginDelayedSequence(0))
        {
            updateStatusText.FadeTo(0.2f).FadeTo(1, 1000).Then().FadeTo(0.2f, 1000).Loop();
        }
    }

    public void SetReleaseInfo(VersionCheckService.ReleaseInfo releaseInfo)
    {
        if (releaseInfo == null) return;

        updateInfoText.Text = releaseInfo.Changelog.TrimStart('\uFEFF');
        updateHeaderText.Text = $"New Update Available! - v{releaseInfo.Version}";
        updateStatusText.Text = $"Downloading version {releaseInfo.Version} for you...";

        // Start download directly after the popup is shown
            var progress = new Progress<double>(p =>
        {
                int percent = (int)(p * 100);
                Schedule(() => updateStatusText.Text = $"Downloading version {releaseInfo.Version}... {percent}%");
            });

            Task.Run(async () =>
            {
                try
                {
                    await Updater.FullDownload(exitGame, progress);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error downloading update: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                    Schedule(() => updateStatusText.Text = $"Error downloading update (This error has been logged): {ex.Message}");
                    await Task.Delay(5000).ContinueWith(_ =>
                    {
                        Hide();
                    });
                }
            });
        }

    [Resolved] OsuGameBase game { get; set; } = null!;

    private void exitGame()
    {
        game.AttemptExit();
    }

    protected override void PopIn()
    {
        this.FadeIn(200, Easing.OutQuint);
        contentContainer.ScaleTo(0.9f).Then().ScaleTo(1f, 500, Easing.OutBounce);
    }

    protected override void PopOut()
    {
        this.FadeOut(200, Easing.OutQuint);
    }
}
