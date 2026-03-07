using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Rulesets;
using osu.Game.Screens;
using osu.Game.Screens.Select;
using osuTK;
using osuTK.Graphics;
using UbStandaloneEditor.BeatmapPicker;

namespace UbStandaloneEditor;

public partial class BeatmapPickerScreen : OsuScreen
{
    [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;
    [Resolved] private IAPIProvider api { get; set; } = null!;
    [Resolved] private RulesetStore rulesets { get; set; } = null!;
    [Resolved] private RealmAccess realm { get; set; } = null!;
    [Resolved] private IDialogOverlay? dialogOverlay { get; set; }

    private readonly Bindable<BeatmapSetInfo?> selectedSet = new();

    private FillFlowContainer setsFlow = null!;
    private RoundedButton editButton = null!;
    private RoundedButton deleteButton = null!;

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
                                Font = OsuFont.GetFont(size: 18, weight: FontWeight.Bold),
                            },
                            new RoundedButton
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Width = 148,
                                Height = 28,
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
                                        Height = 28,
                                        BackgroundColour = new Color4(170, 50, 50, 255),
                                        Action = promptDelete,
                                    },
                                    editButton = new RoundedButton
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Width = 148,
                                        Height = 28,
                                        Text = "Edit Beatmap",
                                        Action = openEditor,
                                    }
                                ],
                            }
                        ],
                    }
                ],
            }
        ];
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        // Overlay a trash icon on the icon-only delete button
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
            (sets, _) =>
            {
                var prevId = selectedSet.Value?.ID;
                setsFlow.Clear();

                if (sets.Count == 0)
                {
                    setsFlow.Add(new EmptyState());
                    selectedSet.Value = null;
                    return;
                }

                BeatmapSetInfo? newSelection = null;



                foreach (var set in sets.OrderBy(s => s.Metadata.Artist).ThenBy(s => s.Metadata.Title))
                {
                    var detached = set.Detach();
                    if (prevId.HasValue && detached.ID == prevId.Value)
                        newSelection = detached;
                    setsFlow.Add(new BeatmapSetRow(detached, selectedSet));
                }

                selectedSet.Value = newSelection;
            }
        );
    }

    private void createNewBeatmap()
    {
        var ruleset = rulesets.GetRuleset("umania")!;
        var working = beatmapManager.CreateNew(ruleset, api.LocalUser.Value);
        Beatmap.Value = working;
        Ruleset.Value = ruleset;
        this.Push(new EditorLoader());
    }

    private void openEditor()
    {
        if (selectedSet.Value == null) return;
        var beatmap = selectedSet.Value.Beatmaps.OrderBy(b => b.StarRating).First();
        var working = beatmapManager.GetWorkingBeatmap(beatmap);
        Beatmap.Value = working;
        Ruleset.Value = rulesets.GetRuleset("umania")!;
        this.Push(new EditorLoader());
    }

    private void promptDelete()
    {
        if (selectedSet.Value == null) return;
        dialogOverlay?.Push(new BeatmapDeleteDialog(selectedSet.Value));
    }

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
                        Font = OsuFont.GetFont(size: 13),
                        Alpha = 0.4f,
                    }
                ],
            };
        }
    }
}
