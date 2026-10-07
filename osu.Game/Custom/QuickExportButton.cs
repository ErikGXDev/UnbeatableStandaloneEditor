// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Setup;
using osuTK;

namespace osu.Game.Custom
{

    public enum QuickExportAction
    {
        [Description("None (Hidden)")]
        None,
        
        [Description("Export to File/Folder")]
        ExportToFile,
        
        [Description("Export via Websocket")]
        ExportWebsocket,
        
        [Description("Export with Practice Mode")]
        ExportPractice
    }
    
    public partial class QuickExportButton : OsuButton
    {
        [Resolved]
        private OsuColour colours { get; set; } = null!;
        
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private IExportsUnbeatable exports;

        protected override SpriteText CreateText() => new OsuSpriteText
        {
            Depth = -1,
            Origin = Anchor.Centre,
            Anchor = Anchor.Centre,
            Font = OsuFont.TorusAlternate.With(weight: FontWeight.Light, size: 24),
            Shadow = false
        };

        private Drawable foundexport;

        [BackgroundDependencyLoader]
        private void load(EditorBeatmap beatmap, Editor editor, OsuConfigManager config)
        {
            
            var exportActionInt = config.Get<int>(OsuSetting.EditorQuickExportMode);
            
            var exportAction = (QuickExportAction)exportActionInt;

            if (exportAction == QuickExportAction.None)
            {
                Alpha = 0;
                return;
            }
            
            BackgroundColour = colourProvider.Background3;

            Content.CornerRadius = 0;

            Add(new SpriteIcon()
            {
                Icon = FontAwesome.Solid.FileExport,
                Size = new Vector2(20),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Colour = colourProvider.Light3,
                X = 3
            });


            var ruleset = beatmap.BeatmapInfo.Ruleset.CreateInstance();

            var editorSections = ruleset.CreateEditorSetupSections().SelectMany(s => s.ChildrenOfType<SetupSection>());
            
            Drawable exportingSection = editorSections.FirstOrDefault(s => s.Title == "Exporting");

            if (exportingSection == null)
                exportingSection = Empty();

            if (exportingSection is IExportsUnbeatable exportSection)
            {
                exports = exportSection;
                Add(exportingSection.With(e => e.Alpha = 0f));

                Action = () =>
                {
                    switch (exportAction)
                    {
                        case QuickExportAction.ExportToFile:
                            exports.ExportMap();
                            break;
                        case QuickExportAction.ExportWebsocket:
                            if (exports.IsWebsocketAvailable())
                                exports.ExportToUnbeatable();
                            else
                                this.FlashColour(colours.Red, 500, Easing.OutQuint);
                            break;
                        case QuickExportAction.ExportPractice:
                            if (exports.IsWebsocketAvailable()) 
                                exports.TestAtPracticeTime();
                            else
                                this.FlashColour(colours.Red, 500, Easing.OutQuint);
                            break;
                    }
                };
            }
            
            
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            Background.FadeColour(colourProvider.Background2, 500, Easing.OutQuint);
            // don't call base in order to block scale animation
            return false;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            Background.FadeColour(colourProvider.Background3, 300, Easing.OutQuint);
            // don't call base in order to block scale animation
        }
    }
}
