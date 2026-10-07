namespace osu.Game.Custom;


public interface IExportsUnbeatable
{
    public void ExportToUnbeatable();
    
    public bool IsWebsocketAvailable();

    public void ExportMap();

    public void TestAtPracticeTime();
}