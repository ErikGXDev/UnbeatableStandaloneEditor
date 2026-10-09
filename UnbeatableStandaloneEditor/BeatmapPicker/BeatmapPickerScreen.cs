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
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Screens;
using osu.Game.Screens.Select;
using osuTK;
using osuTK.Graphics;
using UnbeatableStandaloneEditor.Import;
using UnbeatableStandaloneEditor.Settings;
using UnbeatableStandaloneEditor.Update;
using Container = osu.Framework.Graphics.Containers.Container;

namespace UnbeatableStandaloneEditor.BeatmapPicker;

public partial class BeatmapPickerScreen : OsuScreen
{
    [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;
    [Resolved] private IAPIProvider api { get; set; } = null!;
    [Resolved] private RealmAccess realm { get; set; } = null!;
    [Resolved] private IDialogOverlay? dialogOverlay { get; set; }
    [Resolved] private EditorConfigManager config { get; set; } = null!;

    private readonly Bindable<BeatmapSetInfo?> selectedSet = new();
    private readonly Bindable<string> searchQuery = new();
    private readonly BindableBool gridViewToggle = new();

    private FillFlowContainer setsFlow = null!;
    private FillFlowContainer setsGrid = null!;
    private RoundedButton editButton = null!;
    private RoundedButton deleteButton = null!;
    private RoundedButton? updateButton;
    private SortButton sortByButton = null!;
    private SearchTextBox searchBox = null!;
    private ListGridToggle gridViewButton = null!;

    private OsuClickableContainer versionText = null!;
    private Container? updateButtonContainer;

    private UpdatePopup updatePopup = null!;


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
                            new FillFlowContainer()
                            {
                                Direction = FillDirection.Horizontal,
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Spacing = new Vector2(8, 0),
                                Children = [
                                    new RoundedButton
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Width = 148,
                                        Height = 32,
                                        Text = "+ New Beatmap",
                                        Action = createNewBeatmap,
                                    },
                                    sortByButton = new SortButton(),
                                    gridViewButton = new ListGridToggle(gridViewToggle),
                                    searchBox = new SearchBeatmaps(searchQuery)
                                ]
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
                            Children = new Drawable[]
                            {
                                setsFlow = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 3),
                                    Padding = new MarginPadding { Right = 10, Bottom = 10 },
                                    AlwaysPresent = true
                                },
                                setsGrid = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Full,
                                    Spacing = Vector2.Zero,
                                    Padding = new MarginPadding { Right = 10, Bottom = 10 },
                                    AlwaysPresent = true
                                },
                            }
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
            },
            updatePopup = new UpdatePopup(),
            new ImportDropper()
        ];

        var gridViewOn = config.Get<bool>(EditorSetting.GridViewOn);
        gridViewToggle.Value = gridViewOn;
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

        // Load all the beatmap sets
        realm.RegisterForNotifications(
            r => r.All<BeatmapSetInfo>().Where(s => !s.DeletePending),
            (_, _) =>
            {
                if (this.IsCurrentScreen())
                    rebuildBeatmapList(false);
            }
        );

        sortByButton.CurrentSortMode.BindValueChanged(v =>
        {
            rebuildBeatmapList();
        });

        searchQuery.BindValueChanged(v =>
        {
            rebuildBeatmapListDebounced();
        });

        gridViewToggle.BindValueChanged(v =>
        {
            config.SetValue(EditorSetting.GridViewOn, v.NewValue);
            rebuildBeatmapList();
        });
    }

    public override void OnEntering(ScreenTransitionEvent e)
    {
        base.OnEntering(e);

        checkForUpdates();
    }

    public override void OnResuming(ScreenTransitionEvent e)
    {
        base.OnResuming(e);

        checkForUpdates();
    }

    private double lastUpdateCheckTime = double.NegativeInfinity;

    private void checkForUpdates()
    {
        if (Time.Current - lastUpdateCheckTime < 300000 * 5) // 25 minutes
            return;

        lastUpdateCheckTime = Time.Current;

        // Check for updates asynchronously
        Task.Run(async () =>
        {
            var update = await VersionCheckService.CheckForUpdateAsync();
            if (update != null)
            {
                Schedule(() =>
                {
                    updateButton?.Expire();
                    updateButton = new UpdateButton()
                    {
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
    }

    private CancellationTokenSource? rebuildCancellation;

    private ScheduledDelegate? pendingSearchRebuild;

    private void rebuildBeatmapListDebounced()
    {
        pendingSearchRebuild?.Cancel();
        pendingSearchRebuild = Scheduler.AddDelayed(() => rebuildBeatmapList(true), 200);
    }

    private void rebuildBeatmapList(bool withAnimation = true)
    {
        rebuildCancellation?.Cancel();
        rebuildCancellation?.Dispose();

        var cancellation = new CancellationTokenSource();
        rebuildCancellation = cancellation;

        CancellationToken token = cancellation.Token;

        string query = searchQuery.Value ?? string.Empty;
        SortMode sortMode = sortByButton.CurrentSortMode.Value;
        Guid? selectedId = selectedSet.Value?.ID;

        realm.RunAsync(r =>
        {
            var sets = r.All<BeatmapSetInfo>()
                .Where(s => !s.DeletePending)
                .ToList()
                .Detach();

            if (token.IsCancellationRequested)
                return new List<BeatmapSetInfo>();

            return sets.Where(set => matchesSearch(set, query))
                .OrderBy(set => SortButton.GetSortObject(set, sortMode))
                .ThenBy(set => set.Metadata.Title)
                .ToList();
        }, token).ContinueWith(task =>
        {
            if (token.IsCancellationRequested || task.IsFaulted || task.IsCanceled)
                return;

            if (task.IsCompletedSuccessfully)
                Schedule(() => displaySets(task.Result, selectedId, withAnimation));
        }, token);
    }

    private static bool matchesSearch(BeatmapSetInfo set, string query)
    {
        if (string.IsNullOrEmpty(query))
            return true;

        query = query.Trim().ToLowerInvariant();

        return set.Metadata.Artist.ToLowerInvariant().Contains(query) ||
               set.Metadata.Title.ToLowerInvariant().Contains(query) ||
               set.Metadata.Author.Username.ToLowerInvariant().Contains(query);
    }

    private void displaySets(List<BeatmapSetInfo> sets, Guid? selectedId, bool withAnimation = true)
    {
        setsFlow.Clear();
        setsGrid.Clear();

        setsFlow.Alpha = gridViewToggle.Value ? 0 : 1;
        setsGrid.Alpha = gridViewToggle.Value ? 1 : 0;

        bool grid = gridViewToggle.Value;

        var activeFlow = grid ? setsGrid : setsFlow;


        Logger.Log($"Displaying {sets.Count} sets (selected ID: {selectedId})");
        if (sets.Count == 0)
        {
            selectedSet.Value = null;

            if (string.IsNullOrEmpty(searchQuery.Value?.Trim()))
            {
                sortByButton.Alpha = 0;
                activeFlow.Add(new EmptyState(@"No beatmaps yet. Click ""+ New Beatmap"" to get started.", FontAwesome.Regular.FolderOpen));
            }
            else
            {
                activeFlow.Add(new EmptyState("No beatmaps match your search...", FontAwesome.Solid.Search));
            }

            return;
        }

        sortByButton.Alpha = 1;

        BeatmapSetInfo? newSelection = null;

        var currentSelectedID = selectedSet.Value?.ID;

        foreach (var set in sets)
        {
            newSelection ??= set;

            // async loading magic
            if (grid)
            {
                setsGrid.Add(new SquareDelayedLoadWrapper(
                    () => new BeatmapSetCard(set, selectedSet, openEditor),
                    timeBeforeLoad: 0)
                {
                    RelativeSizeAxes = Axes.X,
                    Width = 0.25f,
                    AlwaysPresent = true,
                });
            }
            else
            {
                // async loading magic
                setsFlow.Add(new DelayedLoadWrapper(
                    () => new BeatmapSetRow(set, selectedSet, openEditor),
                    timeBeforeLoad: 0)
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 56,
                });
            }

            if (currentSelectedID.HasValue && set.ID == currentSelectedID.Value)
                newSelection = set;
        }

        activeFlow.FadeInFromZero(250, Easing.OutQuint);
        if (withAnimation)
        {
            activeFlow.MoveToY(10);
            activeFlow.MoveToY(0, 250, Easing.OutQuint);
        }

        selectedSet.Value = newSelection ?? sets[0];
    }

    protected override void Dispose(bool isDisposing)
    {
        try
        {
            pendingSearchRebuild?.Cancel();

            rebuildCancellation?.Cancel();
            rebuildCancellation?.Dispose();
            rebuildCancellation = null;
        }
        catch
        {
            // ignored
        }


        base.Dispose(isDisposing);
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
        /*if (!config.Get<bool>(EditorSetting.NewDisableUpdater))
        {*/
            updatePopup.SetReleaseInfo(update);
            updatePopup.Show();
            /*return;
        }*/

        /*BrowserUtil.OpenUrl(update.ReleaseUrl);*/
    }

    private partial class SquareDelayedLoadWrapper : DelayedLoadWrapper
    {
        public SquareDelayedLoadWrapper(Func<Drawable> createFunc, double timeBeforeLoad = 500)
            : base(createFunc, timeBeforeLoad)
        {
        }

        protected override void Update()
        {
            base.Update();
            Height = DrawWidth;
        }
    }


    // For when there are no beatmaps
    private partial class EmptyState : CompositeDrawable
    {
        private readonly string message;
        private readonly IconUsage icon;

        public EmptyState(string message, IconUsage icon)
        {
            this.message = message;
            this.icon = icon;
        }

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
                        Icon = icon,
                        Size = new Vector2(40),
                        Alpha = 0.25f,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = message,
                        Font = OsuFont.GetFont(size: 16),
                        Alpha = 0.4f,
                    }
                ],
            };
        }
    }
}
