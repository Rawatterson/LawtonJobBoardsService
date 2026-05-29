using FluentAssertions;
using LawtonJobBoardsServices.Configuration;
using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services;
using LawtonJobBoardsServices.Services.Interfaces;
using LawtonJobBoardsServices.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LawtonJobBoardServices.Tests.Services;

public class JobChangeProcessorTests
{
    private readonly IJobBroadcaster _broadcaster = Substitute.For<IJobBroadcaster>();
    private readonly IDueStatusCalculator _calculator = Substitute.For<IDueStatusCalculator>();

    private JobChangeDiffLogger MakeLogger()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(Path.GetTempPath());
        return new JobChangeDiffLogger(env);
    }

    private JobChangeProcessor Sut() =>
        new(_broadcaster, MakeLogger(), _calculator, NullLogger<JobChangeProcessor>.Instance);

    private static OrdantSchedulerJob Job(int id, string name = "Job") =>
        new() { Id = id, Name = name };

    private static IReadOnlyDictionary<int, OrdantJobsSnapShot> EmptyCache() =>
        new Dictionary<int, OrdantJobsSnapShot>();

    private static IReadOnlyDictionary<int, OrdantJobsSnapShot> CacheWith(params OrdantSchedulerJob[] jobs) =>
        jobs.ToDictionary(j => j.Id, OrdantSnapshotMapper.Map);

    // ── First run (isInitialized = false) ────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_FirstRunWithJobs_PopulatesCacheSilentlyWithNobroadcasts()
    {
        // Arrange
        var jobs = new[] { Job(1), Job(2) };

        // Act
        var result = await Sut().ProcessAsync(jobs, EmptyCache(), isInitialized: false, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.UpdatedCache.Should().HaveCount(2).And.ContainKeys(1, 2);
        await _broadcaster.DidNotReceive().BroadcastJobUpdateAsync(Arg.Any<ResourcePlannerJobDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_FirstRunWithEmptyJobList_SetsIsInitializedTrue()
    {
        // Arrange — Bug 1 regression: empty first poll must still mark the service as initialized.

        // Act
        var result = await Sut().ProcessAsync([], EmptyCache(), isInitialized: false, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.IsInitialized.Should().BeTrue();
        result.UpdatedCache.Should().BeEmpty();
    }

    // ── Second run, no changes ────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_SecondRunNoChanges_DoesNotBroadcastAndCacheIsUnchanged()
    {
        // Arrange
        var job = Job(1, "Unchanged");
        var cache = CacheWith(job); // snapshot matches current job state

        // Act
        var result = await Sut().ProcessAsync([job], cache, isInitialized: true, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.UpdatedCache.Should().BeEquivalentTo(cache);
        await _broadcaster.DidNotReceive().BroadcastJobUpdateAsync(Arg.Any<ResourcePlannerJobDto>(), Arg.Any<CancellationToken>());
    }

    // ── Second run, job field changed ─────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_JobChanged_BroadcastsUpdateAndUpdatesCache()
    {
        // Arrange — cached with Name "Old", fresh has Name "New"
        var oldJob = Job(1, "Old");
        var newJob = Job(1, "New");
        var cache = CacheWith(oldJob);

        // Act
        var result = await Sut().ProcessAsync([newJob], cache, isInitialized: true, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.UpdatedCache[1].Name.Should().Be("New");
        await _broadcaster.Received(1).BroadcastJobUpdateAsync(Arg.Any<ResourcePlannerJobDto>(), Arg.Any<CancellationToken>());
    }

    // ── Second run, new job appeared ──────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_NewJobAppearedAfterInit_BroadcastsAndAddsToCache()
    {
        // Arrange
        var existingJob = Job(1);
        var newJob = Job(2, "Brand New");
        var cache = CacheWith(existingJob);

        // Act
        var result = await Sut().ProcessAsync([existingJob, newJob], cache, isInitialized: true, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.UpdatedCache.Should().ContainKey(2);
        await _broadcaster.Received(1).BroadcastJobUpdateAsync(
            Arg.Is<ResourcePlannerJobDto>(dto => dto.Id == 2),
            Arg.Any<CancellationToken>());
    }

    // ── Second run, job removed ───────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_JobVanished_BroadcastsRemovalAndDropsFromCache()
    {
        // Arrange — job 99 was in cache but is no longer in the fresh list
        var cache = CacheWith(Job(1), Job(99));

        // Act
        var result = await Sut().ProcessAsync([Job(1)], cache, isInitialized: true, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.UpdatedCache.Should().NotContainKey(99);
        await _broadcaster.Received(1).BroadcastJobRemovedAsync(99, Arg.Any<CancellationToken>());
    }

    // ── Broadcast failure — cache atomicity (Bug 2 regression) ───────────────

    [Fact]
    public async Task ProcessAsync_BroadcastUpdateFails_ReturnsFalseAndCacheUnchanged()
    {
        // Arrange — Bug 2 regression: cache must not be partially updated when a broadcast fails.
        var oldJob = Job(1, "Old");
        var newJob = Job(1, "New");
        var cache = CacheWith(oldJob);

        _broadcaster
            .BroadcastJobUpdateAsync(Arg.Any<ResourcePlannerJobDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Hub offline"));

        // Act
        var result = await Sut().ProcessAsync([newJob], cache, isInitialized: true, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        // Original cache returned intact — old snapshot preserved so next tick re-diffs correctly.
        result.UpdatedCache[1].Name.Should().Be("Old");
    }

    [Fact]
    public async Task ProcessAsync_BroadcastRemovalFails_ReturnsFalseAndCacheUnchanged()
    {
        // Arrange
        var cache = CacheWith(Job(1), Job(99));

        _broadcaster
            .BroadcastJobRemovedAsync(99, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Hub offline"));

        // Act
        var result = await Sut().ProcessAsync([Job(1)], cache, isInitialized: true, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.UpdatedCache.Should().ContainKey(99); // removal not committed
    }

    // ── Removed jobs not detected on first run ────────────────────────────────

    [Fact]
    public async Task ProcessAsync_FirstRunWithCacheAlreadyPopulated_DoesNotBroadcastRemovals()
    {
        // Arrange — unusual but defensive: if cache had entries before first run, no removals broadcast.
        var cache = CacheWith(Job(99));

        // Act
        var result = await Sut().ProcessAsync([Job(1)], cache, isInitialized: false, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _broadcaster.DidNotReceive().BroadcastJobRemovedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
