using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
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

namespace osu.Game.Rulesets.UMania.Edit;

public partial class UbAnimateToolbox : EditorToolboxGroup
{
    private const float slider_spacing = 5;
    
    private readonly Bindable<ControlMode> controlModeDropdown = new Bindable<ControlMode>();
    private readonly Bindable<CameraPoint> cameraPointDropdown = new Bindable<CameraPoint>();

    private readonly FormEnumDropdown<CameraPoint> bDropdown;
    private readonly ExpandableSlider<int> bInput;

    private readonly ExpandableSlider<int> cInput;
    private readonly ExpandableSlider<int> dInput;

    private readonly OsuSpriteText hintText;
    

    private readonly BindableInt bankIndexSlider = new BindableInt { MinValue = 0, MaxValue = 12 };
    private readonly BindableInt addBankIndexSlider = new BindableInt { MinValue = -100, MaxValue = 100, Default = 0 };
    private readonly BindableInt customSampleBankSlider = new BindableInt { MinValue = -100, MaxValue = 100, Default = 0 };
    private readonly BindableInt volumeSlider = new BindableInt { MinValue = -100, MaxValue = 100, Default = 0 };

    private readonly BindableBool active = new BindableBool(false);
    private UbNoteBuilder noteBuilder = new UbNoteBuilder(new HitObject());

    public UbAnimateToolbox()
        : base("UNANIMATED")
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;

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
                    createDropdown<ControlMode>("Type", controlModeDropdown),
                    bDropdown = createDropdown<CameraPoint>("Camera Point", cameraPointDropdown),
                    bInput = createSlider("A", addBankIndexSlider),
                    cInput = createSlider("Y", customSampleBankSlider),
                    dInput = createSlider("Z", volumeSlider),
                    hintText = new OsuSpriteText()
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

        hintText.Alpha = 0;
        bDropdown.Alpha = 0;
    }

    [BackgroundDependencyLoader]
    private void load(EditorBeatmap beatmap, OsuColour colour)
    {
        
        cInput.Expanded.BindValueChanged(v =>
        {
            if (v.NewValue)
            {
                hintText.ScaleTo(1f, 200, Easing.OutQuint);
            }
            else
            {
                hintText.ScaleTo(0f, 200, Easing.OutQuint);
            }
        }, true);

        hintText.Colour = colour.YellowLight;
        
        this.beatmap = beatmap;
        beatmap.SelectedHitObjects.CollectionChanged += (_, _) => updateActiveState();
        updateActiveState();
        
        controlModeDropdown.BindValueChanged(v => bankIndexSlider.Value = (int)v.NewValue);
        cameraPointDropdown.BindValueChanged(v => addBankIndexSlider.Value = (int)v.NewValue);
        
        bankIndexSlider.BindValueChanged(v => noteBuilder.SetNormalBankIndex(v.NewValue));
        
        // Handle mode change and show/hide inputs
        bankIndexSlider.BindValueChanged(v =>
        {
            if (!Enum.IsDefined(typeof(ControlMode), v.NewValue))
            {
                bInput.Alpha = 0;
                bDropdown.Alpha = 0;
                cInput.Alpha = 0;
                dInput.Alpha = 0;
                return;
            }
            
            var entry = UbAnimateToolboxData.ModeParameters[(ControlMode)v.NewValue];

            // Show dropdown if B is an enum, otherwise show input
            if (entry.BType.IsEnum)
            {
                bDropdown.Alpha = 1;
                bInput.Alpha = 0;
                
                if (!Enum.IsDefined(typeof(CameraPoint), addBankIndexSlider.Value))
                {
                    addBankIndexSlider.Value = (int)CameraPoint.Left; // Default to Left if the value is not defined
                }
                
                bDropdown.Current.Value = (CameraPoint)addBankIndexSlider.Value;
            }
            else
            {
                bDropdown.Alpha = 0;
                bInput.Alpha = 1;
            }
            
            bInput.ExpandedLabelText = entry.BLabel;
            
            cInput.Alpha = entry.RequiresC ? 1 : 0;
            dInput.Alpha = entry.RequiresD ? 1 : 0;
            
            cInput.ExpandedLabelText = entry.CLabel;
            dInput.ExpandedLabelText = entry.DLabel;
            
            bInput.Slider.ClearMarkers();
            cInput.Slider.ClearMarkers();
            dInput.Slider.ClearMarkers();
            
            hintText.Alpha = 0;
            
            if (entry.bMarkers != null)
            {
                bInput.Slider.SetMarkers(entry.bMarkers.ToList());
                hintText.Alpha = 1;
            }
            if (entry.cMarkers != null)
            {
                cInput.Slider.SetMarkers(entry.cMarkers.ToList());
                hintText.Alpha = 1;
            }
            if (entry.dMarkers != null)
            {
                dInput.Slider.SetMarkers(entry.dMarkers.ToList());
                hintText.Alpha = 1;
            }
            
            applyWhistleIfNeeded();
            
        }, true);
        
        addBankIndexSlider.BindValueChanged(v => {noteBuilder.SetAdditionBankIndex(v.NewValue); applyWhistleIfNeeded();});
        customSampleBankSlider.BindValueChanged(v => {noteBuilder.SetCustomSampleBank(v.NewValue); applyWhistleIfNeeded();});
        volumeSlider.BindValueChanged(v => {noteBuilder.SetVolume(v.NewValue); applyWhistleIfNeeded();});

    }

    private EditorBeatmap beatmap = null!;

    private void applyWhistleIfNeeded()
    {
        if (!noteBuilder.HasHitObject) return;
        
        if (!noteBuilder.HasSample(HitSampleInfo.HIT_WHISTLE))
        {
            noteBuilder.ApplySamples(new List<string>() {HitSampleInfo.HIT_WHISTLE});
        }
    }

    private void updateActiveState()
    {
        var selected = beatmap.SelectedHitObjects.OfType<ManiaHitObject>().ToList();

        if (selected.Count == 1 && selected[0].Column == 1)
        {
            active.Value = true;
            bindToSample(selected[0]);
        }
        else
        {
            active.Value = false;
            noteBuilder.ChangeHitObject(null);
        }
    }

    private void bindToSample(ManiaHitObject hitObject)
    {
        noteBuilder.ChangeHitObject(hitObject);

        bankIndexSlider.Value = noteBuilder.GetNormalBankIndex();
        
        if (Enum.IsDefined(typeof(ControlMode), bankIndexSlider.Value))
        {
            controlModeDropdown.Value = (ControlMode)bankIndexSlider.Value;
        }
        else
        {
            controlModeDropdown.Value = ControlMode.Reset; // Default to Reset if the value is not defined
            bankIndexSlider.Value = (int)ControlMode.Reset;
        }
        
        addBankIndexSlider.Value = noteBuilder.GetAdditionBankIndex();
        customSampleBankSlider.Value = noteBuilder.GetCustomSampleBank();
        volumeSlider.Value = noteBuilder.GetVolume();

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
}