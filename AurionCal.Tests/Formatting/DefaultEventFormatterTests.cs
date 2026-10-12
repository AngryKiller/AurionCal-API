using AurionCal.Api.Services.Formatting;

namespace AurionCal.Tests.Formatting;

public class DefaultEventFormatterTests
{
    private readonly DefaultEventFormatter _formatter = new(TestData.School("other", "default"));

    [Fact]
    public void AnyType_IsFormattedAsGenericCourse()
    {
        var result = _formatter.Format(TestData.Event("Room 1\nAlgorithms\nEXAM_SURV\nMr Smith", "EXAM_SURV"), FormatOptions.Default);

        Assert.Equal("Room 1", result.Location);
        Assert.Equal("Algorithms - Mr Smith (Épreuve)", result.Summary);
    }

    [Fact]
    public void Entreprise_IsNeverHidden()
        => Assert.True(_formatter.IsVisible(TestData.Event("Room 1\nENTREPRISE", "ENTREPRISE"), FormatOptions.Default));

    [Fact]
    public void ExamAccommodations_AreIgnored()
    {
        var result = _formatter.Format(TestData.Event("Room 1\nAlgo\nHoraire TT : 8h00 - 11h30\nEXAM_SURV", "EXAM_SURV"), new FormatOptions(ExamAccommodations: true));

        Assert.Equal(10, result.End.Hour);
    }
}
