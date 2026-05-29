using FluentAssertions;
using LawtonJobBoardsServices.Controllers;
using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace LawtonJobBoardServices.Tests.Controllers;

public class ResourcePlannerControllerTests
{
    private readonly IOrdantClient _client = Substitute.For<IOrdantClient>();
    private readonly IDueStatusCalculator _calculator = Substitute.For<IDueStatusCalculator>();

    private ResourcePlannerController Sut() => new(_client, _calculator);

    // ── GetJobs ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetJobs_WhenClientReturnsNull_ReturnsOkWithEmptyResultSet()
    {
        // Arrange
        _client
            .GetSchedulerJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((OrdantPagedResponse<OrdantSchedulerJob>?)null);

        // Act
        var result = await Sut().GetJobs();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PagedResultDto<ResourcePlannerJobDto>>().Subject;
        dto.ResultSet.Should().BeEmpty();
        dto.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetJobs_WithPagedResponse_PassesThroughPaginationMetadata()
    {
        // Arrange
        _client
            .GetSchedulerJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(new OrdantPagedResponse<OrdantSchedulerJob>
            {
                Count = 55,
                LimitPerPage = 25,
                Offset = 50,
                ResultSet = [],
            });

        // Act
        var result = await Sut().GetJobs();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PagedResultDto<ResourcePlannerJobDto>>().Subject;
        dto.Count.Should().Be(55);
        dto.LimitPerPage.Should().Be(25);
        dto.Offset.Should().Be(50);
    }

    // ── GetJob ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetJob_WhenClientReturnsNull_ReturnsNotFound()
    {
        // Arrange
        _client
            .GetSchedulerJobAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((OrdantSchedulerJob?)null);

        // Act
        var result = await Sut().GetJob(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetJob_WithValidJob_ReturnsMappedDto()
    {
        // Arrange
        var job = new OrdantSchedulerJob
        {
            Id = 7,
            Name = "Large Format Print",
            IsComplete = false,
            Station = new OrdantStation
            {
                Id = 3,
                Name = "Press",
                Group = new OrdantStationGroup { Id = 1, Name = "Print" },
            },
            StationSortOrder = 2,
            SortOrder = 5,
            HasCompletedDependencies = true,
            JobQueue = new OrdantJobQueue
            {
                OrderItem = new OrdantOrderItemRef
                {
                    Id = 20,
                    Description = "Vinyl banner",
                    Order = new OrdantOrderRef
                    {
                        Id = 100,
                        InternalId = "ORD-100",
                        ProjectName = "Trade Show 2026",
                    },
                },
            },
        };
        _client
            .GetSchedulerJobAsync(7, Arg.Any<CancellationToken>())
            .Returns(job);

        // Act
        var result = await Sut().GetJob(7);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<ResourcePlannerJobDto>().Subject;
        dto.Id.Should().Be(7);
        dto.Name.Should().Be("Large Format Print");
        dto.StationId.Should().Be(3);
        dto.StationName.Should().Be("Press");
        dto.StationGroupId.Should().Be(1);
        dto.StationGroupName.Should().Be("Print");
        dto.OrderId.Should().Be(100);
        dto.OrderInternalId.Should().Be("ORD-100");
        dto.OrderProjectName.Should().Be("Trade Show 2026");
        dto.OrderItemId.Should().Be(20);
        dto.OrderItemDescription.Should().Be("Vinyl banner");
        dto.HasCompletedDependencies.Should().BeTrue();
    }
}
