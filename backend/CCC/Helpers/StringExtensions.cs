namespace CCC.Helpers;

public static class StringExtensions
{
    public static string CanonicalName(this string name) => name.Trim().ToLowerInvariant();
}