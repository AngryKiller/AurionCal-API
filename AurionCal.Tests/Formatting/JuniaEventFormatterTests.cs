using AurionCal.Api.Services.Formatting;

namespace AurionCal.Tests.Formatting;

public class JuniaEventFormatterTests
{
    private readonly JuniaEventFormatter _formatter = new(TestData.School());

    [Fact]
    public void Course_UsesFirstLineAsLocationAndAppendsTeacher()
    {
        var evt = TestData.Event("IC2 C350 - TV\n\nArchitecture des ordinateurs\nCOURS_TD\nMonsieur LAMBERT", "COURS_TD");

        var result = _formatter.Format(evt, false);

        Assert.Equal("IC2 C350 - TV", result.Location);
        Assert.Equal("Architecture des ordinateurs - Monsieur LAMBERT (TD)", result.Summary);
        Assert.Equal(evt.Title.Trim(), result.Description);
        Assert.Equal("evt-1", result.Uid);
    }

    [Fact]
    public void ProjetAutoGere_IsFormattedLikeACourse_WithItsOwnTypeName()
    {
        var evt = TestData.Event("IC2 C350\nProjet de fin d'année\nPROJET_AUTO_GERE\nMonsieur LAMBERT", "PROJET_AUTO_GERE");

        var result = _formatter.Format(evt, false);

        Assert.Equal("IC2 C350", result.Location);
        Assert.Equal("Projet de fin d'année - Monsieur LAMBERT (Projet auto-géré)", result.Summary);
    }

    [Fact]
    public void ProjetAutoGere_WithoutTeacher_HasNoTrailingDash()
    {
        var result = _formatter.Format(TestData.Event("Salle A1\nProjet web\nPROJET_AUTO_GERE", "PROJET_AUTO_GERE"), false);

        Assert.Equal("Salle A1", result.Location);
        Assert.Equal("Projet web (Projet auto-géré)", result.Summary);
    }

    [Fact]
    public void Course_WithoutTeacher_HasNoTrailingDash()
    {
        var result = _formatter.Format(TestData.Event("Salle A1\nSystèmes embarqués\nPROJET", "PROJET"), false);

        Assert.Equal("Salle A1", result.Location);
        Assert.Equal("Systèmes embarqués (Projet)", result.Summary);
    }

    [Fact]
    public void Course_UnknownClassName_FallsBackToRawClassNameInSummary()
    {
        var result = _formatter.Format(TestData.Event("Salle A1\nNom\nWEIRD TYPE\nProf", "WEIRD TYPE"), false);

        Assert.Equal("Nom - WEIRD TYPE (WEIRD TYPE)", result.Summary);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \r\n ")]
    public void EmptyTitle_UsesPlaceholderSummary(string title)
    {
        var result = _formatter.Format(TestData.Event(title, "TP"), false);

        Assert.StartsWith("Sans titre", result.Summary);
        Assert.Equal(string.Empty, result.Location);
    }

    [Fact]
    public void Exam_ExtractsRoomAndNameAndDropsTechnicalCode()
    {
        var result = _formatter.Format(TestData.Event("Salle C301\nEXAM_SURV\nSciences de l'ingénieur", "EXAM_SURV"), false);

        Assert.Equal("Salle C301", result.Location);
        Assert.Equal("Sciences de l'ingénieur (Épreuve)", result.Summary);
    }

    [Fact]
    public void Exam_WithSingleDistinctLine_NameOnlyAndFirstLineIsLocation()
    {
        var result = _formatter.Format(TestData.Event("Fabrication additive\nEXAM_SURV", "EXAM_SURV"), false);

        Assert.Equal("Fabrication additive (Épreuve)", result.Summary);
        Assert.Equal(string.Empty, result.Location);
    }

    [Fact]
    public void Exam_WithAccommodations_OverridesTimesWhenEnabled()
    {
        var evt = TestData.Event("Salle C301\nEXAM_SURV\nAlgo\nHoraire TT : 8h00 - 11h30", "EXAM_SURV");

        var result = _formatter.Format(evt, true);

        Assert.Equal(8, result.Start.Hour);
        Assert.Equal(0, result.Start.Minute);
        Assert.Equal(11, result.End.Hour);
        Assert.Equal(30, result.End.Minute);
        Assert.Equal("Algo (Épreuve)", result.Summary);
    }

    [Fact]
    public void Exam_WithAccommodations_KeepsOriginalTimesWhenDisabled()
    {
        var evt = TestData.Event("Salle C301\nEXAM_SURV\nAlgo\nHoraire TT : 8h00 - 11h30", "EXAM_SURV");

        var result = _formatter.Format(evt, false);

        Assert.Equal(10, result.End.Hour);
        Assert.Equal("Algo (Épreuve)", result.Summary);
    }

    [Theory]
    [InlineData("Horaire TT : 9h-8h")]      // end before start
    [InlineData("Horaire TT : n/a")]        // no dash
    [InlineData("Horaire TT 8h00 - 11h00")] // no colon
    [InlineData("Horaire TT : ab - cd")]    // unparsable times
    public void Exam_WithInvalidAccommodationsLine_KeepsOriginalTimes(string accommodationsLine)
    {
        var evt = TestData.Event($"Salle C301\nEXAM_SURV\nAlgo\n{accommodationsLine}", "EXAM_SURV");

        var result = _formatter.Format(evt, true);

        Assert.Equal(10, result.End.Hour);
        Assert.Equal("Algo (Épreuve)", result.Summary);
    }

    [Fact]
    public void Exam_AccommodationsLineNeverLeaksIntoSummaryOrLocation()
    {
        var result = _formatter.Format(TestData.Event("Salle C301\nEXAM_SURV\nAlgo\nHoraire TT : 8h00 - 11h30", "EXAM_SURV"), true);

        Assert.DoesNotContain("Horaire TT", result.Summary);
        Assert.DoesNotContain("Horaire TT", result.Location);
    }

    [Fact]
    public void Rattrapage_UsesRoomTitleAndTeacher()
    {
        var evt = TestData.Event(
            "Salle C854, IC2, 41 Bvd Vauban\nCours de rattrapage de l'UE Sciences de l'ingénieur\nRATTRAPAGE_SURV\nMonsieur LAMBERT",
            "RATTRAPAGE_SURV");

        var result = _formatter.Format(evt, false);

        Assert.Equal("Salle C854, IC2, 41 Bvd Vauban", result.Location);
        Assert.Equal("Cours de rattrapage de l'UE Sciences de l'ingénieur - Monsieur LAMBERT (Rattrapage)", result.Summary);
    }

    [Fact]
    public void Rattrapage_HandlesLeadingLineBreak()
    {
        // A leading <br> arrives as a leading line break
        var evt = TestData.Event(
            "\nSalle C854\nCours de rattrapage\nRATTRAPAGE_SURV\nMonsieur LAMBERT", "RATTRAPAGE_SURV");

        var result = _formatter.Format(evt, false);

        Assert.Equal("Salle C854", result.Location);
        Assert.Equal("Cours de rattrapage - Monsieur LAMBERT (Rattrapage)", result.Summary);
    }

    [Fact]
    public void Conference_UsesSecondLineAsTitle()
    {
        var result = _formatter.Format(TestData.Event("Amphi\nConf IA\nCONF", "CONF"), false);

        Assert.Equal("Amphi", result.Location);
        Assert.Equal("Conf IA (Conférence)", result.Summary);
    }

    [Fact]
    public void Conference_WithOnlyTheTypeLine_KeepsLegacyBehavior()
    {
        // Legacy behaviour kept as is: the type line is used as the title
        var result = _formatter.Format(TestData.Event("Amphi\nCONF", "CONF"), false);

        Assert.Equal("Amphi", result.Location);
        Assert.Equal("CONF (Conférence)", result.Summary);
    }

    [Fact]
    public void Times_AreConvertedToParisTimeZone()
    {
        var result = _formatter.Format(TestData.Event("Salle A1\nNom\nTP", "TP"), false);

        Assert.Equal("Europe/Paris", result.Start.TzId);
        Assert.Equal("Europe/Paris", result.End.TzId);
    }
}
