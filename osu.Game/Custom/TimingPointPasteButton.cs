using System;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Localisation;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.OSD;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Custom
{
    public partial class TimingPointPasteButton : RoundedButton // Didnt want the triangles, so OsuButton only
    {
        public TimingPointPasteButton() {}

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;
        
        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }
        
        [Resolved]
        private Clipboard hostClipboard { get; set; } = null!;
        
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;
        
        [Resolved(canBeNull: true)] private OnScreenDisplay? onScreenDisplay { get; set; }


        [BackgroundDependencyLoader]
        private void load()
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;
            Size = new Vector2(30);

            // Note to self: Do not use Child = ... because it removes the background of the button for some reason.
            Add(new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(16),
                Icon = FontAwesome.Solid.Clipboard,
                Depth = -1,
            });

            Action = () =>
            {
                try
                {
                    timingPointsOpening();
                } catch (Exception e)
                {
                    showToast("Could not paste from clipboard...");
                    Logger.Log($"Error while trying to paste osu! timing points: {e.Message}");
                }
            };
        }
        
        private void showToast(string title)
        {
            onScreenDisplay?.Display(new BeatmapEditorToast(title));
        }
        
        public partial class BeatmapEditorToast : Toast
        {
            public BeatmapEditorToast(LocalisableString value)
                : base(InputSettingsStrings.EditorSection, value)
            {
            }
        }

        private void timingPointsOpening()
        {
            var clipboard = hostClipboard.GetText();
            
            var corrected = "[General]\nMode: 5\n[TimingPoints]\n" + clipboard; 
            var decoder = new LegacyBeatmapDecoder();
            var reader = new LineBufferedReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(corrected)));
            var beatmap = decoder.Decode(reader);

            var points = beatmap.ControlPointInfo.AllControlPoints.ToArray();

            if (points.Length == 0)
            {
                showToast("No osu! timing points found in clipboard");
                return;
            }
            
            dialogOverlay?.Push(new PasteOsuPointsDialog(
                points.Length,
                addAbove: () =>
                {
                    Logger.Log("Pasting osu! timing points above existing ones");
                    foreach (var point in points)
                    {
                        editorBeatmap.ControlPointInfo.Add(point.Time, point);
                    }
                },
                overwrite: () =>
                {
                    Logger.Log("Overwriting existing timing points with osu timing points");
                    editorBeatmap.ControlPointInfo.Clear();
                    foreach (var point in points)
                    {
                        editorBeatmap.ControlPointInfo.Add(point.Time, point);
                    }
                },
                cancel: () =>
                {
                    Logger.Log("Cancelled pasting osu timing points");
                }
            ));
        }
    }

    public partial class PasteOsuPointsDialog : PopupDialog
    {
        public PasteOsuPointsDialog(int count, Action addAbove, Action overwrite, Action cancel)
        {
            HeaderText = $"Would you like to paste {count} osu! timing points?";

            BodyText =
                "You can choose to either overwrite or merge with your existing timing points. Note that doing this CANNOT BE UNDONE.";
            
            Icon = FontAwesome.Solid.Clipboard;
            
            Buttons = new PopupDialogButton[]
            {
                new PopupDialogDangerousButton
                {
                    Text = @"Overwrite all points!",
                    Action = overwrite
                },
                new PopupDialogDangerousButton()
                {
                    Text = @"Merge with existing points!",
                    Action = addAbove
                },
                new PopupDialogCancelButton
                {
                    Text = "No! Abort mission",
                    Action = cancel
                }
            };
        }
    }
}
