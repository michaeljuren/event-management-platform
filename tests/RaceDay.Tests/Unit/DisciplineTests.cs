using RaceDay.Api.Models;

namespace RaceDay.Tests.Unit;

public class DisciplineTests
{
    [Theory]
    [InlineData("run", "Running")]
    [InlineData("Walk", "Walking")]
    [InlineData(" cycle ", "Cycling")]
    [InlineData("Multisport", "Multisport")]
    [InlineData("swim", null)]
    [InlineData(null, null)]
    public void Normalise_MapsEventTypesToStoredValues(string? input, string? expected)
    {
        Assert.Equal(expected, Discipline.Normalise(input));
    }
}
