using AurionCal.Api.Services;

namespace AurionCal.Tests;

public class EmailNormalizerTests
{
    [Theory]
    [InlineData("a@junia.com", "a@junia.com")]
    [InlineData("  A@Junia.COM ", "a@junia.com")]
    [InlineData("Jean.DUPONT@student.junia.com", "jean.dupont@student.junia.com")]
    public void Normalize_TrimsAndLowercases(string input, string expected)
        => Assert.Equal(expected, EmailNormalizer.Normalize(input));
}
