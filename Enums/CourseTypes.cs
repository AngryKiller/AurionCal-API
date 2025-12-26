namespace AurionCal.Api.Enums;

public static class RawCourseTypes
{
    public const string CoursTd = "COURS_TD";
    public const string CoursTp = "TP";
    public const string Projet = "PROJET";
    public const string Epreuve = "est-epreuve";
    public const string AutoAppr = "AUTO_APPR";
}

public enum CourseType
{
    Unknown = 0,
    CoursTd,
    CoursTp,
    Projet,
    Epreuve,
    AutoAppr
}

