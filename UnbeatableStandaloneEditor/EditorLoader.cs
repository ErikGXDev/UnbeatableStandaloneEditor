
using osu.Framework.Screens;

using osu.Game.Screens.Edit;

namespace UbStandaloneEditor;


public partial class EditorLoader : osu.Game.Screens.Edit.EditorLoader
{
    
    
    // Allow the framework to exit this screen normally
    public override bool AllowUserExit => true;

    // When Editor exits and we resume, immediately exit the loader too
    // so the user lands back on BeatmapPickerScreen
    public override void OnResuming(ScreenTransitionEvent e)
    {
        base.OnResuming(e);
        this.Exit();
    }

    public override void OnEntering(ScreenTransitionEvent e)
    {
        base.OnEntering(e);
        this.Push(CreateEditor());
    }
    
   
    
    protected override Editor CreateEditor() => new Editor(this);
    
}
