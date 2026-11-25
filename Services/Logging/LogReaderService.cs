using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services.Logging;

public interface ILogReaderService
{
    /// <summary>Останні <paramref name="count"/> записів (новіші — першими), не нижче рівня <paramref name="minLevel"/>.</summary>
    Task<IReadOnlyList<LogEntry>> ReadLatestAsync(int count, LogLevel minLevel = LogLevel.Trace, CancellationToken ct = default);

    /// <summary>Сирий текст останніх рядків поточного лог-файлу (для звіту про помилку).</summary>
    Task<string> ReadTailTextAsync(int maxLines, CancellationToken ct = default);

    IReadOnlyList<string> GetLogFiles();
}

/// <summary>
/// Читає лог-файли Serilog з FileSystem.AppDataDirectory/logs (Лаб. 5, п. 2.2.6).
/// Рядок формату <see cref="LoggingConfig.FileOutputTemplate"/>:
/// 2026-11-25 14:03:11.512 +02:00 [INF] RailwayErp...TrainsViewModel: Повідомлення
/// Рядки без такого префікса (стек винятку) приєднуються до попереднього запису.
/// </summary>
public partial class LogReaderService : ILogReaderService
{
    [GeneratedRegex(@"^(?<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}) \[(?<lvl>[A-Z]{3})\] (?<src>[^:]*): (?<msg>.*)$")]
    private static partial Regex LinePattern();

    public IReadOnlyList<string> GetLogFiles()
    {
        if (!Directory.Exists(LoggingConfig.LogDirectory))
            return [];

        return new DirectoryInfo(LoggingConfig.LogDirectory)
            .GetFiles("app-*.log")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Select(f => f.FullName)
            .ToList();
    }

    public async Task<IReadOnlyList<LogEntry>> ReadLatestAsync(int count, LogLevel minLevel = LogLevel.Trace, CancellationToken ct = default)
    {
        var result = new List<LogEntry>(count);

        // Йдемо від найновішого файлу до старіших, доки не наберемо N записів.
        foreach (var file in GetLogFiles())
        {
            var entries = Parse(await ReadAllLinesSharedAsync(file, ct));
            for (var i = entries.Count - 1; i >= 0 && result.Count < count; i--)
            {
                if (entries[i].Level >= minLevel)
                    result.Add(entries[i]);
            }

            if (result.Count >= count)
                break;
        }

        return result;
    }

    public async Task<string> ReadTailTextAsync(int maxLines, CancellationToken ct = default)
    {
        var file = GetLogFiles().FirstOrDefault();
        if (file is null)
            return "(лог-файл ще не створено)";

        var lines = await ReadAllLinesSharedAsync(file, ct);
        return string.Join(Environment.NewLine, lines.Skip(Math.Max(0, lines.Count - maxLines)));
    }

    /// <summary>
    /// Serilog тримає файл відкритим, тому читаємо з FileShare.ReadWrite,
    /// інакше на Android/Windows отримаємо IOException "file is being used".
    /// </summary>
    private static async Task<List<string>> ReadAllLinesSharedAsync(string path, CancellationToken ct)
    {
        var lines = new List<string>();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (await reader.ReadLineAsync(ct) is { } line)
            lines.Add(line);
        return lines;
    }

    private static List<LogEntry> Parse(List<string> lines)
    {
        var entries = new List<LogEntry>();
        LogEntry? current = null;
        StringBuilder? exception = null;

        void Flush()
        {
            if (current is null) return;
            if (exception is { Length: > 0 })
                current.Exception = exception.ToString().TrimEnd();
            entries.Add(current);
        }

        foreach (var line in lines)
        {
            var m = LinePattern().Match(line);
            if (m.Success)
            {
                Flush();
                exception = new StringBuilder();
                current = new LogEntry
                {
                    Timestamp = DateTimeOffset.ParseExact(m.Groups["ts"].Value, "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
                    LevelShort = m.Groups["lvl"].Value,
                    Level = MapLevel(m.Groups["lvl"].Value),
                    Source = ShortSource(m.Groups["src"].Value),
                    Message = m.Groups["msg"].Value
                };
            }
            else if (current is not null && !string.IsNullOrWhiteSpace(line))
            {
                exception!.AppendLine(line);
            }
        }

        Flush();
        return entries;
    }

    private static LogLevel MapLevel(string serilogLevel) => serilogLevel switch
    {
        "VRB" => LogLevel.Trace,
        "DBG" => LogLevel.Debug,
        "INF" => LogLevel.Information,
        "WRN" => LogLevel.Warning,
        "ERR" => LogLevel.Error,
        "FTL" => LogLevel.Critical,
        _ => LogLevel.None
    };

    /// <summary>RailwayErp.MvvmDemo.ViewModels.TrainsViewModel → TrainsViewModel.</summary>
    private static string ShortSource(string source)
    {
        var dot = source.LastIndexOf('.');
        return dot >= 0 ? source[(dot + 1)..] : source;
    }
}
