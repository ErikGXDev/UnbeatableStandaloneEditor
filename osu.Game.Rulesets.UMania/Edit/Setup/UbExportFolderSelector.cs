using System.IO;
using HarmonyLib;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Screens.Edit.Setup;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Rulesets.UMania.Edit.Setup;

public partial class UbExportFolderSelector : FormBeatmapFileSelector
{

    public Bindable<string> SelectedDirectory { get; } = new Bindable<string>();

    public UbExportFolderSelector(bool beatmapHasMultipleDifficulties, params string[] handledExtensions)
        : base(beatmapHasMultipleDifficulties, handledExtensions)
    {
        SelectedDirectory.BindValueChanged(dir =>
        {
            var path = dir.NewValue;

            Traverse.Create(this).Field<OsuSpriteText>("filenameText").Value.Text = path;

            var placeHolderTraverse = Traverse.Create(this).Field<OsuSpriteText>("placeholderText");
            if (path.Length > 0)
            {
                placeHolderTraverse.Value.Alpha = 0;
            }
            else
            {
                placeHolderTraverse.Value.Alpha = 1;
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

        private Bindable<string> selectedDirectory;

        public UbExportChoosePopover(string[] handledExtensions, Bindable<FileInfo?> current, string? chooserPath,
            bool beatmapHasMultipleDifficulties, Bindable<string> selectedDirectory)
            : base(handledExtensions, current, chooserPath)
        {
            this.beatmapHasMultipleDifficulties = beatmapHasMultipleDifficulties;

            this.selectedDirectory = selectedDirectory;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new InputBlockingContainer()
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Child =
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
                    }
            });
        }

        protected override void OnFileSelected(FileInfo file)
        {
            return;
        }
    }
}
