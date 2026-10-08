using AurionCal.Api.Enums;
using AurionCal.Api.Schools;
using AurionCal.Api.Services.Formatting;

namespace AurionCal.Tests.Formatting;

public class CourseTypeMapTests
{
    private readonly CourseTypeMap _map = CourseTypeMap.For(new ParsingOptions());

    [Theory]
    [InlineData("COURS_TD", CourseType.CoursTd)]
    [InlineData("TD", CourseType.CoursTd)]
    [InlineData("TP", CourseType.CoursTp)]
    [InlineData("PROJET", CourseType.Projet)]
    [InlineData("est-epreuve", CourseType.Epreuve)]
    [InlineData("EXAM_SURV", CourseType.Epreuve)]
    [InlineData("RATTRAPAGE_SURV", CourseType.Rattrapage)]
    [InlineData("AUTO_APPR", CourseType.AutoAppr)]
    [InlineData("REUNION", CourseType.Reunion)]
    [InlineData("CONF", CourseType.Conference)]
    [InlineData("TD_AUTO_GERE_PLANIFIE", CourseType.TdAutoGere)]
    public void Parse_KnownCodes(string raw, CourseType expected)
        => Assert.Equal(expected, _map.Parse(raw));

    [Theory]
    [InlineData("cours-td", CourseType.CoursTd)]
    [InlineData("  Cours TD ", CourseType.CoursTd)]
    [InlineData("rattrapage surv", CourseType.Rattrapage)]
    public void Parse_NormalizesCaseSpacesAndDashes(string raw, CourseType expected)
        => Assert.Equal(expected, _map.Parse(raw));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SOMETHING_ELSE")]
    public void Parse_UnknownOrEmpty_ReturnsUnknown(string? raw)
        => Assert.Equal(CourseType.Unknown, _map.Parse(raw));

    [Fact]
    public void For_SchoolOverrides_AddAndReplaceMappings()
    {
        var map = CourseTypeMap.For(new ParsingOptions
        {
            ClassNames = new Dictionary<string, CourseType>
            {
                ["LAB"] = CourseType.CoursTp,
                ["TD"] = CourseType.Projet
            }
        });

        Assert.Equal(CourseType.CoursTp, map.Parse("LAB"));
        Assert.Equal(CourseType.Projet, map.Parse("TD"));
        Assert.Equal(CourseType.CoursTd, map.Parse("COURS_TD"));
    }

    [Fact]
    public void For_SchoolOverrides_DoNotLeakIntoOtherMaps()
    {
        CourseTypeMap.For(new ParsingOptions { ClassNames = new() { ["LAB"] = CourseType.CoursTp } });

        Assert.Equal(CourseType.Unknown, _map.Parse("LAB"));
    }

    [Theory]
    [InlineData("COURS_TD", "TD")]
    [InlineData("EXAM_SURV", "Épreuve")]
    [InlineData("RATTRAPAGE_SURV", "Rattrapage")]
    [InlineData("CONF", "Conférence")]
    [InlineData(null, "Autre")]
    [InlineData("", "Autre")]
    public void ToDisplayNameFromRaw_KnownOrEmpty(string? raw, string expected)
        => Assert.Equal(expected, _map.ToDisplayNameFromRaw(raw));

    [Fact]
    public void ToDisplayNameFromRaw_UnknownType_ReturnsCleanedRawValue()
        => Assert.Equal("Weird  type".Replace("  ", " "), _map.ToDisplayNameFromRaw("  Weird   type "));
}
