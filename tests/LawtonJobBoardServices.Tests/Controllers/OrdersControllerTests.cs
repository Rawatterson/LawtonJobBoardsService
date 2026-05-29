using FluentAssertions;
using LawtonJobBoardsServices.Controllers;
using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using LawtonJobBoardsServices.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace LawtonJobBoardServices.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly IOrdantClient _client = Substitute.For<IOrdantClient>();
    private readonly IDueStatusCalculator _calculator = Substitute.For<IDueStatusCalculator>();

    private OrdersController Sut() => new(_client, _calculator);

    // ── GetOrders ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrders_WhenClientReturnsNull_ReturnsOkWithEmptyResultSet()
    {
        // Arrange
        _client
            .GetOrdersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((OrdantPagedResponse<OrdantOrderSummary>?)null);

        // Act
        var result = await Sut().GetOrders();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PagedResultDto<OrderSummaryDto>>().Subject;
        dto.ResultSet.Should().BeEmpty();
        dto.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetOrders_WithPagedResponse_PassesThroughPaginationMetadata()
    {
        // Arrange
        _client
            .GetOrdersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new OrdantPagedResponse<OrdantOrderSummary>
            {
                Count = 42,
                LimitPerPage = 10,
                Offset = 20,
                ResultSet = [],
            });

        // Act
        var result = await Sut().GetOrders();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PagedResultDto<OrderSummaryDto>>().Subject;
        dto.Count.Should().Be(42);
        dto.LimitPerPage.Should().Be(10);
        dto.Offset.Should().Be(20);
    }

    [Theory]
    [InlineData("ACME Corp", "Jane Doe", "ACME Corp")] // Company.Name takes precedence
    [InlineData(null, "Jane Doe", "Jane Doe")]          // fallback to Customer.Name (person customer)
    [InlineData(null, null, null)]                      // both sources null → null
    public async Task GetOrders_CompanyName_UsesCorrectFallback(
        string? companyName, string? customerName, string? expected)
    {
        // Arrange
        _client
            .GetOrdersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new OrdantPagedResponse<OrdantOrderSummary>
            {
                Count = 1,
                ResultSet =
                [
                    new OrdantOrderSummary
                    {
                        Customer = new OrdantCustomer
                        {
                            Name = customerName,
                            FullName = customerName,
                            Company = companyName is null ? null : new OrdantCompanyRef { Name = companyName },
                        },
                    },
                ],
            });

        // Act
        var result = await Sut().GetOrders();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PagedResultDto<OrderSummaryDto>>().Subject;
        dto.ResultSet[0].CompanyName.Should().Be(expected);
    }

    [Fact]
    public async Task GetOrders_WithValidTotalString_MapsDecimalTotal()
    {
        // Arrange
        _client
            .GetOrdersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new OrdantPagedResponse<OrdantOrderSummary>
            {
                Count = 1,
                ResultSet = [new OrdantOrderSummary { Summary = new OrdantFinancialSummary { Total = "1234.56" } }],
            });

        // Act
        var result = await Sut().GetOrders();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PagedResultDto<OrderSummaryDto>>().Subject;
        dto.ResultSet[0].Total.Should().Be(1234.56m);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetOrders_WithUnparseableOrNullTotalString_ReturnsTotalAsZero(string? totalString)
    {
        // Arrange
        _client
            .GetOrdersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new OrdantPagedResponse<OrdantOrderSummary>
            {
                Count = 1,
                ResultSet = [new OrdantOrderSummary { Summary = new OrdantFinancialSummary { Total = totalString! } }],
            });

        // Act
        var result = await Sut().GetOrders();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PagedResultDto<OrderSummaryDto>>().Subject;
        dto.ResultSet[0].Total.Should().Be(0m);
    }

    [Fact]
    public async Task GetOrders_WithNullSummary_ReturnsTotalAsZero()
    {
        // Arrange
        _client
            .GetOrdersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new OrdantPagedResponse<OrdantOrderSummary>
            {
                Count = 1,
                ResultSet = [new OrdantOrderSummary { Summary = null }],
            });

        // Act
        var result = await Sut().GetOrders();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PagedResultDto<OrderSummaryDto>>().Subject;
        dto.ResultSet[0].Total.Should().Be(0m);
    }

    // ── GetOrder ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrder_WhenClientReturnsNull_ReturnsNotFound()
    {
        // Arrange
        _client
            .GetOrderAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((OrdantOrderDetail?)null);

        // Act
        var result = await Sut().GetOrder(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetOrder_WithValidOrder_MapsDetailOnlyFields()
    {
        // Arrange
        var order = new OrdantOrderDetail
        {
            Id = 1,
            InternalId = "ORD-001",
            NotesCustomer = "Rush job",
            NotesInternal = "Check specs",
            PoNumber = "PO-9999",
            CustomerOrderId = "CUST-42",
            TransactionId = "TXN-7",
            AssignedTo = new OrdantPersonRef { FullName = "Alice" },
            AccountManager = new OrdantPersonRef { FullName = "Bob" },
            SalesPerson = new OrdantPersonRef { FullName = "Carol" },
        };
        _client
            .GetOrderAsync(1, Arg.Any<CancellationToken>())
            .Returns(order);

        // Act
        var result = await Sut().GetOrder(1);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OrderDetailDto>().Subject;
        dto.NotesCustomer.Should().Be("Rush job");
        dto.NotesInternal.Should().Be("Check specs");
        dto.PoNumber.Should().Be("PO-9999");
        dto.CustomerOrderId.Should().Be("CUST-42");
        dto.TransactionId.Should().Be("TXN-7");
        dto.AssignedToName.Should().Be("Alice");
        dto.AccountManagerName.Should().Be("Bob");
        dto.SalesPersonName.Should().Be("Carol");
    }

    [Fact]
    public async Task GetOrder_WithOrderItems_MapsItemsCollection()
    {
        // Arrange
        var due = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var order = new OrdantOrderDetail
        {
            Id = 5,
            Items =
            [
                new OrdantOrderItem
                {
                    Id = 10,
                    Description = "Banner 24x36",
                    Sku = "BAN-24",
                    SortOrder = 1,
                    SortId = "A1",
                    DateDue = due,
                    DateShipBy = due.AddHours(2),
                    DateProofDue = due.AddDays(-1),
                },
            ],
        };
        _client
            .GetOrderAsync(5, Arg.Any<CancellationToken>())
            .Returns(order);

        // Act
        var result = await Sut().GetOrder(5);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OrderDetailDto>().Subject;
        dto.Items.Should().HaveCount(1);
        var item = dto.Items[0];
        item.Id.Should().Be(10);
        item.Description.Should().Be("Banner 24x36");
        item.Sku.Should().Be("BAN-24");
        item.SortId.Should().Be("A1");
        item.DateDue.Should().Be(due);
    }
}
