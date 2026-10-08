using Microsoft.Extensions.Configuration;
using AurionCal.Api.Schools;

namespace AurionCal.Tests.Schools;

public class SchoolsOptionsValidatorTests
{
    private readonly SchoolsOptionsValidator _validator = new();

    private static SchoolsOptions Valid() => new()
    {
        DefaultSchoolId = "junia",
        Items = [TestData.School("junia")]
    };

    [Fact]
    public void Valid_Succeeds()
        => Assert.True(_validator.Validate(null, Valid()).Succeeded);

    [Fact]
    public void NoSchool_Fails()
    {
        var options = Valid();
        options.Items = [];

        Assert.Contains(_validator.Validate(null, options).Failures!, f => f.Contains("No school"));
    }

    [Fact]
    public void DuplicateIds_FailEvenWhenCaseDiffers()
    {
        var options = Valid();
        options.Items = [TestData.School("junia"), TestData.School("JUNIA")];

        Assert.Contains(_validator.Validate(null, options).Failures!, f => f.Contains("Duplicate"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("/relative")]
    [InlineData("ftp://aurion.example.com")]
    public void InvalidAurionBaseUrl_Fails(string url)
    {
        var options = Valid();
        options.Items = [TestData.School("junia") with { AurionBaseUrl = url }];

        Assert.Contains(_validator.Validate(null, options).Failures!, f => f.Contains("AurionBaseUrl"));
    }

    [Fact]
    public void MissingEmailDomains_Fails()
    {
        var options = Valid();
        options.Items = [TestData.School("junia") with { EmailDomains = [] }];

        Assert.Contains(_validator.Validate(null, options).Failures!, f => f.Contains("email domain"));
    }

    [Fact]
    public void MissingName_Fails()
    {
        var options = Valid();
        options.Items = [TestData.School("junia") with { Name = "" }];

        Assert.Contains(_validator.Validate(null, options).Failures!, f => f.Contains("name"));
    }

    [Fact]
    public void DefaultSchoolNotInItems_Fails()
    {
        var options = Valid();
        options.DefaultSchoolId = "ghost";

        Assert.Contains(_validator.Validate(null, options).Failures!, f => f.Contains("DefaultSchoolId"));
    }

    [Fact]
    public void ShippedSchoolsJson_IsValid()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "schools.json");
        var config = new ConfigurationBuilder().AddJsonFile(path).Build();
        var options = new SchoolsOptions();
        config.GetSection(SchoolsOptions.SectionName).Bind(options);

        Assert.True(_validator.Validate(null, options).Succeeded);
        Assert.Contains(options.Items, s => s.Id == options.DefaultSchoolId);
    }
}
