using BookmarkOrganizer.Models;
using FluentAssertions;

namespace BookmarkOrganizer.Tests.Unit.Models;

public sealed class BookmarkTests
{
    [Fact]
    [Trait("Category", "Foundational")]
    public void IsValidUrl_GivenAbsoluteUrl_ReturnsTrue()
    {
        BookmarkValidation.IsValidUrl("https://example.com/path").Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Foundational")]
    public void IsValidUrl_GivenRelativeUrl_ReturnsFalse()
    {
        BookmarkValidation.IsValidUrl("/relative").Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Foundational")]
    public void IsValidTag_GivenValidTag_ReturnsTrue()
    {
        BookmarkValidation.IsValidTag("dev-tools").Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Foundational")]
    public void IsValidTag_GivenInvalidTag_ReturnsFalse()
    {
        BookmarkValidation.IsValidTag("Dev Tools").Should().BeFalse();
    }
}
