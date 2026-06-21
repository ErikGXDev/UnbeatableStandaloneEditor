using System.ComponentModel;
using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Overlays;
using UnbeatableStandaloneEditor.Components;

namespace UnbeatableStandaloneEditor.BeatmapPicker;

public enum SortMode
{
    Artist,
    Title,
    [Description("Last Edited")] LastEdited
}

public partial class SortButton : BlankButton
{
    [Resolved] private OverlayColourProvider colours { get; set; } = null!;

    [Resolved] private EditorConfigManager editorConfig { get; set; } = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        Anchor = Anchor.CentreRight;
        Origin = Anchor.CentreRight;
        X = -158;
        Width = 148;
        Height = 32;
        Text = "Sort by: ...";
        Colour = colours.Colour1;
        BackgroundColour = colours.Background4;
        Action = SwitchSortMode;
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        CurrentSortMode.Value = editorConfig.Get<SortMode>(EditorSetting.SortMode);

        CurrentSortMode.BindValueChanged(v =>
        {
            UpdateSortButtonText();
            editorConfig.SetValue(EditorSetting.SortMode, v.NewValue);
        });

        UpdateSortButtonText();
    }

    public Bindable<SortMode> CurrentSortMode = new(SortMode.Artist);


    public void SwitchSortMode()
    {
        if (CurrentSortMode.Value == SortMode.Artist)
        {
            CurrentSortMode.Value = SortMode.Title;
        }
        else if (CurrentSortMode.Value == SortMode.Title)
        {
            CurrentSortMode.Value = SortMode.LastEdited;
        }
        else if (CurrentSortMode.Value == SortMode.LastEdited)
        {
            CurrentSortMode.Value = SortMode.Artist;
        }
    }


    public void UpdateSortButtonText()
    {
        Text = "Sort by: " + CurrentSortMode.Value.Humanize();
    }

    public object GetSortObject(BeatmapSetInfo set)
    {
        if (CurrentSortMode.Value == SortMode.Artist)
        {
            return set.Metadata.Artist;
        }

        if (CurrentSortMode.Value == SortMode.Title)
        {
            return set.Metadata.Title;
        }

        if (CurrentSortMode.Value == SortMode.LastEdited)
        {
            return -set.DateAdded.UtcTicks;
        }

        return set.Metadata.Artist;
    }
}
