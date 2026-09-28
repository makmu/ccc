namespace CCC.Domain.ValueObjects;

// Tags and uniqueness checks must keep producing exactly this string, or they
// stop matching events that are already stored.
static class CanonicalForm
{
    public static string Of(string name) => name.Trim().ToLowerInvariant();
}
