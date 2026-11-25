using Microsoft.Extensions.Logging;

namespace RailwayErp.MvvmDemo.Models;

/// <summary>Один запис лог-файлу, розібраний для екрана перегляду логів.</summary>
public class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }

    public LogLevel Level { get; init; }

    /// <summary>Скорочення рівня так, як його пише Serilog: VRB, DBG, INF, WRN, ERR, FTL.</summary>
    public string LevelShort { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    /// <summary>Стек винятку (рядки, що йдуть після основного повідомлення).</summary>
    public string? Exception { get; set; }

    public bool HasException => !string.IsNullOrWhiteSpace(Exception);

    public string TimeText => Timestamp.ToString("dd.MM HH:mm:ss");

    /// <summary>Колір бейджа рівня на екрані логів.</summary>
    public Color LevelColor => Level switch
    {
        LogLevel.Trace or LogLevel.Debug => Colors.Gray,
        LogLevel.Information => Color.FromArgb("#2E7D32"),
        LogLevel.Warning => Color.FromArgb("#EF6C00"),
        LogLevel.Error => Color.FromArgb("#C62828"),
        LogLevel.Critical => Color.FromArgb("#6A1B9A"),
        _ => Colors.Gray
    };
}
