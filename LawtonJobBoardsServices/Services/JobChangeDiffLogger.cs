using LawtonJobBoardsServices.Models.Dto;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LawtonJobBoardsServices.Services;

public class JobChangeDiffLogger(IWebHostEnvironment env)
{
    private const int MaxEntries = 500;
    private readonly ConcurrentQueue<JobChangeDiffEntry> _queue = new();
    private readonly string _logDirectory = Path.Combine(env.ContentRootPath, "logs");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    public void Log(JobChangeDiffEntry entry)
    {
        _queue.Enqueue(entry);
        while (_queue.Count > MaxEntries)
            _queue.TryDequeue(out _);

        WriteToFile(entry);
    }

    public IReadOnlyList<JobChangeDiffEntry> GetRecent(int limit)
    {
        return _queue.TakeLast(Math.Clamp(limit, 1, MaxEntries)).ToList();
    }

    private void WriteToFile(JobChangeDiffEntry entry)
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
            var fileName = $"job-changes-{entry.Timestamp:yyyy-MM-dd}.json";
            var filePath = Path.Combine(_logDirectory, fileName);
            var line = JsonSerializer.Serialize(entry, JsonOptions);
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
        catch
        {
            // File logging is best-effort — don't crash the background service
        }
    }
}
