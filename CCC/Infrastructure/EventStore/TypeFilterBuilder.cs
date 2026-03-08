using System.Linq.Expressions;
using System.Text.Json;

namespace CCC.Infrastructure.EventStore;

class TypeFilterBuilder<T>
{
    private readonly Dictionary<string, string> _properties = new();

    public TypeFilterBuilder<T> With<TValue>(Expression<Func<T, TValue>> property, TValue value)
        => Or(property, value);

    public TypeFilterBuilder<T> Or<TValue>(Expression<Func<T, TValue>> property, TValue value)
    {
        if (property.Body is not MemberExpression member)
            throw new ArgumentException("Property selector must be a direct member access (e.g. e => e.Email).", nameof(property));
        _properties[member.Member.Name] = JsonSerializer.Serialize(value);
        return this;
    }

    internal IReadOnlyDictionary<string, string> Properties => _properties;
}
