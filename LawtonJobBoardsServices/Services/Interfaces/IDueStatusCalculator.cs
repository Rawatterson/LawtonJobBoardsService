using LawtonJobBoardsServices.Models.Dto;

namespace LawtonJobBoardsServices.Services.Interfaces
{
    public interface IDueStatusCalculator
    {
        DueStatus Calculate(DateTimeOffset? dueDate, bool isComplete);
    }
}
