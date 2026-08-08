using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Tests;

namespace osu.Game.Rulesets.UMania.Edit;

public enum CategoryType
{
    Camera,
    Character,
}

public enum CameraType
{
    [Description("Reset")]
    Reset = 0,

    [Description("Camera Target Point")]
    CameraTargetPoint = 1,

    [Description("Zoom Offset")]
    ZoomOffset = 2,

    [Description("Zoom Target")]
    ZoomTarget = 3,

    [Description("Rotation Offset")]
    RotationOffset = 4,

    [Description("Rotation Target")]
    RotationTarget = 5,

    [Description("Horizontal Offset")]
    HorizontalOffset = 6,

    [Description("Horizontal Target")]
    HorizontalTarget = 7,

    [Description("Custom Camera Target Point")]
    CustomCameraTargetPoint = 8,

    [Description("Ease Time")]
    EaseTime = 9,

    [Description("Ease Mode")]
    EaseMode = 10,

    [Description("FOV Target")]
    FovTarget = 11,

    [Description("FOV Offset")]
    FovOffset = 12
}

public enum CharacterType
{
    Reset = 0,
    
    [Description("Set Character")]
    SetCharacter
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



public class UbAnimateToolboxData
{

    public static string CharacterListText = """
                                             List of available characters:

                                             Beat, Beat (Hoodie), Beat (Guitar), Beat (Up), Beat (Nothing), Clef, Quaver, Quaver (Acoustic), Quaver (CQC), Treble, Rest, Rest (OMF), Eve, Grace, Crest, Crest (Maid), DC, Poco, Apoco, Penny, Sforzando, JamieP, Quaver (Shrimp)
                                             
                                             ---
                                             
                                             Type "Reset" to reset the character.
                                             """;
    
    public abstract class BaseOption
    {
        public string Label { get; protected set; } = string.Empty;
        
        public virtual (Drawable, IBindable) CreateDrawableAndBindable(Action? onValueChanged = null) => (null, null);
        
    }

    public abstract class BaseOption<TBindable> : BaseOption where TBindable : IBindable
    {
    }

    public class IntOption : BaseOption<BindableInt>
    {
        public int DefaultValue { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public int[]? Markers { get; set; }
        

        public IntOption(string label, int defaultValue, int min, int max, int[]? markers = null)
        {
            Label = label;
            DefaultValue = defaultValue;
            Min = min;
            Max = max;
            Markers = markers;
        }

        public override (Drawable, IBindable) CreateDrawableAndBindable(Action? onValueChanged)
        {
            var bindable = new BindableInt() { MinValue = Min, MaxValue = Max, Default = DefaultValue };
            
            bindable.BindValueChanged(v => onValueChanged?.Invoke());

            var slider = new FormSliderBar<int>()
            {
                Caption = Label,
                Current = bindable,
                KeyboardStep = 1
            };
            
            if (Markers != null)
            {
                slider.SetMarkersScheduled(Markers.ToList());
                Logger.Log("Markers set for slider: " + string.Join(", ", Markers));
            }
            
            return (slider, bindable);
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
            CategoryType.Camera, new Dictionary<Enum, List<BaseOption>>
            {
                { CameraType.Reset, [new IntOption("Position Only?", 0, 0, 1)] },
                { CameraType.CameraTargetPoint, [new EnumStringOption<CameraPoint>("Camera Point")] },
                { CameraType.ZoomOffset, [new IntOption("Offset", 0, -100, 100)] },
                { CameraType.ZoomTarget, [new IntOption("Target", 0, -100, 100)] },
                { CameraType.RotationOffset, [new IntOption("Degrees", 0, -180, 180)] },
                { CameraType.RotationTarget, [new IntOption("Degrees", 0, -180, 180)] },
                { CameraType.HorizontalOffset, [new IntOption("Offset", 0, -100, 100)] },
                { CameraType.HorizontalTarget, [new IntOption("Target", 0, -100, 100)] },
                {
                    CameraType.CustomCameraTargetPoint,
                    [
                        new IntOption("X", 0, -100, 100, new[] { -55, -19, -10, 0, 10, 19, 55 }),
                        new IntOption("Y", 0, -100, 100, new[] { 20, 35, 5 }),
                        new IntOption("Z", 0, -100, 100, new[] { -60, -80, -85 })
                    ]
                },
                { CameraType.EaseTime, [new IntOption("Time (ms)", 0, 0, 5000)] },
                { CameraType.EaseMode, [new EnumStringOption<CameraEasing>("Easing")] },
                { CameraType.FovTarget, [new IntOption("Target (Degrees)", 60, 1, 179)] },
                { CameraType.FovOffset, [new IntOption("Offset (Degrees)", 0, -180, 180)] }
            }
           
        },
        {
            CategoryType.Character, new Dictionary<Enum, List<BaseOption>>
            {
                { CharacterType.Reset, new List<BaseOption>() },
                { CharacterType.SetCharacter, [new StringOption("Character 1", "Beat", CharacterListText), new StringOption("Character 2", "Quaver", CharacterListText)] }
            }
        }
        
    };

    public static readonly Dictionary<CategoryType, CategoryInfo> CategoryTypeInfo = new()
    {
        { CategoryType.Camera, new CategoryInfo(typeof(CameraType), "Type") },
        { CategoryType.Character, new CategoryInfo(typeof(CharacterType), "Type") }
    };
    
    
}