namespace Fateforged.Data.Academy;

/// <summary>
/// Strongly-typed identifier for academy courses/classes.
/// </summary>
public readonly record struct CourseId(string Value)
{
    public override string ToString() => Value;

    public static implicit operator string(CourseId id) => id.Value;

    public static explicit operator CourseId(string value) => new(value);

    public static CourseId FromString(string id) => new(id);

    public bool HasValue => !string.IsNullOrEmpty(Value);

    public static readonly CourseId None = new("");
}

/// <summary>
/// Course identifiers for the academy curriculum.
/// </summary>
public static class CourseIds
{
    public static readonly CourseId IntroductionToMagic101 = new("introduction_to_magic_101");
    public static readonly CourseId SummoningBasics = new("summoning_basics");
    public static readonly CourseId PracticalSpellcraft = new("practical_spellcraft");
    public static readonly CourseId IntroToFire = new("intro_to_fire");
    public static readonly CourseId IntroToWater = new("intro_to_water");
    public static readonly CourseId IntroToEarth = new("intro_to_earth");
    public static readonly CourseId IntroToAir = new("intro_to_air");

    public static readonly CourseId FoundationsOfMagicII = new("foundations_of_magic_ii");
    public static readonly CourseId IntroductionToEmpowerment = new(
        "introduction_to_empowerment"
    );
    public static readonly CourseId IntroductionToManaChanneling = new(
        "introduction_to_mana_channeling"
    );

    public static readonly CourseId FirePracticumI = new("fire_practicum_i");
    public static readonly CourseId WaterPracticumI = new("water_practicum_i");
    public static readonly CourseId EarthPracticumI = new("earth_practicum_i");
    public static readonly CourseId AirPracticumI = new("air_practicum_i");
}
