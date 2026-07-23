namespace osu.Game.Screens.Edit;

// FIX: This shared state exists so osu.Game does not need a dependency
// on the ruleset.
public static class EditorKeyBasedCharting
{
    // If key-charting is active
    public static bool IsActive { get; set; }
}
