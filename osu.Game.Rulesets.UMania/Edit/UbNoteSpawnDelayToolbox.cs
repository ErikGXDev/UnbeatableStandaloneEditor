using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit;

public partial class UbNoteSpawnDelayToolbox : EditorToolboxGroup
{
    private const float slider_spacing = 5;
        
    [Resolved]
    private OverlayColourProvider colourProvider { get; set; }
    
    [Resolved]
    private EditorBeatmap beatmap { get; set; }
    
    private readonly BindableBool active = new BindableBool(false);
    private UbNoteBuilder noteBuilder = new UbNoteBuilder(null);
    
    private BindableFloat scrollSpeedBindable = new BindableFloat(1f)
    {
        Precision = 0.01f,
        MinValue = 0.5f,
        MaxValue = 3f
    };
    
    private BindableInt distanceBindable = new BindableInt(0)
    {
        MinValue = -1000,
        MaxValue = 4000
    };
    
    private Bindable<string> scrollSpeedBindableString = new Bindable<string>("1.00");
    private Bindable<string> distanceBindableString = new Bindable<string>("0");
    
    private bool writeLock = false;

    public UbNoteSpawnDelayToolbox() : base("Note Spawn Delay")
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
                    new FormNumberBox(allowDecimals: true)
                    {
                        Caption = "Scroll Speed",
                        Current = scrollSpeedBindableString
                    },
                    new FormNumberBox
                    {
                        Caption = "Distance",
                        Current = distanceBindableString
                    },
                    new RoundedButton()
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 30,
                        Origin = Anchor.TopCentre,
                        Anchor = Anchor.TopCentre,
                        Scale = new Vector2(0.8f),
                        BackgroundColour = colourProvider.Background4,
                        HasTriangles = false,
                        Text = "Remove spawn delay",
                        TooltipText = "Consider re-placing the note, if removing data through this button breaks things.",
                        Action = removeFromSelection
                    }
                }
            }
        };
        
        scrollSpeedBindableString.BindValueChanged(v =>
        {
            if (float.TryParse(v.NewValue, out float newValue))
            {
                scrollSpeedBindable.Value = newValue;
            }
        });
        
        distanceBindableString.BindValueChanged(v =>
        {
            if (int.TryParse(v.NewValue, out int newValue))
            {
                distanceBindable.Value = newValue;
            }
        });
        
        scrollSpeedBindable.BindValueChanged(v =>
        {
            if (!writeLock)
            {
                writeData();
            }
        });
        distanceBindable.BindValueChanged(v => 
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

    private void removeFromSelection()
    {
        foreach (var b in beatmap.SelectedHitObjects)
        {
            noteBuilder.ChangeHitObject(b);
            noteBuilder.SetFileHitSampleData("");
        }
        
        beatmap.SelectedHitObjects.Clear();
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
                if (icon == UbIconType.Animated || icon == UbIconType.AnimatedHold)
                {
                    active.Value = false;
                
                    return;
                } 
            }
           
            active.Value = true;
            
            var (scrollSpeed, distance) = getCommonData();
            writeLock = true;
            scrollSpeedBindableString.Value = scrollSpeed.ToString("0.00");
            distanceBindableString.Value = distance.ToString();
            
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
        var dataString = "setpiece_" + scrollSpeedBindable.Value.ToString("0.00") + "_" + distanceBindable.Value.ToString();
        foreach (var b in beatmap.SelectedHitObjects)
        {
            noteBuilder.ChangeHitObject(b);
            noteBuilder.SetFileHitSampleData(dataString);
        }
    }

    // if multiple hitobjects are selected, just use the first one to display.
    private (float, int) getCommonData()
    {
        var selected = beatmap.SelectedHitObjects.OfType<ManiaHitObject>().ToList();
        if (selected.Count > 0)
        {
            noteBuilder.ChangeHitObject(selected[0]);
            var dataString = noteBuilder.GetFileHitSampleData();
            if (dataString.StartsWith("setpiece_"))
            {
                var parts = dataString.Split('_');
                if (parts.Length == 3)
                {
                    if (float.TryParse(parts[1], out float scrollSpeed) && int.TryParse(parts[2], out int distance))
                    {
                        return (scrollSpeed, distance);
                    }
                }
            }
        }

        return (1f, 0);
    }

}