using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osuTK;

namespace UbStandaloneEditor;

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Screens;

public partial class BeatmapPickerScreen : OsuScreen
{
    [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;

    [Resolved] private IAPIProvider api { get; set; } = null!;

    [Resolved] private RulesetStore rulesets { get; set; } = null!;

    [Resolved] private RealmAccess realm { get; set; } = null!;
    

    private FillFlowContainer flow = null!;

    [Cached] private OverlayColourProvider colours = new OverlayColourProvider(OverlayColourScheme.Aquamarine);


    public override bool AllowUserExit { get; } = false;

    [BackgroundDependencyLoader]
    private void load()
    {
        InternalChildren = new Drawable[]
        {
            new Box { RelativeSizeAxes = Axes.Both, Colour = colours.Background4 },
            new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding(10),
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 8),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = "Select a beatmap to edit",
                                Font = OsuFont.GetFont(size: 24, weight: FontWeight.SemiBold),
                            },
                            new FormButton
                            {
                                Caption = "Create a new beatmap",
                                ButtonText = "New",
                                Action = createNewBeatmap,
                            },
                        }
                    },
                    new OsuScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 100 },
                        Child = flow = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 4),
                        }
                    },
                }
            }
        };
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();


        // RegisterForNotifications keeps the list live — adds/deletes reflect immediately
        realm.RegisterForNotifications(
            r => r.All<BeatmapSetInfo>().Where(s => !s.DeletePending),
            (sets, _) =>
            {
                flow.Clear();
                foreach (var set in sets.OrderBy(s => s.Metadata.Artist))
                {
                    // Detach so the objects are safe to use off the realm thread
                    var detached = set.Detach();
                    foreach (var beatmap in detached.Beatmaps.OrderBy(b => b.StarRating))
                        flow.Add(new BeatmapEntry(detached, beatmap, openEditor));
                }
            }
        );
    }

    private void createNewBeatmap()
    {
        var maniaRuleset = rulesets.GetRuleset("umania")!;
        var working = beatmapManager.CreateNew(maniaRuleset, api.LocalUser.Value);

        Beatmap.Value = working;
        Ruleset.Value = maniaRuleset;
        this.Push(new EditorLoader());
    }

    private void openEditor(BeatmapInfo beatmap)
    {
        var working = beatmapManager.GetWorkingBeatmap(beatmap);

        Beatmap.Value = working;
        Ruleset.Value = rulesets.GetRuleset("umania")!;
        this.Push(new EditorLoader());
    }

    private partial class BeatmapEntry : OsuClickableContainer
    {
        private readonly BeatmapSetInfo set;
        private readonly BeatmapInfo beatmap;
        private readonly Action<BeatmapInfo> onSelect;

        public BeatmapEntry(BeatmapSetInfo set, BeatmapInfo beatmap, Action<BeatmapInfo> onSelect)
        {
            this.set = set;
            this.beatmap = beatmap;
            this.onSelect = onSelect;

            RelativeSizeAxes = Axes.X;
            Height = 50;
            Action = () => onSelect(beatmap);
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colours)
        {
            Children = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = colours.Background3 },
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding { Horizontal = 12 },
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = $"{set.Metadata.Artist} - {set.Metadata.Title}",
                            Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold),
                        },
                        new OsuSpriteText
                        {
                            Text = $"{beatmap.DifficultyName}  ★{beatmap.StarRating:F2}",
                            Font = OsuFont.GetFont(size: 12),
                            Alpha = 0.7f,
                        }
                    }
                }
            };
        }
    }
}