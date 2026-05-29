using LawtonJobBoardsServices.Configuration;
using LawtonJobBoardsServices.Models.Dto;
using Microsoft.Extensions.Options;
using LawtonJobBoardsServices.Services.Interfaces;

namespace LawtonJobBoardsServices.Services;

public class DueStatusCalculator(IOptions<OrdantSettings> settings) : IDueStatusCalculator
{
    private readonly int _thresholdHours = settings.Value.DueSoonThresholdHours;

    public DueStatus Calculate(DateTimeOffset? dueDate, bool isComplete)
    {
        if (dueDate is null)
            return DueStatus.NoDueDate;

        var now = DateTimeOffset.UtcNow;

        if (dueDate.Value < now)
            return isComplete ? DueStatus.OnTime : DueStatus.Overdue;

        if (dueDate.Value <= now.AddHours(_thresholdHours))
            return DueStatus.DueSoon;

        return DueStatus.OnTime;
    }
}
