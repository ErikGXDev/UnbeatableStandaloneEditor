using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.UMania.Edit.Setup
{

    public partial class UbPlaytestButton : UbFormButton
    {
        [Resolved] private EditorClock editorClock { get; set; } = null!;

        public Action ExportToUnbeatable;
        public Action TestAtPracticeTime;

        public UbPlaytestButton()
        {
            Caption = "Test your map in Unbeatable (Through Websocket)";
            ButtonText = "Test Beatmap";
            SecondButtonText = "At time";
            Action = () => ExportToUnbeatable?.Invoke();
            SecondAction = () => TestAtPracticeTime?.Invoke();
        }

        protected override void Update()
        {
            base.Update();

            if (!IsLoaded || editorClock == null)
                return;

            SetSecondButtonText($"From {humanizeTime(editorClock.CurrentTime)}");
        }

        private static string humanizeTime(double milliseconds)
        {
            int totalSeconds = (int)Math.Max(0, milliseconds) / 1000;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes}m{seconds:D2}s";
        }
    }
}
