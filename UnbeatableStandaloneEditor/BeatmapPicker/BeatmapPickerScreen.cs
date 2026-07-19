using System.ComponentModel;
using System.Threading.Tasks;
using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Screens;
using osu.Game.Screens.Select;
using osuTK;
using osuTK.Graphics;
using UnbeatableStandaloneEditor.Import;
using UnbeatableStandaloneEditor.Settings;
using Container = osu.Framework.Graphics.Containers.Container;

namespace UnbeatableStandaloneEditor.BeatmapPicker;

public partial class BeatmapPickerScreen : OsuScreen
{
    [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;
    [Resolved] private IAPIProvider api { get; set; } = null!;
    [Resolved] private RealmAccess realm { get; set; } = null!;
    [Resolved] private IDialogOverlay? dialogOverlay { get; set; }

    private readonly Bindable<BeatmapSetInfo?> selectedSet = new();

    private FillFlowContainer setsFlow = null!;
    private RoundedButton editButton = null!;
    private RoundedButton deleteButton = null!;
    private RoundedButton? updateButton;
    private SortButton sortByButton = null!;

    private OsuClickableContainer versionText = null!;
    private Container? updateButtonContainer;


    [Cached] private OverlayColourProvider colours = new(OverlayColourScheme.Aquamarine);

    public override bool AllowUserExit => false;

    [BackgroundDependencyLoader]
    private void load()
    {
        InternalChildren =
        [
            new Box { RelativeSizeAxes = Axes.Both, Colour = colours.Background5 },
            new Container
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.Y,
                Width = 840,
                Padding = new MarginPadding { Vertical = 16 },
                Children =
                [
                    // Header
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 34,
                        Children =
                        [
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = "Your Beatmaps",
                                Font = OsuFont.GetFont(size: 20, weight: FontWeight.Bold),
                            },
                            sortByButton = new SortButton(),
                            new RoundedButton
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Width = 148,
                                Height = 32,
                                Text = "+ New Beatmap",
                                Action = createNewBeatmap,
                            }
                        ],
                    },
                    // Scrollable list
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 44, Bottom = 54 },
                        Masking = true,
                        Child = new OsuScrollContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = setsFlow = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 3),
                                Padding = new MarginPadding { Right = 10, Bottom = 10 },
                            },
                        },
                    },
                    // Footer
                    new Container
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        RelativeSizeAxes = Axes.X,
                        Height = 44,
                        Masking = true,
                        CornerRadius = 8,
                        Children =
                        [
                            new Box { RelativeSizeAxes = Axes.Both, Colour = colours.Background4 },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Horizontal = 10 },
                                Children =
                                [
                                    deleteButton = new RoundedButton
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Width = 36,
                                        Height = 32,
                                        BackgroundColour = new Color4(170, 50, 50, 255),
                                        Action = promptDelete,
                                    },
                                    editButton = new RoundedButton
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Width = 148,
                                        Height = 32,
                                        Text = "Edit Beatmap",
                                        Action = openEditor,
                                    }
                                ],
                            }
                        ],
                    }
                ],
            },
            new MenuPopoverContainer()
            {
                new FillFlowContainer()
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,

                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(8, 0),
                    Padding = new MarginPadding { Right = 10, Top = 10 },
                    Children = [
                        new SettingsButton(),
                        new ImportButton()
                    ]
                }
            },
            // Version and update button at bottom right
            updateButtonContainer = new Container
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Padding = new MarginPadding { Right = 16, Bottom = 16 },
                Width = 80,
                AutoSizeAxes = Axes.Y,
                Children =
                [
                    versionText = new OsuClickableContainer
                    {
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                        Y = -8,
                        AutoSizeAxes = Axes.Both,
                        Action = openGitHubRepo,
                        Child = new OsuSpriteText
                        {
                            Text = $"v{AppVersion.Current}",
                            Font = OsuFont.GetFont(size: 16),
                            Colour = colours.Highlight1,
                            Alpha = 0.6f,
                        }
                    }
                ]
            }
        ];
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        deleteButton.Add(new SpriteIcon
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Size = new Vector2(14),
            Icon = FontAwesome.Solid.Trash,
            Depth = -1,
        });

        selectedSet.BindValueChanged(v =>
        {
            bool has = v.NewValue != null;
            editButton.Enabled.Value = has;
            deleteButton.Enabled.Value = has;
        }, true);

        // Check for updates asynchronously
        Task.Run(async () =>
        {
            var update = await VersionCheckService.CheckForUpdateAsync();
            if (update != null)
            {
                Schedule(() =>
                {
                    updateButton = new RoundedButton
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Width = 180,
                        Height = 28,
                        Scale = new Vector2(0f),
                        Y = 30,
                        Colour = colours.Colour1,
                        BackgroundColour = colours.Background3,
                        Text = "New version available!",
                        Action = () => openUpdateRelease(update),
                    };
                    versionText.Y = -36;
                    updateButtonContainer!.Add(updateButton);
                    Schedule(() =>
                    {
                        updateButton.ScaleTo(1f, 800, Easing.OutElastic);

                    });
                });
            }
        });

        // Load all the beatmap sets
        realm.RegisterForNotifications(
            r => r.All<BeatmapSetInfo>().Where(s => !s.DeletePending),
            (sets, _) =>
            {
                buildFlowFromSets(sets.ToList());
            }
        );

        sortByButton.CurrentSortMode.BindValueChanged(v =>
        {
            rebuildBeatmapList();
        });

    }

    private void rebuildBeatmapList()
    {
        realm.Run(r =>
        {
            var sets = r.All<BeatmapSetInfo>().Where(s => !s.DeletePending);
            buildFlowFromSets(sets.ToList());
        });
    }

    private void buildFlowFromSets(List<BeatmapSetInfo> sets)
    {

        var prevId = selectedSet.Value?.ID;
        setsFlow.Clear();

        if (!sets.Any())
        {
            sortByButton.Alpha = 0;
            setsFlow.Add(new EmptyState());
            selectedSet.Value = null;
            return;
        }

        sortByButton.Alpha = 1;

        BeatmapSetInfo? newSelection = null;
        BeatmapSetInfo? firstSet = null;

        foreach (var set in sets.OrderBy(sortByButton.GetSortObject).ThenBy(s => s.Metadata.Title))
        {
            var detached = set.Detach();
            firstSet ??= detached;
            if (prevId.HasValue && detached.ID == prevId.Value)
                newSelection = detached;
            setsFlow.Add(new BeatmapSetRow(detached, selectedSet));
        }

        // If nothing was previously selected (e.g. first beatmap just created),
        // fall back to the first set so the buttons activate automatically.
        selectedSet.Value = newSelection ?? firstSet;
    }

    private void createNewBeatmap()
    {
        var ruleset = UbRuleset.GetRulesetInfo();
        var working = beatmapManager.CreateNew(ruleset, api.LocalUser.Value);

        Beatmap.Value = working;
        Ruleset.Value = ruleset;
        this.Push(new EditorLoader(true));
    }

    private void openEditor()
    {
        if (selectedSet.Value == null) return;

        // Update selectedSet.Value.DateAdded
        realm.Write(r =>
        {
            var realmSet = r.Find<BeatmapSetInfo>(selectedSet.Value.ID);

            if (realmSet != null)
            {
                realmSet.DateAdded = DateTime.Now;
            }
        });
        selectedSet.Value.DateAdded = DateTime.Now;


        var beatmap = selectedSet.Value.Beatmaps.OrderBy(b => b.StarRating).First();
        var working = beatmapManager.GetWorkingBeatmap(beatmap);
        Beatmap.Value = working;
        Ruleset.Value = UbRuleset.GetRulesetInfo();
        this.Push(new EditorLoader(false));
    }

    private void promptDelete()
    {
        if (selectedSet.Value == null) return;
        dialogOverlay?.Push(new BeatmapDeleteDialog(selectedSet.Value));
    }

    private void openGitHubRepo()
    {
        BrowserUtil.OpenUrl("https://github.com/ErikGXDev/UnbeatableStandaloneEditor");
    }

    private void openUpdateRelease(VersionCheckService.ReleaseInfo update)
    {
        BrowserUtil.OpenUrl(update.ReleaseUrl);
    }

    // For when there are no beatmaps
    private partial class EmptyState : CompositeDrawable
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
                Padding = new MarginPadding { Top = 48 },
                Children =
                [
                    new SpriteIcon
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Icon = FontAwesome.Regular.FolderOpen,
                        Size = new Vector2(40),
                        Alpha = 0.25f,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = @"No beatmaps yet. Click ""+ New Beatmap"" to get started.",
                        Font = OsuFont.GetFont(size: 16),
                        Alpha = 0.4f,
                    }
                ],
            };
        }
    }
}
