using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit;
using osuTK;
using Vector2 = osuTK.Vector2;

namespace osu.Game.Rulesets.UMania.Edit;

public partial class UbAnimateToolbox : EditorToolboxGroup
{
    private const float slider_spacing = 5;
    

    private readonly OsuSpriteText markerHintText;

    private readonly Bindable<CategoryType> eventCategorySlider = new Bindable<CategoryType>(CategoryType.Camera);
    private readonly BindableInt eventTypeSlider = new BindableInt(0);
    
    private UbAnimateToolboxData.BaseOption<BindableInt> currentOption;
    
    private readonly List<IBindable> bindables = new List<IBindable>();
        
    private FormEnumDropdown<CategoryType> categoryDropdown = null!;
    private Drawable typeDropdown = null!;
    
    private Container typeContainer = null!;
    private FillFlowContainer parameterContainer = null!;
    
    private readonly BindableBool active = new BindableBool(false);
    private UbNoteBuilder noteBuilder = new UbNoteBuilder(new HitObject());

    public UbAnimateToolbox()
        : base("UNANIMATED")
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
        
        currentOption = createEnumIntOption("Type", typeof(CameraAction)) as UbAnimateToolboxData.BaseOption<BindableInt> ?? throw new InvalidOperationException("Failed to create current option.");

        var (currentOptionDrawable, currentOptionBindable) = currentOption.CreateDrawableAndBindable();
        currentOptionBindable.BindTo(eventTypeSlider);
        
        // Hide toolbox when not active
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
                    categoryDropdown = createDropdown<CategoryType>("Category", eventCategorySlider).With(d => d.Margin = new MarginPadding { Bottom = 4}),
                    
                    typeContainer = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Child = currentOptionDrawable
                    },
                    
                    parameterContainer = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, slider_spacing),
                        Children = new Drawable[]
                        {
                            
                        }
                    },
                    
                    // Only shown if current a NumberOption is present that has markers defined
                    markerHintText = new OsuSpriteText()
                    {
                        AllowMultiline = true,
                        RelativeSizeAxes = Axes.X,
                        Text = "Tip: Markers indicate values that are also used by the game.",
                        Font = OsuFont.Default.With(size: 12, weight: FontWeight.Regular),
                        Colour = Colour4.Yellow,
                        Margin = new MarginPadding { Top = 4 },

                    }
                }
            }
        };

        markerHintText.Alpha = 0;

        // Bind category/type changes to parameter refresh
        eventCategorySlider.BindValueChanged(_ => refreshCategory(), true);

        eventTypeSlider.BindValueChanged(_ => refreshParameters(), true);
    }

    private void writeEventData()
    {
        Logger.Log("Parameters: " + string.Join(", ", bindables.Select(b => $"{b.GetType().Name}: {b.GetType().GetProperty("Value")?.GetValue(b)}")));
        
        if (!noteBuilder.HasHitObject)
        {
            Logger.Log("[UNANIMATED] No hit object selected to write data to.");
            return;
        }
        
        // Convert eventTypeSlider to a string according to the enum type of the current category
        var category = eventCategorySlider.Value;
        if (!Enum.IsDefined(typeof(CategoryType), category))
            return;
        
        var targetEnum = UbAnimateToolboxData.CategoryTypeInfo[category].TypeEnum;
        Enum eventType = (Enum)Enum.ToObject(targetEnum, eventTypeSlider.Value);
        
        var eventTypeString = eventType.ToString();
        
        // Store category, type, and parameters as a string concatenated with |
        var encoded = $"{eventCategorySlider.Value}|{eventTypeString}";

        if (bindables.Count > 0)
        {
            encoded += $"|{string.Join("|", bindables.Select(b => b.GetType().GetProperty("Value")?.GetValue(b)?.ToString() ?? ""))}";
        }
        
        noteBuilder.SetFileHitSampleData(encoded);
    }


    [BackgroundDependencyLoader]
    private void load(EditorBeatmap beatmap, OsuColour colour)
    {
        markerHintText.Colour = colour.YellowLight;
        
        this.beatmap = beatmap;
        beatmap.SelectedHitObjects.CollectionChanged += selectedCollectionChanged;
        updateActiveState();
    }

    protected override void Dispose(bool isDisposing)
    {
        beatmap.SelectedHitObjects.CollectionChanged -= selectedCollectionChanged;
        
        base.Dispose(isDisposing);
    }

    private void selectedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        updateActiveState();
    }

    private void refreshCategory()
    {
        var category = eventCategorySlider.Value;
        if (!Enum.IsDefined(typeof(CategoryType), category))
            return;
        
        typeContainer.Clear();
        
        // Get the enum type for the selected category
        var targetEnum = UbAnimateToolboxData.CategoryTypeInfo[category].TypeEnum;
        
        // Create a new option for the selected category
        var label = UbAnimateToolboxData.CategoryTypeInfo[category].TypeLabel;
        
        currentOption = createEnumIntOption(label, targetEnum) as UbAnimateToolboxData.BaseOption<BindableInt> ?? throw new InvalidOperationException("Failed to create current option.");
        
        var (currentOptionDrawable, currentOptionBindable) = currentOption.CreateDrawableAndBindable();
        currentOptionBindable.BindTo(eventTypeSlider);
        
        typeContainer.Add(currentOptionDrawable);

        eventTypeSlider.Value = 0; // Reset eventType, triggers refreshParameters as well
        
        refreshParameters();
    }

    private void refreshParameters(object[]? parameters = null)
    {
        parameterContainer.Clear();
        bindables.Clear();
        
        markerHintText.Alpha = 0;

        var category = eventCategorySlider.Value;
        if (!Enum.IsDefined(typeof(CategoryType), category))
            return;

        // Get the enum type for the selected category
        var targetEnum = UbAnimateToolboxData.CategoryTypeInfo[category].TypeEnum;
        
        Enum eventType = (Enum)Enum.ToObject(targetEnum, eventTypeSlider.Value);
        
        if (UbAnimateToolboxData.CategoryOptions.TryGetValue(category, out var typeOptions) && typeOptions.TryGetValue(eventType, out var options))
        {
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option is UbAnimateToolboxData.IntOption intOption)
                {
                    if (intOption.Markers != null)
                    {
                        markerHintText.Alpha = 1;
                    }
                }

                var (drawable, bindable) = option.CreateDrawableAndBindable(writeEventData);
                parameterContainer.Add(drawable);
                bindables.Add(bindable);

                if (parameters != null)
                {
                    var valueProperty = bindable.GetType().GetProperty("Value");
                    if (valueProperty != null && i < parameters.Length)
                    {
                        // Check if the parameter is of the correct type before setting it
                        var parameterType = parameters[i]?.GetType();
                        if (parameterType != null && valueProperty.PropertyType.IsAssignableFrom(parameterType))
                        {
                            valueProperty.SetValue(bindable, parameters[i]);
                        }
                        else
                        {
                            Logger.Log("[UNANIMATED] Parameter type mismatch for index " + i);
                        }
                    }
                }
            }
        }
        
        writeEventData();
    }
    

    private EditorBeatmap beatmap = null!;

    private void updateActiveState()
    {
        
        var selected = beatmap.SelectedHitObjects.OfType<ManiaHitObject>().ToList();

        if (selected.Count == 1 && selected[0].Column == 1)
        {
            noteBuilder.ChangeHitObject(selected[0]);

            var icon = noteBuilder.InferObjectTypeIcon();
            if (icon == UbIconType.Animated || icon == UbIconType.AnimatedHold)
            {
                // Good, we can edit this sample
                active.Value = true;
                
                bindToSample(selected[0]);
                return;
            } 
            
            // Not a valid note
            active.Value = false;
            noteBuilder.ChangeHitObject(null);
            
            
            
        }
        else
        {
            active.Value = false;
            noteBuilder.ChangeHitObject(null);
        }
    }

    private void bindToSample(ManiaHitObject hitObject)
    {
        var existingData = noteBuilder.GetFileHitSampleData();
        if (!string.IsNullOrEmpty(existingData)) 
        {
            var parts = existingData.Split('|');
            if (parts.Length >= 2)
            {
                if (Enum.TryParse(parts[0], out CategoryType category))
                {
                    eventCategorySlider.Value = category;
                }
                
                // eventType is a string that needs to be parsed into the correct enum type based on the selected category
                Type enumType = UbAnimateToolboxData.CategoryTypeInfo[eventCategorySlider.Value].TypeEnum;

                if (Enum.TryParse(enumType, parts[1], out object? eventType) && eventType != null)
                {
                    eventTypeSlider.Value = Convert.ToInt32(eventType);
                }
                else
                {
                    eventTypeSlider.Value = 0;
                }

                // Pass the remaining parts as parameters to refreshParameters
                var parameters = parts.Skip(2).ToArray();
                
                // Parse some params as numbers if possible
                var parsedParameters = parameters.Select(p => int.TryParse(p, out int intValue) ? (object)intValue : p).ToArray();
                
                refreshParameters(parsedParameters);
            }
        }
        else
        {
            // No existing data, reset to defaults
            eventCategorySlider.Value = CategoryType.Camera;
            eventTypeSlider.Value = 0;
            refreshParameters();
        }
    }

    private static ExpandableSlider<int> createSlider(string label, BindableInt bindable)
    {
        return new ExpandableSlider<int>
        {
            Current = bindable,
            KeyboardStep = 1,
            ExpandedLabelText = label,
        };
    }
    
    private static FormEnumDropdown<T> createDropdown<T>(string label, Bindable<T> bindable) where T : struct, Enum
    {
        return new FormEnumDropdown<T>
        {
            Current = bindable,
            Caption = label,
        };
    }
    
    // yay, Reflection!
    private static object createEnumIntOption(string label, Type enumType)
    {
        Type optionType = typeof(UbAnimateToolboxData.EnumIntOption<>).MakeGenericType(enumType);

        ConstructorInfo? constructor = optionType.GetConstructor(new[] { typeof(string) });
        
        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {optionType.FullName} with a single string parameter.");

        return constructor.Invoke(new object[] { label });
    }
    
    private static object createEnumStringOption(string label, Type enumType)
    {
        Type optionType = typeof(UbAnimateToolboxData.EnumStringOption<>).MakeGenericType(enumType);

        ConstructorInfo? constructor = optionType.GetConstructor(new[] { typeof(string) });
        
        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {optionType.FullName} with a single string parameter.");

        return constructor.Invoke(new object[] { label });
    }
}