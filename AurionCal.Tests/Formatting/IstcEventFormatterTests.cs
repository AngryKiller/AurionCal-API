using AurionCal.Api.Services.Formatting;
using Ical.Net;

namespace AurionCal.Tests.Formatting;

public class IstcEventFormatterTests
{
    private readonly IstcEventFormatter _formatter = new(TestData.School("istc", "istc"));

    [Fact]
    public void Course_UsesRoomAsLocationAndSubjectTeacherTypeAsSummary()
    {
        var evt = TestData.Event(
            "CONFIRME -  -  -  - M DHULST - B201 - IA et créative cloud - Cours -  - 1ère année - Groupe F", "COURS");

        var result = _formatter.Format(evt, false);

        Assert.Equal("B201", result.Location);
        Assert.Equal("IA et créative cloud - M DHULST (Cours)", result.Summary);
        Assert.Equal(evt.Title, result.Description);
        Assert.Null(result.Status);
    }

    [Fact]
    public void Course_KeepsMultiWordRoom()
    {
        var result = _formatter.Format(TestData.Event(
            "CONFIRME -  -  -  - MME LEPRETRE - A302 Digital Lab - Développement personnel - Cours -  - 1ère année - Groupe F",
            "COURS"), false);

        Assert.Equal("A302 Digital Lab", result.Location);
        Assert.Equal("Développement personnel - MME LEPRETRE (Cours)", result.Summary);
    }

    [Fact]
    public void Course_WithoutGroup_IsParsed()
    {
        var result = _formatter.Format(TestData.Event(
            "CONFIRME -  -  -  - M HINDMARSH - A102 - Electif Majeur : English Talk - Cours -  - 1ère année", "COURS"), false);

        Assert.Equal("A102", result.Location);
        Assert.Equal("Electif Majeur : English Talk - M HINDMARSH (Cours)", result.Summary);
    }

    [Fact]
    public void Course_WithComment_PrependsItToDescription()
    {
        var evt = TestData.Event(
            "CONFIRME -  -  -  - M DEBERNARDI - B911 - Employabilité - Cours - En visio (Teams) - 1ère année - Groupe F",
            "COURS");

        var result = _formatter.Format(evt, false);

        Assert.Equal("Employabilité - M DEBERNARDI (Cours)", result.Summary);
        Assert.Equal($"En visio (Teams)\n\n{evt.Title}", result.Description);
    }

    [Fact]
    public void Cancelled_IsPrefixedAndMarkedCancelled()
    {
        var result = _formatter.Format(TestData.Event(
            "ANNULE_INTERVENANT - ANNULE -  -  - MME GUNERI -  - Economie - Cours -  - 1ère année - Groupe EF", "COURS"), false);

        Assert.Equal("[Annulé] Economie - MME GUNERI (Cours)", result.Summary);
        Assert.Equal(string.Empty, result.Location);
        Assert.Equal(EventStatus.Cancelled, result.Status);
    }

    [Fact]
    public void Exam_MergesDuplicatedRoomAndSubject()
    {
        var evt = TestData.Event(
            "CONFIRME -  -  -  - M BOUTE / MME KENNEY - B015 / B015 - Introduction aux théories de la communication / Introduction aux théories de la communication - Examen écrit - Midterm - 1ère année - Groupe EF",
            "EXAMEN_ECRIT");

        var result = _formatter.Format(evt, false);

        Assert.Equal("B015", result.Location);
        Assert.Equal("Introduction aux théories de la communication - M BOUTE / MME KENNEY (Examen écrit)", result.Summary);
        Assert.StartsWith("Midterm\n\n", result.Description);
    }

    [Fact]
    public void WithoutSubjectOrTeacher_UsesCommentAsName()
    {
        var evt = TestData.Event(
            "CONFIRME -  -  -  -   -  -  - Cours - Sensibilisation Santé Mentale (présence obligatoire) - 1ère année", "COURS");

        var result = _formatter.Format(evt, false);

        Assert.Equal(string.Empty, result.Location);
        Assert.Equal("Sensibilisation Santé Mentale (présence obligatoire) (Cours)", result.Summary);
        Assert.Equal(evt.Title, result.Description);
    }

    [Fact]
    public void MultiLineComment_IsJoinedOnOneLine()
    {
        var result = _formatter.Format(TestData.Event(
            "CONFIRME -  -  -  - M MBADINGA - A202 -  - Présentation - Claire CARRIN\nvisites de prévention en santé\n(obligatoire) - 1ère année - Groupe EF",
            "PRESENTATION"), false);

        Assert.Equal("A202", result.Location);
        Assert.Equal("Claire CARRIN visites de prévention en santé (obligatoire) - M MBADINGA (Présentation)", result.Summary);
    }

    [Fact]
    public void CommentContainingSeparator_IsKeptWhole()
    {
        var evt = TestData.Event(
            "CONFIRME -  -  -  - M ORIGI - B015 - Gestion - Cours - Salle - Bâtiment B - 1ère année - Groupe EF", "COURS");

        var result = _formatter.Format(evt, false);

        Assert.StartsWith("Salle - Bâtiment B\n\n", result.Description);
    }

    [Fact]
    public void UnexpectedLayout_FallsBackToGenericFormat()
    {
        var result = _formatter.Format(TestData.Event("Salle A1\nNom\nProf", "COURS"), false);

        Assert.Equal("Salle A1", result.Location);
        Assert.Equal("Nom - Prof (COURS)", result.Summary);
    }
}
