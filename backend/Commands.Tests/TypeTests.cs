using CCC.Domain.Types;

namespace CCC.Commands.Tests;

public class TypeTests
{
    [Fact]
    public void Ids_reject_the_empty_guid()
    {
        var error = Assert.Throws<InvalidValueException>(() => TenantId.Create(Guid.Empty));
        Assert.Equal("Tenant id must not be empty.", error.Message);

        Assert.Throws<InvalidValueException>(() => TeamId.Create(Guid.Empty));
        Assert.Throws<InvalidValueException>(() => ProjectId.Create(Guid.Empty));
        Assert.Throws<InvalidValueException>(() => KeyId.Create(Guid.Empty));
        Assert.Throws<InvalidValueException>(() => UserId.Create(Guid.Empty));
        Assert.Throws<InvalidValueException>(() => SubscriptionModelId.Create(Guid.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Names_and_codes_reject_blank_input(string? input)
    {
        Assert.Throws<InvalidValueException>(() => TenantName.Create(input));
        Assert.Throws<InvalidValueException>(() => TeamName.Create(input));
        Assert.Throws<InvalidValueException>(() => ProjectName.Create(input));
        Assert.Throws<InvalidValueException>(() => KeyName.Create(input));
        Assert.Throws<InvalidValueException>(() => LanguageCode.Create(input));
        Assert.Throws<InvalidValueException>(() => EmailAddress.Create(input));
    }

    [Theory]
    [InlineData("Acme", "acme")]
    [InlineData("  Acme Corp  ", "acme corp")]
    [InlineData("ÄRGER", "ärger")]
    public void Names_keep_the_input_and_canonicalize_by_trimming_and_lowercasing(string input, string canonical)
    {
        var name = TenantName.Create(input);

        Assert.Equal(input, name.Value);
        Assert.Equal(canonical, name.Canonical);
        Assert.Equal(canonical, TeamName.Create(input).Canonical);
        Assert.Equal(canonical, ProjectName.Create(input).Canonical);
    }

    [Theory]
    [InlineData("alice@example.com")]
    [InlineData("a@b")]
    public void EmailAddress_accepts_something_at_something(string input)
    {
        Assert.Equal(input, EmailAddress.Create(input).Value);
    }

    [Theory]
    [InlineData("alice")]
    [InlineData("@example.com")]
    [InlineData("alice@")]
    [InlineData("alice @example.com")]
    public void EmailAddress_rejects_what_cannot_be_an_address(string input)
    {
        var error = Assert.Throws<InvalidValueException>(() => EmailAddress.Create(input));
        Assert.Equal("Email address is not valid.", error.Message);
    }

    [Fact]
    public void KeyLimit_allows_zero_and_rejects_negative_values()
    {
        Assert.Equal(0, KeyLimit.Create(0).Value);
        Assert.Throws<InvalidValueException>(() => KeyLimit.Create(-1));
    }
}
