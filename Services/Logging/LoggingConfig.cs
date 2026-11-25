using Serilog;
using Serilog.Events;

namespace RailwayErp.MvvmDemo.Services.Logging;

/// <summary>
/// Налаштування Serilog (Лаб. 5, п. 2.1.1): два sink'и —
///  • Console (+ Debug-вікно IDE) для розробки;
///  • File із щоденною ротацією та обмеженням кількості файлів.
/// </summary>
public static class LoggingConfig
{
    /// <summary>Шаблон рядка лог-файлу. Його ж розбирає <see cref="LogReaderService"/>.</summary>
    public const string FileOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>Скільки щоденних файлів зберігати (старіші видаляються автоматично).</summary>
    public const int RetainedFileCount = 7;

    /// <summary>Папка з логами всередині пісочниці застосунку.</summary>
    public static string LogDirectory => Path.Combine(FileSystem.AppDataDirectory, "logs");

    /// <summary>Шаблон імені: Serilog додасть дату → app-20261125.log.</summary>
    public static string LogFilePathTemplate => Path.Combine(LogDirectory, "app-.log");

    public static ILogger CreateLogger()
    {
        Directory.CreateDirectory(LogDirectory);

        var config = new LoggerConfiguration()
#if DEBUG
            // Debug-збірка: бачимо все, включно з технічними деталями.
            .MinimumLevel.Debug()
#else
            // Release-збірка: лише важливе, щоб не засмічувати лог і не гальмувати застосунок.
            .MinimumLevel.Warning()
#endif
            // Внутрішній "шум" фреймворку MAUI піднімаємо до Warning.
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("AppVersion", AppInfo.Current.VersionString)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: LogFilePathTemplate,
                rollingInterval: RollingInterval.Day,      // новий файл щодня
                retainedFileCountLimit: RetainedFileCount, // зберігаємо лише останні 7
                fileSizeLimitBytes: 5 * 1024 * 1024,       // і не більше 5 МБ на файл
                rollOnFileSizeLimit: true,
                shared: true,                              // щоб екран логів міг читати файл під час запису
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                outputTemplate: FileOutputTemplate);

        return config.CreateLogger();
    }
}
