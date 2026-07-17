using System;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Screens.Edit.Setup;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Overlays;
using osuTK;
using WebSocketSharp;
using Logger = osu.Framework.Logging.Logger;

namespace osu.Game.Rulesets.UMania.Edit.Setup;

public partial class UbExportFolderSelector : FormBeatmapFileSelector
{

    public Bindable<string> SelectedDirectory { get; } = new Bindable<string>();

    public UbExportFolderSelector(bool beatmapHasMultipleDifficulties, params string[] handledExtensions)
        : base(beatmapHasMultipleDifficulties, handledExtensions)
    {
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        SelectedDirectory.BindValueChanged(dir =>
        {
            
            Logger.Log("Selected export directory: " + dir.NewValue);
            
            var path = dir.NewValue;


            filenameText.Text = path;

            if (path.Length > 0)
            {
                placeholderText.Alpha = 0;
            }
            else
            {
                placeholderText.Alpha = 1;
            }
        });
        

    }

    protected override FileChooserPopover CreatePopover(string[] handledExtensions, Bindable<FileInfo?> current,
        string? chooserPath)
    {
        var popover = new UbExportChoosePopover(handledExtensions, current, chooserPath, false, SelectedDirectory);

        return popover;
    }



    private partial class UbExportChoosePopover : FileChooserPopover
    {
        private readonly bool beatmapHasMultipleDifficulties;

        private Bindable<string> selectedDirectory = new Bindable<string>();

        public UbExportChoosePopover(string[] handledExtensions, Bindable<FileInfo?> current, string? chooserPath,
            bool beatmapHasMultipleDifficulties, Bindable<string> selectedDirectory)
            : base(handledExtensions, current, chooserPath)
        {
            this.beatmapHasMultipleDifficulties = beatmapHasMultipleDifficulties;

            this.selectedDirectory.BindTo(selectedDirectory);
        }
        
        private FormButton.Button userPackagesButton;
        private FormButton.Button customSongsButton;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider provider)
        {
            Add(new InputBlockingContainer()
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Child = new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Spacing = new Vector2(0, 4),
                    Children = new Drawable[]
                    {
                      
                        new FormButton.Button
                        {
                            Anchor = Anchor.BottomRight,
                            Origin = Anchor.BottomRight,
                            Width = 160f,
                            Text = "Use this folder",
                            Action = () =>
                            {
                                
                                selectedDirectory.Value = FileSelector.CurrentPath.Value.FullName;

                                PopoverExtensions.HidePopover(this);
                            },
                        },
                        userPackagesButton = new FormButton.Button
                        {
                            Anchor = Anchor.BottomRight,
                            Origin = Anchor.BottomRight,
                            Colour = provider.Colour2,
                            BackgroundColour = provider.Background3,
                            Width = 160f,
                            Height = 32,
                            Text = "USER_PACKAGES folder",
                            Action = () =>
                            {

                                var userPackagesPath = UbSteamDirectoryFinder.FindUnbeatableUserPackages();
                                
                                if (userPackagesPath == null)
                                {
                                    Logger.Log("Could not find USER_PACKAGES folder.");
                                    
                                    return;
                                }
                                
                                selectedDirectory.Value = userPackagesPath;


                                PopoverExtensions.HidePopover(this);
                            },
                        },
                        customSongsButton = new FormButton.Button()
                        {
                            Anchor = Anchor.BottomRight,
                            Origin = Anchor.BottomRight,
                            Colour = provider.Colour2,
                            BackgroundColour = provider.Background3,
                            Width = 160f,
                            Height = 32,
                            Text = "CustomSongs folder",
                            Action = () =>
                            {
                                var unbeatablePath = UbExportSection.GetCustomSongsDirectory();
                                
                                if (!Directory.Exists(unbeatablePath))
                                    Directory.CreateDirectory(unbeatablePath);
                                
                                selectedDirectory.Value = unbeatablePath;
                                
                                PopoverExtensions.HidePopover(this);
                            }
                        }
                    }
                }
            });
            
            var userPackagesPath = UbSteamDirectoryFinder.FindUnbeatableUserPackages();
            if (userPackagesPath == null)
                userPackagesButton.Alpha = 0;
            
            var customSongsPath = UbExportSection.GetCustomSongsDirectory();
            var customSongsParent = Path.GetDirectoryName(customSongsPath);
            if (!Directory.Exists(customSongsPath) && (customSongsParent == null || !Directory.Exists(customSongsParent)))
            {
                customSongsButton.Alpha = 0;
            }
        }

        protected override void OnFileSelected(FileInfo file)
        {
            return;
        }
    }
}
