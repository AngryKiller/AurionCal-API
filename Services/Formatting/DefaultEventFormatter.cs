using AurionCal.Api.Schools;

namespace AurionCal.Api.Services.Formatting;

/// <summary>
/// Generic formatter for schools without specific rules: the location is the first title line,
/// the rest is read as [Name, Teacher].
/// </summary>
public sealed class DefaultEventFormatter(School school) : EventFormatterBase(school);
