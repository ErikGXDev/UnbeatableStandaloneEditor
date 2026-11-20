using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit;

public partial class UbBlindSideswapToolbox : EditorToolboxGroup
{
    private const float slider_spacing = 5;
        
    [Resolved]
    private OverlayColourProvider colourProvider { get; set; }
    
    [Resolved]
    private EditorBeatmap beatmap { get; set; }
    
    private readonly BindableBool active = new BindableBool(false);
    private UbNoteBuilder noteBuilder = new UbNoteBuilder(null);
    
    private BindableBool isBlind = new BindableBool(false);
    
    private bool writeLock = false;

    public UbBlindSideswapToolbox() : base("Blind Sideswap")
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
        
    }

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider)
    {
        active.BindValueChanged(v => Alpha = v.NewValue ? 1 : 0, true);
        
        Children = new Drawable[]
        {
            new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, slider_spacing),
                Padding = new MarginPadding(5),
                Children = new Drawable[]
                {
                    new FormCheckBox()
                    {
                        Caption = "Is Blind",
                        Current = isBlind
                    },
                    new OsuTextFlowContainer(t =>
                    {
                        t.Font = OsuFont.Default.With(size: 12, weight: FontWeight.Regular);
                    })
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Text = "Tip: Blind flips only swap notes but not the camera. The preview won't accurately show this.",
                        Colour = colourProvider.Content1.Opacity(0.8f),
                        Margin = new MarginPadding { Top = 4 },
                    }
                    /*new RoundedButton()
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 30,
                        Origin = Anchor.TopCentre,
                        Anchor = Anchor.TopCentre,
                        Scale = new Vector2(0.8f),
                        BackgroundColour = colourProvider.Background4,
                        HasTriangles = false,
                        Text = "Remove blind data",
                        TooltipText = "Consider re-placing the note, if removing data through this button breaks things.",
                        Action = removeFromSelection
                    }*/
                }
            }
        };
        
        isBlind.BindValueChanged(v =>
        {
            if (!writeLock)
            {
                writeData();
            }
        });
        
        beatmap.SelectedHitObjects.CollectionChanged += selectedCollectionChanged;
    }
    
    protected override void Dispose(bool isDisposing)
    {
        beatmap.SelectedHitObjects.CollectionChanged -= selectedCollectionChanged;
    }
    
    private void selectedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        updateActiveState();
    }
    
    private void updateActiveState()
    {
        
        var selected = beatmap.SelectedHitObjects.OfType<ManiaHitObject>().ToList();

        if (selected.Count > 0)
        {
            foreach (var b in selected)
            {
                noteBuilder.ChangeHitObject(b);

                var icon = noteBuilder.InferObjectTypeIcon();
                if (icon != UbIconType.Flip)
                {
                    active.Value = false;
                
                    return;
                } 
            }
           
            active.Value = true;
            isBlind.Value = getCommonData();
            var isBlindEnabled = getCommonData();
            writeLock = true;
            isBlind.Value = isBlindEnabled;
            
            Schedule(() => writeLock = false);
            
        }
        else
        {
            active.Value = false;
            noteBuilder.ChangeHitObject(null);
        }
    }

    private void writeData()
    {
        // setpiece_SCROLLSPEED_DISTANCE
        var dataString = isBlind.Value ? "blind" : "";
        foreach (var b in beatmap.SelectedHitObjects)
        {
            noteBuilder.ChangeHitObject(b);
            noteBuilder.SetFileHitSampleData(dataString);
        }
    }

    // if multiple hitobjects are selected, just use the first one to display.
    private bool getCommonData()
    {
        var selected = beatmap.SelectedHitObjects.OfType<ManiaHitObject>().ToList();
        if (selected.Count > 0)
        {
            noteBuilder.ChangeHitObject(selected[0]);
            var dataString = noteBuilder.GetFileHitSampleData();
            if (dataString.StartsWith("blind"))
            {
                return true;
            }
        }

        return false;
    }

}