using AurionCal.Api.Schools;
using Microsoft.Extensions.Options;

namespace AurionCal.Tests.Schools;

public class ConfigSchoolCatalogTests
{
    private static ConfigSchoolCatalog CreateCatalog(params School[] schools)
        => new(Options.Create(new SchoolsOptions
        {
            DefaultSchoolId = "junia",
            Items = [.. schools]
        }));

    private static School School(string id, params string[] domains)
        => TestData.School(id) with { EmailDomains = [.. domains] };

    private readonly ConfigSchoolCatalog _catalog = CreateCatalog(
        School("junia", "junia.com", "student.junia.com"),
        School("other", "*.other.edu"));

    [Fact]
    public void Default_IsTheConfiguredDefaultSchool()
        => Assert.Equal("junia", _catalog.Default.Id);

    [Fact]
    public void All_ReturnsEverySchool()
        => Assert.Equal(["junia", "other"], _catalog.All.Select(s => s.Id));

    [Theory]
    [InlineData("junia")]
    [InlineData("JUNIA")]
    public void GetById_IsCaseInsensitive(string id)
        => Assert.Equal("junia", _catalog.GetById(id)?.Id);

    [Fact]
    public void GetById_Unknown_ReturnsNull()
        => Assert.Null(_catalog.GetById("nope"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_EmptyId_ReturnsDefault(string? id)
        => Assert.Equal("junia", _catalog.Resolve(id)?.Id);

    [Fact]
    public void Resolve_KnownId_ReturnsThatSchool()
        => Assert.Equal("other", _catalog.Resolve(" other ")?.Id);

    [Fact]
    public void Resolve_UnknownId_ReturnsNull()
        => Assert.Null(_catalog.Resolve("nope"));

    [Theory]
    [InlineData("a@junia.com", true)]
    [InlineData("a@student.junia.com", true)]
    [InlineData("a@JUNIA.COM", true)]
    [InlineData("a@evil.com", false)]
    [InlineData("a@notjunia.com", false)]
    [InlineData("a@junia.com.evil.com", false)]
    [InlineData("a@sub.junia.com", false)]
    [InlineData("junia.com", false)]
    [InlineData("a@", false)]
    [InlineData("", false)]
    public void EmailMatches_ExactDomains(string email, bool expected)
        => Assert.Equal(expected, _catalog.EmailMatches(_catalog.GetById("junia")!, email));

    [Theory]
    [InlineData("a@cs.other.edu", true)]
    [InlineData("a@x.y.other.edu", true)]
    [InlineData("a@other.edu", false)]
    [InlineData("a@evilother.edu", false)]
    public void EmailMatches_WildcardDomain_AcceptsSubdomainsOnly(string email, bool expected)
        => Assert.Equal(expected, _catalog.EmailMatches(_catalog.GetById("other")!, email));

    [Fact]
    public void Constructor_DefaultSchoolMissing_Throws()
        => Assert.ThrowsAny<Exception>(() => new ConfigSchoolCatalog(Options.Create(new SchoolsOptions
        {
            DefaultSchoolId = "missing",
            Items = [School("junia", "junia.com")]
        })));
}
