using FluentAssertions;
using LawtonJobBoardsServices.Configuration;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services;
using LawtonJobBoardsServices.Services.Interfaces;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LawtonJobBoardServices.Tests.Services;

public class OrdantJobPollerTests
{
    private readonly IOrdantClient _client = Substitute.For<IOrdantClient>();

    private OrdantJobPoller Sut(params int[] stationIds) =>
        new(_client, Options.Create(new BoardStationsSettings { StationIds = [.. stationIds] }));

    private static OrdantPagedResponse<OrdantSchedulerJob> Page(int count, params OrdantSchedulerJob[] jobs) =>
        new() { Count = count, ResultSet = [.. jobs] };

    private static OrdantSchedulerJob Job(int id) => new() { Id = id };

    // ── Single station, single page ───────────────────────────────────────────

    [Fact]
    public async Task GetAllJobsAsync_SingleStation_SinglePage_ReturnsAllJobs()
    {
        // Arrange
        _client
            .GetSchedulerJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), 1, Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Page(2, Job(10), Job(11)));
        var sut = Sut(1);

        // Act
        var result = await sut.GetAllJobsAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(j => j.Id).Should().BeEquivalentTo([10, 11]);
    }

    // ── Single station, multi-page ────────────────────────────────────────────

    [Fact]
    public async Task GetAllJobsAsync_SingleStation_MultiPage_PaginatesUntilTotalReached()
    {
        // Arrange — Ordant reports Count=3 but returns 2 per page
        _client
            .GetSchedulerJobsAsync(1, Arg.Any<int>(), Arg.Any<bool?>(), 1, Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Page(3, Job(1), Job(2)));
        _client
            .GetSchedulerJobsAsync(2, Arg.Any<int>(), Arg.Any<bool?>(), 1, Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Page(3, Job(3)));
        var sut = Sut(1);

        // Act
        var result = await sut.GetAllJobsAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Select(j => j.Id).Should().BeEquivalentTo([1, 2, 3]);
    }

    // ── Null response breaks pagination gracefully ────────────────────────────

    [Fact]
    public async Task GetAllJobsAsync_WhenClientReturnsNull_ReturnsEmptyWithoutThrowing()
    {
        // Arrange
        _client
            .GetSchedulerJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), 1, Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((OrdantPagedResponse<OrdantSchedulerJob>?)null);
        var sut = Sut(1);

        // Act
        var result = await sut.GetAllJobsAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    // ── Multiple stations ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllJobsAsync_MultipleStations_FetchesEachStationAndCombinesResults()
    {
        // Arrange
        _client
            .GetSchedulerJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), 1, Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Page(1, Job(100)));
        _client
            .GetSchedulerJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), 2, Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Page(1, Job(200)));
        var sut = Sut(1, 2);

        // Act
        var result = await sut.GetAllJobsAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(j => j.Id).Should().BeEquivalentTo([100, 200]);
    }

    // ── One station returns null, another has jobs ────────────────────────────

    [Fact]
    public async Task GetAllJobsAsync_OneStationNull_OtherStationReturnsJobs()
    {
        // Arrange
        _client
            .GetSchedulerJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), 1, Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((OrdantPagedResponse<OrdantSchedulerJob>?)null);
        _client
            .GetSchedulerJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), 2, Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Page(1, Job(42)));
        var sut = Sut(1, 2);

        // Act
        var result = await sut.GetAllJobsAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(42);
    }

    // ── No stations configured ────────────────────────────────────────────────

    [Fact]
    public async Task GetAllJobsAsync_NoStationsConfigured_ReturnsEmpty()
    {
        // Arrange
        var sut = Sut(); // empty StationIds

        // Act
        var result = await sut.GetAllJobsAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
