namespace CCC.Domain;

public static class StringExtensions
{
    public static string CanonicalName(this string name) => name.Trim().ToLowerInvariant();
}