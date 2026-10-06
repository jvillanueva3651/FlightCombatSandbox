using System.IO;
using Core;
using Godot;

/// <summary>
/// Loads the base Pack when the game starts. It's an autoload: Godot creates it before the
/// first scene and keeps it for the whole run, so any script can read Packs.Base.
/// </summary>
public partial class Packs : Node
{
    public static Pack Base { get; private set; }

    // Loads on entering the tree rather than in _Ready, so the Controls autoload after it can bind
    // its actions with the Feel Settings' dead zone.
    public override void _EnterTree()
    {
        Base = PackLoader.Load(BaseFolder());
        GD.Print($"Pack '{Base.Id}' {Base.Version}: {Base.Feel}");
        foreach (var (id, unitType) in Base.UnitTypes)
            GD.Print($"Pack '{Base.Id}' {Base.Version}: {id} {unitType.Handling}");
    }

    // The base Pack sits beside game/ when run from the editor, and beside the .exe in exported builds.
    static string BaseFolder()
    {
        var root = OS.HasFeature("editor")
            ? Path.Combine(ProjectSettings.GlobalizePath("res://"), "..")
            : Path.GetDirectoryName(OS.GetExecutablePath());
        return Path.GetFullPath(Path.Combine(root, "packs", "base"));
    }
}
