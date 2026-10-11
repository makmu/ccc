using CCC.Domain.Types;

namespace CCC.Domain.Tests;

public class TypeTests
{
    [Fact]
    public void Ids_reject_the_empty_guid()
    {
        var error = Assert.Throws<InvalidValueException>(() => TenantId.From(Guid.Empty));
        Assert.Equal("Tenant id must not be empty.", error.Message);

        Assert.Throws<InvalidValueException>(() => TeamId.From(Guid.Empty));
        Assert.Throws<InvalidValueException>(() => ProjectId.From(Guid.Empty));
        Assert.Throws<InvalidValueException>(() => KeyId.From(Guid.Empty));
        Assert.Throws<InvalidValueException>(() => UserId.From(Guid.Empty));
        Assert.Throws<InvalidValueException>(() => SubscriptionModelId.From(Guid.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Names_and_codes_reject_blank_input(string? input)
    {
        Assert.Throws<InvalidValueException>(() => TenantName.From(input));
        Assert.Throws<InvalidValueException>(() => TeamName.From(input));
        Assert.Throws<InvalidValueException>(() => ProjectName.From(input));
        Assert.Throws<InvalidValueException>(() => KeyName.From(input));
        Assert.Throws<InvalidValueException>(() => LanguageCode.From(input));
        Assert.Throws<InvalidValueException>(() => EmailAddress.From(input));
        Assert.Throws<InvalidValueException>(() => UserName.From(input));
        Assert.Throws<InvalidValueException>(() => SubscriptionModelName.From(input));
    }

    [Theory]
    [InlineData("Acme", "acme")]
    [InlineData("  Acme Corp  ", "acme corp")]
    [InlineData("ÄRGER", "ärger")]
    public void Names_keep_the_input_and_canonicalize_by_trimming_and_lowercasing(string input, string canonical)
    {
        var name = TenantName.From(input);

        Assert.Equal(input, name.Value);
        Assert.Equal(canonical, name.Canonical);
        Assert.Equal(canonical, TeamName.From(input).Canonical);
        Assert.Equal(canonical, ProjectName.From(input).Canonical);
    }

    [Theory]
    [InlineData("alice@example.com")]
    [InlineData("a@b")]
    public void EmailAddress_accepts_something_at_something(string input)
    {
        Assert.Equal(input, EmailAddress.From(input).Value);
    }

    [Theory]
    [InlineData("alice")]
    [InlineData("@example.com")]
    [InlineData("alice@")]
    [InlineData("alice @example.com")]
    public void EmailAddress_rejects_what_cannot_be_an_address(string input)
    {
        var error = Assert.Throws<InvalidValueException>(() => EmailAddress.From(input));
        Assert.Equal("Email address is not valid.", error.Message);
    }

    [Fact]
    public void KeyLimit_allows_zero_and_rejects_negative_values()
    {
        Assert.Equal(0, KeyLimit.From(0).Value);
        Assert.Throws<InvalidValueException>(() => KeyLimit.From(-1));
    }

    [Fact]
    public void KeyCount_starts_at_zero_and_rejects_negative_values()
    {
        Assert.Equal(0, KeyCount.Zero.Value);
        Assert.Equal(2, KeyCount.Zero.Increment().Increment().Value);
        Assert.Throws<InvalidValueException>(() => KeyCount.From(-1));
    }

    [Theory]
    [InlineData(1, 2, false)]
    [InlineData(2, 2, true)]
    [InlineData(3, 2, true)]
    public void KeyCount_reaches_a_key_limit_at_or_above_it(int count, int limit, bool reached)
    {
        Assert.Equal(reached, KeyCount.From(count) >= KeyLimit.From(limit));
    }

    [Fact]
    public void Text_allows_empty_text_but_not_a_missing_one()
    {
        Assert.Equal("", Text.From("").Value);
        var error = Assert.Throws<InvalidValueException>(() => Text.From(null));
        Assert.Equal("Text must be provided.", error.Message);
    }
}
