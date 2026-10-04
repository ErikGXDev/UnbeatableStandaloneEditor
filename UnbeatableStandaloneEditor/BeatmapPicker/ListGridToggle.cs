using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Overlays;
using osuTK;
using UnbeatableStandaloneEditor.Components;

namespace UnbeatableStandaloneEditor.BeatmapPicker;

public partial class ListGridToggle : BlankButton, IHasTooltip
{
    private SpriteIcon listIcon;
    private SpriteIcon gridIcon;

    private readonly BindableBool isGridView;

    public LocalisableString TooltipText { get; private set; } = "Switch to list";

    public ListGridToggle(BindableBool isGridView)
    {
        this.isGridView = isGridView;
        Anchor = Anchor.CentreRight;
        Origin = Anchor.CentreRight;
        Width = 32;
        Height = 32;

        Action = () => isGridView.Toggle();
    }

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colours)
    {
        BackgroundColour = colours.Background4;

        listIcon = new SpriteIcon
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Size = new Vector2(16),
            Icon = FontAwesome.Solid.List
        };

        gridIcon = new SpriteIcon
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Size = new Vector2(16),
            Icon = FontAwesome.Solid.ThLarge
        };

        Add(listIcon);
        Add(gridIcon);

        isGridView.BindValueChanged(_ => updateIcon(), true);
    }

    private void updateIcon()
    {
        if (isGridView.Value)
        {
            gridIcon.Alpha = 1;
            listIcon.Alpha = 0;
        }
        else
        {
            gridIcon.Alpha = 0;
            listIcon.Alpha = 1;
        }

        TooltipText = isGridView.Value ? "Switch to list view" : "Switch to grid view";
    }
}
