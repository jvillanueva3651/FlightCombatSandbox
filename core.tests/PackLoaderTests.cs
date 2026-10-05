namespace Core.Tests;

public class PackLoaderTests
{
    [Fact]
    public void Loads_the_base_pack_with_the_light_fighter()
    {
        var pack = PackLoader.Load(BasePackFolder());

        Assert.Equal("base", pack.Id);
        Assert.Contains("light-fighter", pack.UnitTypes);
    }

    // Walks up from the test binaries (core.tests/bin/...) to the repository's packs/base.
    static string BasePackFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!Directory.Exists(Path.Combine(dir.FullName, "packs", "base")))
            dir = dir.Parent ?? throw new DirectoryNotFoundException("packs/base not found above the test binaries.");
        return Path.Combine(dir.FullName, "packs", "base");
    }
}
