using System.Text;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Services.CrashReporting;
using RailwayErp.MvvmDemo.Services.Logging;

namespace RailwayErp.MvvmDemo.Services;

public interface IBugReportService
{
    /// <summary>Формує текст звіту: опис користувача + інформація про пристрій + останні рядки логу.</summary>
    Task<string> BuildReportAsync(string userComment, CancellationToken ct = default);

    /// <summary>Відкриває системне вікно "Поділитися" зі звітом (пошта, месенджер, нотатки тощо).</summary>
    Task ShareReportAsync(string userComment, CancellationToken ct = default);
}

/// <summary>
/// Ручне формування звіту про помилку — кнопка "Повідомити про помилку" (Лаб. 5, п. 1.6).
/// До звіту додається інформація про пристрій і останні записи логу.
/// Персональні дані користувача сюди НЕ потрапляють — лише технічна інформація.
/// </summary>
public class BugReportService : IBugReportService
{
    private const int LogLinesInReport = 80;

    private readonly ILogReaderService _logReader;
    private readonly ICrashReportingService _crashReporting;
    private readonly ILogger<BugReportService> _logger;

    public BugReportService(ILogReaderService logReader, ICrashReportingService crashReporting, ILogger<BugReportService> logger)
    {
        _logReader = logReader;
        _crashReporting = crashReporting;
        _logger = logger;
    }

    public async Task<string> BuildReportAsync(string userComment, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Звіт про помилку RailwayErp ===");
        sb.AppendLine($"Час: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine();
        sb.AppendLine("Опис від користувача:");
        sb.AppendLine(string.IsNullOrWhiteSpace(userComment) ? "(не вказано)" : userComment.Trim());
        sb.AppendLine();
        sb.AppendLine("--- Застосунок ---");
        sb.AppendLine($"Версія: {AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})");
        sb.AppendLine($"Пакет: {AppInfo.Current.PackageName}");
        sb.AppendLine($"Сервіс крашів: {_crashReporting.ProviderName}");
        sb.AppendLine($"Падіння при попередньому запуску: {(_crashReporting.DidCrashOnPreviousExecution() ? "так" : "ні / невідомо")}");
        sb.AppendLine();
        sb.AppendLine("--- Пристрій ---");
        sb.AppendLine($"Платформа: {DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}");
        sb.AppendLine($"Модель: {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}");
        sb.AppendLine($"Тип: {DeviceInfo.Current.Idiom}, {(DeviceInfo.Current.DeviceType == DeviceType.Virtual ? "емулятор" : "фізичний")}");
        sb.AppendLine($"Мова: {System.Globalization.CultureInfo.CurrentUICulture.Name}");
        sb.AppendLine();
        sb.AppendLine($"--- Останні {LogLinesInReport} рядків логу ---");
        sb.AppendLine(await _logReader.ReadTailTextAsync(LogLinesInReport, ct));
        return sb.ToString();
    }

    public async Task ShareReportAsync(string userComment, CancellationToken ct = default)
    {
        _logger.LogInformation("Користувач формує звіт про помилку");
        _crashReporting.Log($"Користувач надіслав звіт: {userComment}");

        var report = await BuildReportAsync(userComment, ct);

        // Звіт зберігаємо у файл у кеші та віддаємо системному "Поділитися".
        var path = Path.Combine(FileSystem.CacheDirectory, $"bug-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        await File.WriteAllTextAsync(path, report, Encoding.UTF8, ct);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Звіт про помилку RailwayErp",
            File = new ShareFile(path, "text/plain")
        });

        _logger.LogInformation("Звіт про помилку сформовано: {File}", Path.GetFileName(path));
    }
}
