namespace Fateforged.Tests.Projectiles;

using Fateforged.Data.Projectiles;
using Fateforged.Projectiles;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class ProjectileDefinitionsTest
{
    [TestCase]
    public void WindPuff_ConfiguresNonDefaultHitRadius()
    {
        var windPuff = ProjectileDefinitions.Get(ProjectileIds.WindPuff);
        AssertThat(windPuff).IsNotNull();

        // Regression guard: default ProjectileData hit radius is 2.5f, which caused
        // over-large contact checks and "invisible instant hits" for puff projectiles.
        AssertThat(windPuff!.HitRadius).IsEqual(0.45f);
    }
}
