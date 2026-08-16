using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.UMania.Edit;

public partial class UbPlacementToolbox : EditorToolboxGroup
{

    [Resolved] private EditorBeatmap beatmap { get; set; } = null!;
    
    private OsuRearrangeableListContainer<UbPlacementHitObjectInfo> placementOrderList = null!;
    
    private List<HitObject> targetHitObjects = new List<HitObject>();
    
    private bool currentlyMovingItems = false;

    public UbPlacementToolbox() : base("Placement Order")
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
    }
    
    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider)
    {
        Child = new FillFlowContainer()
        {
            Spacing = new osuTK.Vector2(0, 5),
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Children = new Drawable[]
            {
                new OsuSpriteText()
                {
                    Margin = new MarginPadding() { Left = 24 },
                    Text = "Last",
                    Font = OsuFont.Default.With(size: 14),
                    Colour = colourProvider.Content2.Darken(0.1f)
                },
                placementOrderList = new UbPlacementList()
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 100
                },
                new OsuSpriteText()
                {
                    Margin = new MarginPadding() { Left = 24 },
                    Text = "First",
                    Font = OsuFont.Default.With(size: 14),
                    Colour = colourProvider.Content2.Darken(0.1f)
                },
            }
        };
       
        
        beatmap.SelectedHitObjects.CollectionChanged += onCollectionChange;

        placementOrderList.Items.CollectionChanged += onItemsUpdated;
        
        updateActiveState();
    }

    private void onItemsUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Move)
        {
            onItemsMoved();
        }
    }

    private void onCollectionChange(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (currentlyMovingItems) 
            return;
        
        updateActiveState();
    }

    private void updateActiveState()
    {
        
        targetHitObjects.Clear();
        targetHitObjects.AddRange(beatmap.SelectedHitObjects); // Make copy, as SelectedHitObjects can be bound to other things
        
        if (targetHitObjects.Count == 0)
        {
            Alpha = 0f;
            return;
        }
        
        var time = targetHitObjects.First().StartTime;
        
        var hasSelectedOnSameTime = targetHitObjects.All(h => Math.Abs(h.StartTime - time) < 1);
        
        if (!hasSelectedOnSameTime)
        {
            Alpha = 0f;
            return;
        }
        
        // Find more objects with the same start time and add them to the list as well
        foreach (var hitObject in beatmap.HitObjects)
        {
            if (Math.Abs(hitObject.StartTime - time) < 1 && !targetHitObjects.Contains(hitObject))
            {
                targetHitObjects.Add(hitObject);
            }
        }
        
        if (targetHitObjects.Count <= 1)
        {
            Alpha = 0f;
            return;
        }
        
        targetHitObjects.Sort((a, b) => beatmap.FindIndex(a).CompareTo(beatmap.FindIndex(b)));

        Alpha = 1f;
        
        refreshItems();

    }

    private void refreshItems()
    {
        placementOrderList.Items.Clear();
        
        for (int i = 0; i < targetHitObjects.Count; i++)
        {
            var hitObject = targetHitObjects[targetHitObjects.Count - i - 1];
            var info = new UbPlacementHitObjectInfo(hitObject);
            info.Index.Value = targetHitObjects.Count - i - 1;
            placementOrderList.Items.Add(info);
        }
    }

    private void onItemsMoved()
    {
        Logger.Log("Items were moved, order: " + string.Join(", ", placementOrderList.Items.Select(h => h.ToString())), LoggingTarget.Runtime, LogLevel.Debug);

        currentlyMovingItems = true;
        
        IList modifiableHitObjects = (IList)beatmap.PlayableBeatmap.HitObjects; // This is a hack to make the list modifiable, the original is read-only.
        
        var indices = placementOrderList.Items
            .Select(h => beatmap.FindIndex(h.HitObject))
            .OrderByDescending(i => i)
            .ToList();

        foreach (var index in indices)
            modifiableHitObjects.RemoveAt(index);

        var sortedIndices = indices.OrderBy(x => x).ToList();

        for (int i = 0; i < placementOrderList.Items.Count; i++)
        {
            var reversedIndex = placementOrderList.Items.Count - i - 1;
            var item = placementOrderList.Items[reversedIndex];
            item.Index.Value = i;
            
            var hitObject = item.HitObject;
            var index = sortedIndices[i];

            if (index >= 0 && index <= modifiableHitObjects.Count)
                modifiableHitObjects.Insert(index, hitObject);
            else
                Logger.Log($"Index {index} out of bounds for {hitObject}. Skipping.");
        }

        currentlyMovingItems = false;
    }

    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);
        
        beatmap.SelectedHitObjects.CollectionChanged -= onCollectionChange;
        
        placementOrderList.Items.CollectionChanged -= onItemsUpdated;
    }
}

// A wrapper class that stores a reference to a hitobject
public class UbPlacementHitObjectInfo
{
    public HitObject HitObject { get; set; }
    
    public BindableInt Index { get; set; }

    public UbPlacementHitObjectInfo(HitObject hitObject)
    {
        HitObject = hitObject;
        Index = new BindableInt(-1);
    }

    public override string ToString()
    {
        return HitObject.ToString();
    }
}