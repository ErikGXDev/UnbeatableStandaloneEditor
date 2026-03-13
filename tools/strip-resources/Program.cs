using Mono.Cecil;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: strip-resources <source.dll> <output.dll>");
    return 1;
}

string sourcePath = Path.GetFullPath(args[0]);
string outputPath = Path.GetFullPath(args[1]);

// Embedded resource name prefixes to REMOVE.
// Resource names look like: osu.Game.Resources.Tracks.circles.mp3
var stripPrefixes = new[]
{
    "osu.Game.Resources.Tracks.",                  // Background music tracks        (~10.2 MB)
    "osu.Game.Resources.Samples.Multiplayer.",     // Multiplayer sounds              (~7.0 MB)
    "osu.Game.Resources.Samples.Results.",         // Results-screen sounds           (~5.7 MB)
    "osu.Game.Resources.Textures.Intro.",          // Intro-screen textures           (~5.6 MB)
    "osu.Game.Resources.Skins.Retro.",             // Classic/retro skin              (~2.0 MB)
    "osu.Game.Resources.Samples.DailyChallenge.",  // Daily challenge sounds          (~0.9 MB)
    "osu.Game.Resources.Samples.Intro.",           // Intro-screen sounds             (~0.6 MB)
    "osu.Game.Resources.Samples.MedalSplash.",     // Medal unlock sounds             (~0.2 MB)
    "osu.Game.Resources.Textures.MedalSplash.",    // Medal unlock textures           (~0.03 MB)
    "osu.Game.Resources.Skins.Legacy."
};

Console.WriteLine($"Source : {sourcePath}");
Console.WriteLine($"Output : {outputPath}");
Console.WriteLine();

// Read with Cecil
var assembly = AssemblyDefinition.ReadAssembly(sourcePath);
var toRemove = assembly.MainModule.Resources
    .Where(r => stripPrefixes.Any(p => r.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
    .ToList();

long savedBytes = 0;
foreach (var resource in toRemove)
{
    long size = ((EmbeddedResource)resource).GetResourceData().Length;
    Console.WriteLine($"  [-] {size / 1024.0,7:N1} KB  {resource.Name}");
    assembly.MainModule.Resources.Remove(resource);
    savedBytes += size;
}

int kept    = assembly.MainModule.Resources.Count;
int skipped = toRemove.Count;

assembly.Write(outputPath);

long origSize = new FileInfo(sourcePath).Length;
long newSize  = new FileInfo(outputPath).Length;

Console.WriteLine();
Console.WriteLine($"  Kept:    {kept,4} resources");
Console.WriteLine($"  Removed: {skipped,4} resources  ({savedBytes / 1024.0 / 1024.0:F1} MB content)");
Console.WriteLine($"  DLL:     {origSize / 1024.0 / 1024.0:F1} MB  ->  {newSize / 1024.0 / 1024.0:F1} MB  (saved {(origSize - newSize) / 1024.0 / 1024.0:F1} MB)");
return 0;
