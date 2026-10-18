using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Testing;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;

namespace osu.Game.Rulesets.UMania.Edit.Setup;

public partial class UbOfficialEditorCheckbox : FormCheckBox
{
    public UbOfficialEditorCheckbox()
    {
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        
        
    }
    
    [Resolved]
    private OsuColour colours { get; set; } = null!;

    protected override void LoadComplete()
    {
        base.LoadComplete();
        
        this.ChildrenOfType<FormFieldCaption>().ForEach(caption =>
        {
            caption.Colour = colours.Yellow;
        });
    }
}