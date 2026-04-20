namespace LawtonJobBoardsServices.Models.Dto;

public class PagedResultDto<T>
{
    public int Count { get; set; }
    public int LimitPerPage { get; set; }
    public int Offset { get; set; }
    public List<T> ResultSet { get; set; } = [];
}
