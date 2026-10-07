namespace Core.Tests;

public class PackLoaderTests : IDisposable
{
    // A copy of the base Pack that each test can save over; xunit makes one per test.
    readonly string _copy = CopyOfBasePack();

    public void Dispose() => Directory.Delete(_copy, recursive: true);

    [Fact]
    public void Loads_the_base_pack_with_the_light_fighter()
    {
        var pack = PackLoader.Load(BasePackFolder());

        Assert.Equal("base", pack.Id);
        Assert.Contains("light-fighter", pack.UnitTypes);
        Assert.Equal(0.1f, pack.Feel.DeadZone);
    }

    [Fact]
    public void Saving_unchanged_values_leaves_the_files_as_they_were()
    {
        PackLoader.Save(PackLoader.Load(_copy), _copy);

        foreach (var file in new[] { "feel.json", Path.Combine("unit-types", "light-fighter.json") })
            Assert.Equal(File.ReadAllText(Path.Combine(BasePackFolder(), file)), File.ReadAllText(Path.Combine(_copy, file)));
    }

    [Fact]
    public void Saved_values_load_back()
    {
        var pack = PackLoader.Load(_copy);
        pack.UnitTypes["light-fighter"].Handling.TopSpeed = 1800;
        pack.Feel.CameraLag = 0.25f;

        PackLoader.Save(pack, _copy);
        var reloaded = PackLoader.Load(_copy);

        Assert.Equal(pack.UnitTypes["light-fighter"].Handling, reloaded.UnitTypes["light-fighter"].Handling);
        Assert.Equal(pack.Feel, reloaded.Feel);
    }

    // Walks up from the test binaries (core.tests/bin/...) to the repository's packs/base.
    static string BasePackFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!Directory.Exists(Path.Combine(dir.FullName, "packs", "base")))
            dir = dir.Parent ?? throw new DirectoryNotFoundException("packs/base not found above the test binaries.");
        return Path.Combine(dir.FullName, "packs", "base");
    }

    static string CopyOfBasePack()
    {
        var copy = Directory.CreateTempSubdirectory("pack-").FullName;
        foreach (var file in Directory.GetFiles(BasePackFolder(), "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(copy, Path.GetRelativePath(BasePackFolder(), file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        return copy;
    }
}
