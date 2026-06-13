using osu.Framework.Screens;
using osu.Game.Screens.Edit;

namespace UnbeatableStandaloneEditor;


public partial class EditorLoader : osu.Game.Screens.Edit.EditorLoader
{

    public bool IsNewBeatmap;

    public EditorLoader(bool isNewBeatmap)
    {
        IsNewBeatmap = isNewBeatmap;
     }

    // Allow the framework to exit this screen normally
    public override bool AllowUserExit => true;

    // When Editor exits and we resume, immediately exit the loader too
    // so the user lands back on BeatmapPickerScreen
    public override void OnResuming(ScreenTransitionEvent e)
    {
        base.OnResuming(e);

        if (!ValidForResume)
            this.Exit();
    }

    public override void OnEntering(ScreenTransitionEvent e)
    {
        base.OnEntering(e);
        var editor = CreateEditor();
        editor.IsActuallyNewBeatmap = IsNewBeatmap;
        this.Push(editor);

        if (IsNewBeatmap)
        {
            editor.Mode.Value = EditorScreenMode.SongSetup;
        }
        else
        {
            editor.Mode.Value = EditorScreenMode.Compose;
        }

        ValidForResume = false;
    }

    protected override Editor CreateEditor() => new Editor(this);
}
