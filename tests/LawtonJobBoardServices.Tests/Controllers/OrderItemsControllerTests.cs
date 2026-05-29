using FluentAssertions;
using LawtonJobBoardsServices.Configuration;
using LawtonJobBoardsServices.Controllers;
using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services;
using LawtonJobBoardsServices.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LawtonJobBoardServices.Tests.Controllers;

public class OrderItemsControllerTests
{
    private readonly IOrdantClient _client = Substitute.For<IOrdantClient>();

    // Use the real calculator so tests exercise the end-to-end DueStatus logic.
    private static IDueStatusCalculator RealCalculator() =>
        new DueStatusCalculator(Options.Create(new OrdantSettings { DueSoonThresholdHours = 4 }));

    private OrderItemsController Sut(IDueStatusCalculator? calculator = null) =>
        new(_client, calculator ?? RealCalculator());

    // ── GetOrderItems ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderItems_WhenClientReturnsNull_ReturnsOkWithEmptyResultSet()
    {
        // Arrange
        _client
            .GetOrderItemsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns((OrdantPagedResponse<OrdantOrderItemSummary>?)null);

        // Act
        var result = await Sut().GetOrderItems();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PagedResultDto<OrderItemSummaryDto>>().Subject;
        dto.ResultSet.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderItems_ForItemWithPastDueDate_ReturnsDueStatusOverdue()
    {
        // Arrange — the list serializer does not return IsComplete, so the controller
        // hardcodes isComplete=false. A past-due item therefore always shows Overdue,
        // even if the underlying record is complete.
        var pastDue = DateTimeOffset.UtcNow.AddHours(-1);
        _client
            .GetOrderItemsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new OrdantPagedResponse<OrdantOrderItemSummary>
            {
                Count = 1,
                ResultSet = [new OrdantOrderItemSummary { DateDue = pastDue }],
            });

        // Act
        var result = await Sut().GetOrderItems();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PagedResultDto<OrderItemSummaryDto>>().Subject;
        dto.ResultSet[0].DueStatus.Should().Be(DueStatus.Overdue);
    }

    // ── GetOrderItem ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderItem_WhenClientReturnsNull_ReturnsNotFound()
    {
        // Arrange
        _client
            .GetOrderItemAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((OrdantOrderItemDetail?)null);

        // Act
        var result = await Sut().GetOrderItem(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetOrderItem_ForCompleteItemWithPastDueDate_ReturnsDueStatusOnTime()
    {
        // Arrange — contrast with the list endpoint: detail has IsComplete, so the
        // calculator receives the true completion state and returns OnTime.
        var pastDue = DateTimeOffset.UtcNow.AddHours(-1);
        _client
            .GetOrderItemAsync(1, Arg.Any<CancellationToken>())
            .Returns(new OrdantOrderItemDetail { DateDue = pastDue, IsComplete = true });

        // Act
        var result = await Sut().GetOrderItem(1);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OrderItemDetailDto>().Subject;
        dto.DueStatus.Should().Be(DueStatus.OnTime);
    }

    [Theory]
    [InlineData("bad-price", "10.00", 0.00, 10.00)]
    [InlineData("25.50", "bad-cost", 25.50, 0.00)]
    [InlineData(null, null, 0.00, 0.00)]
    public async Task GetOrderItem_WithUnparseablePricing_ReturnsPricingAsZero(
        string? sellPrice, string? cost, double expectedSell, double expectedCost)
    {
        // Arrange
        _client
            .GetOrderItemAsync(1, Arg.Any<CancellationToken>())
            .Returns(new OrdantOrderItemDetail
            {
                Summary = new OrdantItemPricing { SellPrice = sellPrice, Cost = cost },
            });

        // Act
        var result = await Sut().GetOrderItem(1);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OrderItemDetailDto>().Subject;
        dto.SellPrice.Should().Be((decimal)expectedSell);
        dto.Cost.Should().Be((decimal)expectedCost);
    }

    [Fact]
    public async Task GetOrderItem_WithIsDetachedTrue_MapsIsDetachedTrue()
    {
        // Arrange
        _client
            .GetOrderItemAsync(1, Arg.Any<CancellationToken>())
            .Returns(new OrdantOrderItemDetail { IsDetached = true });

        // Act
        var result = await Sut().GetOrderItem(1);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OrderItemDetailDto>().Subject;
        dto.IsDetached.Should().BeTrue();
    }
}
