namespace LawtonJobBoardsServices.Configuration;

public class OrdantSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUser { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Hours before the due date at which a job is considered "DueSoon".
    /// Defaults to 4 hours.
    /// </summary>
    public int DueSoonThresholdHours { get; set; } = 4;
}
