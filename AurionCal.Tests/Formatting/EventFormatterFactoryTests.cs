using AurionCal.Api.Services.Formatting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AurionCal.Tests.Formatting;

public class EventFormatterFactoryTests
{
    private readonly EventFormatterFactory _factory = new(NullLogger<EventFormatterFactory>.Instance);

    [Fact]
    public void For_JuniaParserId_ReturnsJuniaFormatter()
        => Assert.IsType<JuniaEventFormatter>(_factory.For(TestData.School("junia", "junia")));

    [Fact]
    public void For_IstcParserId_ReturnsIstcFormatter()
        => Assert.IsType<IstcEventFormatter>(_factory.For(TestData.School("istc", "istc")));

    [Fact]
    public void For_DefaultParserId_ReturnsDefaultFormatter()
        => Assert.IsType<DefaultEventFormatter>(_factory.For(TestData.School("other", "default")));

    [Fact]
    public void For_UnknownParserId_FallsBackToDefaultFormatter()
        => Assert.IsType<DefaultEventFormatter>(_factory.For(TestData.School("other", "does-not-exist")));

    [Fact]
    public void For_ParserIdIsCaseInsensitive()
        => Assert.IsType<JuniaEventFormatter>(_factory.For(TestData.School("junia", "JUNIA")));

    [Fact]
    public void For_SameSchool_ReturnsCachedInstance()
    {
        var school = TestData.School();

        Assert.Same(_factory.For(school), _factory.For(school));
    }

    [Fact]
    public void For_DifferentSchools_ReturnDifferentInstances()
        => Assert.NotSame(_factory.For(TestData.School("a", "default")), _factory.For(TestData.School("b", "default")));
}
