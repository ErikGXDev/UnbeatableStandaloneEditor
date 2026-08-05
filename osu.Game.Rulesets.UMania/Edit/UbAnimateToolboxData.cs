using System;
using System.Collections.Generic;

namespace osu.Game.Rulesets.UMania.Edit;

using System.ComponentModel;

public enum ControlMode
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
   
    
    public record ControlModeParameters(
        Type BType,               // Type of B (enum or numeric)
        string BLabel,            // Label for B (e.g., "Zoom Amount")
        bool RequiresC,           // Whether C is required
        bool RequiresD,           // Whether D is required
        string CLabel = "Y",      // Label for C (default: "Y")
        string DLabel = "Z",       // Label for D (default: "Z")
        int[]? bMarkers = null,
        int[]? cMarkers = null,
        int[]? dMarkers = null
    );
    
    public static readonly Dictionary<ControlMode, ControlModeParameters> ModeParameters =
        new()
        {
            // Reset (A=0) - B is ignored (no params needed)
            { ControlMode.Reset, new ControlModeParameters(typeof(int), "Something?", false, false) },

            // Camera Target Point (A=1) - B is CameraPoint enum
            { ControlMode.CameraTargetPoint, new ControlModeParameters(typeof(CameraPoint), "Camera Point", false, false) },

            // Zoom Offset/Target (A=2,3) - B is number
            { ControlMode.ZoomOffset, new ControlModeParameters(typeof(int), "Offset", false, false) },
            { ControlMode.ZoomTarget, new ControlModeParameters(typeof(int), "Target", false, false) },

            // Rotation Offset/Target (A=4,5) - B is number
            { ControlMode.RotationOffset, new ControlModeParameters(typeof(int), "Degrees", false, false) },
            { ControlMode.RotationTarget, new ControlModeParameters(typeof(int), "Degrees", false, false) },

            // Horizontal Offset/Target (A=6,7) - B is number
            { ControlMode.HorizontalOffset, new ControlModeParameters(typeof(int), "Offset", false, false) },
            { ControlMode.HorizontalTarget, new ControlModeParameters(typeof(int), "Target", false, false) },

            // Custom Camera Target Point (A=8) - B,C,D are number (X,Y,Z)
            { ControlMode.CustomCameraTargetPoint, new ControlModeParameters(typeof(int), "X", true, true, "Y", "Z", new []{-55, -19, -10, 0, 10, 19, 55}, new []{20, 35, 5}, new []{-60, -80, -85}) },

            // Ease Time (A=9) - B is number
            { ControlMode.EaseTime, new ControlModeParameters(typeof(int), "Time (ms)", false, false) },

            // Ease Mode (A=10) - B is EaseModeType enum
            { ControlMode.EaseMode, new ControlModeParameters(typeof(CameraEasing), "Easing", false, false) },

            // Fov Target/Offset (A=11,12) - B is number
            { ControlMode.FovTarget, new ControlModeParameters(typeof(float), "Target (Degrees)", false, false) },
            { ControlMode.FovOffset, new ControlModeParameters(typeof(float), "Offset (Degrees)", false, false) }
        };
}