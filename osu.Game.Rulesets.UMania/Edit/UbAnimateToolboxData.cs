using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Graphics.UserInterfaceV2;
using Container = osu.Framework.Graphics.Containers.Container;

namespace osu.Game.Rulesets.UMania.Edit;

public enum CategoryType
{
    UNANIMATED,
    Camera,
    Character,
    Gameplay,
    [Description("Stage Switch")]
    StageScene,
    UI
}

public enum UnanimatedAction
{
    [Description("Set Default Stage")]
    DefaultStageScene,
    
    [Description("Show background video")]
    ShowBackgroundVideo,
    
    [Description("Use old units (pre-0.1.14)")]
    LegacyCameraUnits,
    
}

public enum CameraAction
{
    [Description("Reset")]
    Reset,

    [Description("Camera Target")]
    CameraTarget,

    [Description("Zoom Offset")]
    ZoomOffset,

    [Description("Zoom Target")]
    ZoomTarget,

    [Description("Rotation Offset")]
    RotOffset,

    [Description("Rotation Target")]
    RotTarget,

    [Description("Horizontal Offset")]
    HorizontalOffset,

    [Description("Horizontal Target")]
    HorizontalTarget,

    [Description("Custom Camera Target")]
    CustomCameraTarget,
    
    [Description("Custom Camera Offset")]
    CustomCameraOffset,
    
    [Description("Custom Rotation Target")]
    CustomRotTarget,
    
    [Description("Custom Rotation Offset")]
    CustomRotOffset,

    [Description("Ease Time")]
    EaseTime,

    [Description("Ease Mode")]
    EaseMode,

    [Description("FOV Target")]
    FOVTarget,

    [Description("FOV Offset")]
    FOVOffset
}

public enum CustomModifier
{
    [Description("None")]
    Normal,
    
    [Description("Instant")]
    Instant,
    
    [Description("Reversed")]
    Reversed,
}

public enum CharacterAction
{
    Reset,
    
    [Description("Set Character")]
    SetCharacter
}

public enum GameplayAction
{
    [Description("Screen Shake")]
    ScreenShake,
    
    [Description("Screen Rotation")]
    ScreenRot,
    
    [Description("Screen Zoom")]
    ScreenZoom
}

public enum StageSceneAction
{
    TrainStationRhythm,
    StadiumPast,
    StadiumPresent,
    DreamReflection,
    PrisonYard,
    MovingTrain,
    LighthouseStage,
    LoadingDockInterior,
    RecordingStudio,
    CityStreet,
    CityCenter,
    TutorialRhythm,
    RichPeopleConcert,
    AlleywayStage,
    WarehouseStage,
    HARMLobby,
    HARMZeroMomentArray,
    HARMSforzandoArena,
    HARMGraveyard,
    NSR_Stage,
    GreenscreenRhythm,
    PlaybackStage,
    BirdBrainRhythm,
    WomenWrestlingRhythm,
    NOISZRhythm,
    TrainStationRhythmPixel,
}

public enum UIAction
{
    [Description("Force Locked UI")]
    ForceLockedUI,
    
    Show,
    
    Hide,
    
    [Description("Slide In")]
    SlideIn,
    
    [Description("Slide Out")]
    SlideOut
}


public enum CameraPoint
{
    [Description("Left")]
    Left = 1,

    [Description("Left Wide")]
    LeftWide = 2,

    [Description("Wide")]
    Wide = 3,

    [Description("Right Wide")]
    RightWide = 4,

    [Description("Right")]
    Right = 5
}

public enum CameraEasing
{
    Linear = 1,
    InSine,
    OutSine,
    InOutSine,
    InQuad,
    OutQuad,
    InOutQuad,
    InCubic,
    OutCubic,
    InOutCubic,
    InQuart,
    OutQuart,
    InOutQuart,
    InQuint,
    OutQuint,
    InOutQuint,
    InExpo,
    OutExpo,
    InOutExpo,
    InCirc,
    OutCirc,
    InOutCirc,
    InElastic,
    OutElastic,
    InOutElastic,
    InBack,
    OutBack,
    InOutBack,
    InBounce,
    OutBounce,
    InOutBounce,
    Flash,
    InFlash,
    OutFlash,
    InOutFlash,
}



public partial class UbAnimateToolboxData
{

    public static string CharacterListText = """
                                             List of available characters:

                                             Beat, Beat (Hoodie), Beat (Guitar), Beat (Up), Beat (Nothing), Clef, Quaver, Quaver (Acoustic), Quaver (CQC), Treble, Rest, Rest (OMF), Eve, Grace, Crest, Crest (Maid), DC, Poco, Apoco, Penny, Sforzando, JamieP, Quaver (Shrimp)
                                             
                                             ---
                                             
                                             Type "Reset" to reset the character.
                                             """;

    public static string StageListText = """
                                         List of available stages:

                                         TrainStationRhythm, StadiumPast, StadiumPresent, DreamReflection, PrisonYard, MovingTrain, LighthouseStage, LoadingDockInterior, RecordingStudio, CityStreet, CityCenter, RichPeopleConcert, AlleywayStage, WarehouseStage, HARMLobby, HARMZeroMomentArray, HARMSforzandoArena, HARMGraveyard, NSR_Stage, GreenscreenRhythm, PlaybackStage, BirdBrainRhythm, WomenWrestlingRhythm, NOISZRhythm, NSR_Stage, TrainStationRhythmPixel
                                         
                                         ---
                                         
                                         Stage swapping only works if the "Default" stage is selected in UNBEATABLE.
                                         """;

    public static string ShowHideText = """
                                        Show or hide UI elements by entering them here, separated by vertical bars (|).
                                        
                                        List of available UI elements:
                                        Score, Accuracy, MaxCombo, VignetteBars, BlackBGBars, FourByThreeBars, Health, LeftJudgementLine, RightJudgementLine, MeasureBars, SpeedLines, UpNextIndicators, Reticle, Notes
                                        """;
    
    public static string SlideInOutText = """
                                        Show or hide UI elements with a transition by entering them here, separated by vertical bars (|).

                                        List of available UI elements:
                                        AllOverlayUI, LeftJudgementLine, RightJudgementLine, BackgroundGradient, HealthBar, SpeedLines, FourByThreeBars, BlackBGBars
                                        """;
    
    public abstract class BaseOption
    {
        public string Label { get; protected set; } = string.Empty;
        
        public virtual (Drawable, IBindable) CreateDrawableAndBindable(Action? onValueChanged = null) => (null, null);
        
    }

    public abstract class BaseOption<TBindable> : BaseOption where TBindable : IBindable
    {
    }

    public class FloatOption : BaseOption<BindableFloat>
    {
        
        public static Bindable<bool> UseTextBox = new Bindable<bool>(false);
        
        public float DefaultValue { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float[]? Markers { get; set; }
        public float Precision { get; set; }

        public FloatOption(string label, float defaultValue, float min, float max, float precision = 0.01f, float[]? markers = null)
        {
            Label = label;
            DefaultValue = defaultValue;
            Min = min;
            Max = max;
            Precision = precision;
            Markers = markers;
        }

        public override (Drawable, IBindable) CreateDrawableAndBindable(Action? onValueChanged)
        {
            var finalFloatBindable = new BindableFloat() { Default = DefaultValue, Precision = Precision };
            finalFloatBindable.BindValueChanged(v => onValueChanged?.Invoke());
            
            var sliderBindable = new BindableFloat() { MinValue = Min, MaxValue = Max, Default = DefaultValue, Precision = Precision };
            var boxBindable = new Bindable<string>() { Default = DefaultValue.ToString() };
            
            var slider = new FormSliderBar<float>()
            {
                LabelFormat = i => i.ToString(),
                Caption = Label,
                Current = sliderBindable,
                KeyboardStep = 1,
            };
            
            boxBindable.BindValueChanged(v =>
            {
                var newFloat = float.TryParse(v.NewValue, out var parsed) ? parsed : sliderBindable.Value;
                
                if (sliderBindable.Value != newFloat)
                    sliderBindable.Value = newFloat;
                
                if (finalFloatBindable.Value != newFloat)
                    finalFloatBindable.Value = newFloat;
            }, true);
            
            sliderBindable.BindValueChanged(v =>
            {
                if (boxBindable.Value != v.NewValue.ToString())
                    boxBindable.Value = v.NewValue.ToString();
                
                if (finalFloatBindable.Value != v.NewValue)
                    finalFloatBindable.Value = v.NewValue;
            }, true);
            
            finalFloatBindable.BindValueChanged(v =>
            {
                if (sliderBindable.Value != v.NewValue)
                    sliderBindable.Value = v.NewValue;
                
                if (boxBindable.Value != v.NewValue.ToString())
                    boxBindable.Value = v.NewValue.ToString();
            });
            
            var numberBox = new FormNumberBox(allowDecimals: Precision < 1)
            {
                Alpha = 0,
                Caption = Label,
                Current = boxBindable
            };
            
            var container = new Container()
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    slider,
                    numberBox
                }
            };
            
            UseTextBox.BindValueChanged(v =>
            {
                if (v.NewValue)
                {
                    slider.Alpha = 0;
                    numberBox.Alpha = 1;
                }
                else
                {
                    numberBox.Alpha = 0;
                    slider.Alpha = 1;
                    
                    // Clamp the slider value to the min/max if the textbox value is out of bounds
                    var numberBoxValue = float.TryParse(boxBindable.Value, out var parsed) ? parsed : sliderBindable.Value;
                    
                    // Clamp the value to precision
                    numberBoxValue = (float)Math.Round(numberBoxValue / Precision) * Precision;
                    
                    if (numberBoxValue < Min || numberBoxValue > Max)
                    {
                        var clampedValue = Math.Clamp(numberBoxValue, Min, Max);
                        sliderBindable.Value = clampedValue;
                        boxBindable.Value = clampedValue.ToString();
                    }
                    
                }
            }, true);
            
            
            if (Markers != null)
            {
                slider.SetMarkersScheduled(Markers.ToList());
                Logger.Log("Markers set for slider: " + string.Join(", ", Markers));
            }
            
            return (container, finalFloatBindable);
        }
    }
    
    public class StringOption : BaseOption<Bindable<string>>
    {
        public string DefaultValue { get; set; }
        
        public string HintText { get; set; }
        
        public StringOption(string label, string defaultValue, string hintText = "")
        {
            Label = label;
            DefaultValue = defaultValue;
            HintText = hintText;
        }
        
        public override (Drawable, IBindable) CreateDrawableAndBindable(Action? onValueChanged)
        {
            var bindable = new Bindable<string>(DefaultValue);
            
            bindable.BindValueChanged(v => onValueChanged?.Invoke());
            
            var textBox = new FormTextBox()
            {
                Caption = Label,
                Current = bindable,
                HintText = HintText
            };
            
            return (textBox, bindable);
        }
    }

    public class IntCheckboxOption : BaseOption<Bindable<int>>
    {
        public int DefaultValue { get; set; }
        
        public IntCheckboxOption(string label, int defaultValue)
        {
            Label = label;
            DefaultValue = defaultValue;
        }
        
        public override (Drawable, IBindable) CreateDrawableAndBindable(Action? onValueChanged)
        {
            var bindable = new Bindable<int>(DefaultValue);
            
            var boolBindable = new Bindable<bool>(DefaultValue == 1 ? true : false);
            
            boolBindable.BindValueChanged(v =>
            {
                bindable.Value = v.NewValue ? 1 : 0;
                onValueChanged?.Invoke();
            });
            
            var checkbox = new FormCheckBox()
            {
                Caption = Label,
                Current = boolBindable,
            };
            
            return (checkbox, bindable);
        }
    }
    
    
    // An option that represents an enum by having the dropdown items be the enum values as strings
    public class EnumStringOption<T> : BaseOption<Bindable<string>> where T : struct, Enum
    {
        public Type EnumType { get; set; }
        
        public EnumStringOption(string label)
        {
            Label = label;
            EnumType = typeof(T);
        }
        
        public override (Drawable, IBindable) CreateDrawableAndBindable(Action? onValueChanged)
        {

            var firstEnumValue = Enum.GetValues<T>().First();
            
            var firstString = firstEnumValue.ToString();
            
            var stringBindable = new Bindable<string>(firstString);
            
            stringBindable.BindValueChanged(v => onValueChanged?.Invoke());
            
            // Find the first enum value of the enum for default value
            var enumBindable = new Bindable<T>(firstEnumValue);

            // Attempt to back-bind string changes if the string value is newer than the enum value
            stringBindable.BindValueChanged(v =>
            {
                if (enumBindable.Value.ToString() == v.NewValue)
                    return;
                
                if (Enum.TryParse<T>(v.NewValue, out var parsedEnum))
                {
                    enumBindable.Value = parsedEnum;
                }
            });
            
            enumBindable.BindValueChanged(v =>
            {
                stringBindable.Value = v.NewValue.ToString();
            });
            
            var dropdown = new FormEnumDropdown<T>()
            {
                Caption = Label,
                Current = enumBindable,
            };
            
            return (dropdown, stringBindable);
        }
    }
    
    // An option that represents an enum by having the items be the enum values as integers, but the dropdown items are the enum values as strings
    public class EnumIntOption<T> : BaseOption<BindableInt> where T : struct, Enum
    {
        public Type EnumType { get; set; }
        
        public EnumIntOption(string label)
        {
            Label = label;
            EnumType = typeof(T);
        }
        
        public override (Drawable, IBindable) CreateDrawableAndBindable(Action? onValueChanged)
        {
            var intBindable = new BindableInt();
            
            intBindable.BindValueChanged(v => onValueChanged?.Invoke());
            
            var enumBindable = new Bindable<T>();

            // Attempt to back-bind int changes if the int value is newer than the enum value
            intBindable.BindValueChanged(v =>
            {
                if (Convert.ToInt32(enumBindable.Value) == v.NewValue)
                    return;
                
                if (Enum.IsDefined(typeof(T), v.NewValue))
                {
                    enumBindable.Value = (T)Enum.ToObject(typeof(T), v.NewValue);
                }
            });
            
            
            enumBindable.BindValueChanged(v =>
            {
                intBindable.Value = Convert.ToInt32(v.NewValue);
            });

            var dropdown = new FormEnumDropdown<T>()
            {
                Caption = Label,
                Current = enumBindable
            };
            
            return (dropdown, intBindable);
        }
    }
    
    
    public record CategoryInfo(Type TypeEnum, string TypeLabel);
    
    public static readonly Dictionary<CategoryType, Dictionary<Enum, List<BaseOption>>> CategoryOptions = new()
    {
        {
            CategoryType.UNANIMATED, new Dictionary<Enum, List<BaseOption>>
            {
                { UnanimatedAction.DefaultStageScene, [new EnumStringOption<StageSceneAction>("Stage")] },
                { UnanimatedAction.ShowBackgroundVideo, [] },
                { UnanimatedAction.LegacyCameraUnits, [] }
            }
        },
        {
            CategoryType.Camera, new Dictionary<Enum, List<BaseOption>>
            {
                { CameraAction.Reset, [new IntCheckboxOption("Position Only?", 0)] },
                { CameraAction.CameraTarget, [new EnumStringOption<CameraPoint>("Camera Point")] },
                { CameraAction.ZoomOffset, [new FloatOption("Offset", 0, -10, 10), new IntCheckboxOption("Reversed?", 0)] },
                { CameraAction.ZoomTarget, [new FloatOption("Target", 0, -10, 10)] },
                { CameraAction.RotOffset, [new FloatOption("Degrees", 0, -720, 720), new IntCheckboxOption("Reversed?", 0)] },
                { CameraAction.RotTarget, [new FloatOption("Degrees", 0, -720, 720)] },
                { CameraAction.HorizontalOffset, [new FloatOption("Offset", 0, -10, 10), new IntCheckboxOption("Reversed?", 0)] },
                { CameraAction.HorizontalTarget, [new FloatOption("Target", 0, -10, 10)] },
                {
                    CameraAction.CustomCameraTarget,
                    [
                        new FloatOption("X", 0, -10, 10, 0.01f, new float[] { -5.5f, -1.9f, -1.0f, 0, 1.0f, 1.9f, 5.5f }),
                        new FloatOption("Y", 0, -10, 10, 0.01f, new float[] { 2.0f, 3.5f, 5 }),
                        new FloatOption("Z", 0, -10, 10, 0.01f, new float[] { -6.0f, -8.0f, -8.5f }),
                        new EnumStringOption<CustomModifier>("Modifier")
                    ]
                },
                {
                  CameraAction.CustomCameraOffset,  
                    [
                        new FloatOption("X", 0, -10, 10),
                        new FloatOption("Y", 0, -10, 10),
                        new FloatOption("Z", 0, -10, 10),
                        new EnumStringOption<CustomModifier>("Modifier")
                    ]
                },
                {
                    CameraAction.CustomRotTarget,
                    [
                        new FloatOption("X", 0, -360, 360),
                        new FloatOption("Y", 0, -360, 360),
                        new FloatOption("Z", 0, -360, 360),
                        new EnumStringOption<CustomModifier>("Modifier")
                    ]
                },
                {
                    CameraAction.CustomRotOffset,
                    [
                        new FloatOption("X", 0, -360, 360),
                        new FloatOption("Y", 0, -360, 360),
                        new FloatOption("Z", 0, -360, 360),
                        new EnumStringOption<CustomModifier>("Modifier")
                    ]
                },
                { CameraAction.EaseTime, [new FloatOption("Time (ms)", 0, 0, 5000, 1f)] },
                { CameraAction.EaseMode, [new EnumStringOption<CameraEasing>("Easing")] },
                { CameraAction.FOVTarget, [new FloatOption("Target (Degrees)", 60, 1, 180)] },
                { CameraAction.FOVOffset, [new FloatOption("Offset (Degrees)", 0, -180, 180), new IntCheckboxOption("Reversed?", 0)] }
            }
        },
        {
            CategoryType.Character, new Dictionary<Enum, List<BaseOption>>
            {
                { CharacterAction.Reset, new List<BaseOption>() },
                { CharacterAction.SetCharacter, [new StringOption("Character 1", "Beat", CharacterListText), new StringOption("Character 2", "Quaver", CharacterListText)] }
            }
        },
        {
            CategoryType.Gameplay, new Dictionary<Enum, List<BaseOption>>
            {
                { GameplayAction.ScreenShake, [new IntCheckboxOption("Enabled?", 1)] },
                { GameplayAction.ScreenRot, [new IntCheckboxOption("Enabled?", 1)] },
                { GameplayAction.ScreenZoom, [new IntCheckboxOption("Enabled?", 1)] }
            }
        },
        {
            CategoryType.UI, new Dictionary<Enum, List<BaseOption>>
            {
                { UIAction.ForceLockedUI, [new IntCheckboxOption("Enabled?", 1)] },
                { UIAction.Show, [new StringOption("Elements", "", ShowHideText)] },
                { UIAction.Hide, [new StringOption("Elements", "", ShowHideText)] },
                { UIAction.SlideIn, [new StringOption("Elements", "", SlideInOutText)] },
                { UIAction.SlideOut, [new StringOption("Elements", "", SlideInOutText)] }
            }
        }
        
    };

    public static readonly Dictionary<CategoryType, CategoryInfo> CategoryTypeInfo = new()
    {
        { CategoryType.Camera, new CategoryInfo(typeof(CameraAction), "Type") },
        { CategoryType.UNANIMATED, new CategoryInfo(typeof(UnanimatedAction), "Type") },
        { CategoryType.Character, new CategoryInfo(typeof(CharacterAction), "Type") },
        { CategoryType.Gameplay, new CategoryInfo(typeof(GameplayAction), "Type") },
        { CategoryType.StageScene, new CategoryInfo(typeof(StageSceneAction), "New Stage") },
        { CategoryType.UI, new CategoryInfo(typeof(UIAction), "Type") }
    };
    
    
}