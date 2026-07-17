using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Database;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings.Sections.Input;
using osuTK;
using Realms;

namespace UnbeatableStandaloneEditor.Settings;

public partial class EditorKeyBindingsSubsection : KeyBindingsSubsection
{
    private static readonly GlobalAction[] excluded_actions =
    {
        GlobalAction.EditorTestGameplay,
        GlobalAction.EditorDesignMode,
        GlobalAction.EditorToggleRotateControl,
        GlobalAction.EditorToggleMoveControl,
        GlobalAction.EditorToggleScaleControl,
    };

    protected override LocalisableString Header => InputSettingsStrings.EditorSection;

    public EditorKeyBindingsSubsection()
    {
        FlowContent.Spacing = new Vector2(0, 8);

        Defaults = GlobalActionContainer.GetDefaultBindingsFor(GlobalActionCategory.Editor)
            .Where(b => !excluded_actions.Contains((GlobalAction)b.Action));
    }

    protected override Drawable CreateHeader() => Empty();

    protected override IEnumerable<RealmKeyBinding> GetKeyBindings(Realm realm)
    {
        var bindings = realm.All<RealmKeyBinding>()
            .Where(b => b.RulesetName == null && b.Variant == null)
            .Detach();

        var actionsInSection = GlobalActionContainer.GetGlobalActionsFor(GlobalActionCategory.Editor)
            .Where(a => !excluded_actions.Contains(a))
            .Cast<int>()
            .ToHashSet();
        return bindings.Where(kb => actionsInSection.Contains(kb.ActionInt));
    }
}
