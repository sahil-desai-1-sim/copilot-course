using BookmarkOrganizer.Infrastructure;
using BookmarkOrganizer.Models;
using BookmarkOrganizer.Services;
using FluentAssertions;
using Moq;

namespace BookmarkOrganizer.Tests.Unit.Services;

public sealed class BookmarkServiceTests
{
    [Fact]
    [Trait("Category", "US1")]
    public async Task AddAsync_GivenValidBookmark_CreatesBookmark()
    {
        var repository = new Mock<IBookmarkRepository>();
        repository
            .Setup(r => r.FindByUrlAsync("https://example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bookmark?)null);
        repository
            .Setup(r => r.AddAsync(It.IsAny<AddBookmarkRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bookmark(
                1,
                "https://example.com",
                "Example",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                [new Tag(1, "dev")]));

        var service = new BookmarkService(repository.Object);

        var result = await service.AddAsync("https://example.com", "Example", ["dev"], CancellationToken.None);

        result.IsDuplicate.Should().BeFalse();
        result.Bookmark.Url.Should().Be("https://example.com");
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task AddAsync_GivenDuplicateUrl_ReturnsDuplicateResult()
    {
        var existing = new Bookmark(
            7,
            "https://example.com",
            "Existing",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            []);

        var repository = new Mock<IBookmarkRepository>();
        repository
            .Setup(r => r.FindByUrlAsync("https://example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var service = new BookmarkService(repository.Object);

        var result = await service.AddAsync("https://example.com", "Example", [], CancellationToken.None);

        result.IsDuplicate.Should().BeTrue();
        result.Bookmark.Id.Should().Be(7);
        repository.Verify(r => r.AddAsync(It.IsAny<AddBookmarkRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task AddAsync_GivenInvalidUrl_ThrowsArgumentException()
    {
        var repository = new Mock<IBookmarkRepository>();
        var service = new BookmarkService(repository.Object);

        var action = async () => await service.AddAsync("not-a-url", "Bad", [], CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task GetAllAsync_GivenNoTags_DelegatesToRepository()
    {
        var expected = new List<Bookmark>
        {
            new(1, "https://example.com", "Example", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [])
        };

        var repository = new Mock<IBookmarkRepository>();
        repository
            .Setup(r => r.GetAllAsync(Constants.DefaultLimit, Constants.SortCreatedAt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new BookmarkService(repository.Object);

        var actual = await service.GetAllAsync([], Constants.DefaultLimit, Constants.SortCreatedAt, CancellationToken.None);

        actual.Should().BeEquivalentTo(expected);
    }
}
