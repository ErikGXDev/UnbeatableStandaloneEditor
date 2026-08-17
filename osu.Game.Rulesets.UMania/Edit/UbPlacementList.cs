using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.UMania.Edit;

public partial class UbPlacementList : OsuRearrangeableListContainer<UbPlacementHitObjectInfo>
{
    protected override OsuRearrangeableListItem<UbPlacementHitObjectInfo> CreateOsuDrawable(UbPlacementHitObjectInfo item)
    {
        return new UbPlacementListItem(item);
    }
    
    private Drawable listFillFlowContainer = null!;

    protected override void Update()
    {
        Height = listFillFlowContainer.DrawHeight;
    }

    protected override FillFlowContainer<RearrangeableListItem<UbPlacementHitObjectInfo>> CreateListFillFlowContainer() => new FillFlowContainer<RearrangeableListItem<UbPlacementHitObjectInfo>>().With(d =>
    {
        listFillFlowContainer = d;
        
        d.LayoutDuration = 160;
        d.LayoutEasing = Easing.OutQuint;
        d.Spacing = new Vector2(1);
    });
}

public partial class UbPlacementListItem : OsuRearrangeableListItem<UbPlacementHitObjectInfo>
{
    private UbPlacementHitObjectInfo hitObjectInfo;
    
    public UbPlacementListItem(UbPlacementHitObjectInfo hitObjectInfo) : base(hitObjectInfo)
    {
        this.hitObjectInfo = hitObjectInfo;
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
    }
    
    [Resolved]
    private UnbeatableHitObjectComposer composer { get; set; } = null!;
    
    private OsuSpriteText? text;
    private OsuSpriteText? indexText;

    private string getNoteVerb()
    {
        var is4Key = composer.Is4Key;

        var ubNoteBuilder = new UbNoteBuilder(hitObjectInfo.HitObject);

        var icon = ubNoteBuilder.InferObjectTypeIcon();

        var verb = icon.Humanize();

        var heightStr = "";

        if (hitObjectInfo.HitObject is ManiaHitObject mania)
        {
            if (is4Key)
            {
                heightStr = mania.Column switch
                {
                    0 => "Top",
                    1 => "Bottom",
                    2 => "Top",
                    3 => "Bottom",
                    4 => "",
                    5 => "",
                    _ => ""
                };
            }
            else
            {
                heightStr = mania.Column switch
                {
                    0 => "",
                    1 => "",
                    2 => "Top",
                    3 => "Bottom",
                    4 => "",
                    5 => "",
                    _ => ""
                };
            }
        }

        if (!string.IsNullOrEmpty(heightStr))
        {
            heightStr += " ";
        }
        
        return $"{heightStr}{verb}";
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        if (text != null)
            text.Text = getNoteVerb();
        
        if (indexText != null)
            indexText.Text = $"{hitObjectInfo.Index.Value + 1}.";
        
        hitObjectInfo.Index.BindValueChanged(index =>
        {
            if (indexText != null)
                indexText.Text = $"{index.NewValue + 1}.";
        });
        
        hitObjectInfo.HitObject.SamplesBindable.BindCollectionChanged((_, _) =>
        {
            if (text != null)
                text.Text = getNoteVerb();
        });
    }


    protected override Drawable CreateContent()
    {
        return new Container
        {
            RelativeSizeAxes = Axes.X,
            Height = 30,
            Masking = true,
            CornerRadius = 5f,
            Margin = new MarginPadding { Vertical = 1 },
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White.Opacity(0.1f)
                },
                text = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Font = OsuFont.Default.With(size: 16),
                    Text = "",
                    Margin = new MarginPadding { Left = 10 },
                },
                indexText = new OsuSpriteText
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Font = OsuFont.Default.With(size: 14),
                    Colour = Color4.White.Darken(0.2f),
                    Text = "",
                    Margin = new MarginPadding { Right = 10 },
                },
            }
        };
    }
}