using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Services;
using RailwayErp.MvvmDemo.Services.CrashReporting;
using RailwayErp.MvvmDemo.Services.Logging;

namespace RailwayErp.MvvmDemo.ViewModels;

/// <summary>
/// Екран "Діагностика": моделювання помилок (п. 2.1.4), тестовий краш для Crashlytics (п. 2.2.5)
/// і кнопка "Повідомити про помилку" (п. 1.6).
/// </summary>
public partial class DiagnosticsViewModel : ObservableObject
{
    private readonly ICrashReportingService _crashReporting;
    private readonly IBugReportService _bugReport;
    private readonly ILogger<DiagnosticsViewModel> _logger;

    public DiagnosticsViewModel(ICrashReportingService crashReporting, IBugReportService bugReport, ILogger<DiagnosticsViewModel> logger)
    {
        _crashReporting = crashReporting;
        _bugReport = bugReport;
        _logger = logger;
    }

    public string CrashProvider => _crashReporting.ProviderName;

    public bool CrashReportingEnabled => _crashReporting.IsEnabled;

    public string LogDirectory => LoggingConfig.LogDirectory;

    [ObservableProperty]
    public partial string BugComment { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? LastResult { get; set; }

    /// <summary>Записи всіх рівнів — для фрагмента лог-файлу у звіті.</summary>
    [RelayCommand]
    private void WriteAllLevels()
    {
        _logger.LogTrace("Trace: найдетальніший рівень (у Debug-збірці відсікається MinimumLevel.Debug)");
        _logger.LogDebug("Debug: технічні деталі для розробника, напр. параметри запиту");
        _logger.LogInformation("Information: потяг {Number} вирушив за маршрутом {Route}", "743К", "Київ — Львів");
        _logger.LogWarning("Warning: потяг {Number} затримується на {Minutes} хв", "091К", 25);
        _logger.LogError(new TimeoutException("Сервер диспетчерської не відповідає"), "Error: не вдалося синхронізувати розклад");
        _logger.LogCritical("Critical: втрачено зв'язок з усіма потягами на ділянці {Section}", "Фастів — Козятин");
        LastResult = "У лог записано повідомлення всіх рівнів. Відкрийте вкладку «Логи».";
    }

    /// <summary>
    /// Необроблений виняток у фоновій задачі (п. 2.1.4).
    /// Task ніхто не очікує (fire-and-forget), тому виняток "губиться".
    /// Подія TaskScheduler.UnobservedTaskException спрацьовує лише під час фіналізації Task
    /// збирачем сміття — тут ми примусово запускаємо GC, щоб побачити результат одразу.
    /// </summary>
    [RelayCommand]
    private async Task SimulateUnobservedTaskExceptionAsync()
    {
        _logger.LogInformation("Моделювання необробленого винятку у фоновій задачі");

        StartFireAndForget();

        await Task.Delay(500); // даємо задачі впасти
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        LastResult = "Фонова задача впала без await. Подія UnobservedTaskException має з'явитися в лозі з рівнем ERR.";
    }

    // Окремий метод без async, щоб посилання на Task не залишилось у стеку й GC міг його зібрати.
    private static void StartFireAndForget()
    {
        _ = Task.Run(() =>
        {
            throw new InvalidOperationException("Збій фонової синхронізації розкладу (змодельований необроблений виняток у Task)");
        });
    }

    /// <summary>
    /// Необроблений виняток в окремому потоці → AppDomain.UnhandledException (FTL у лозі), застосунок завершується.
    /// </summary>
    [RelayCommand]
    private async Task SimulateBackgroundThreadCrashAsync()
    {
        var page = Shell.Current?.CurrentPage;
        if (page is not null &&
            !await page.DisplayAlertAsync("Увага", "Застосунок аварійно завершиться. Після перезапуску подивіться запис FTL на вкладці «Логи».", "Впасти", "Скасувати"))
            return;

        _logger.LogWarning("Моделювання необробленого винятку у фоновому потоці (застосунок завершиться)");

        var thread = new Thread(() =>
            throw new ApplicationException("Критичний збій потоку телеметрії потягів (змодельований)"))
        {
            IsBackground = true,
            Name = "TelemetryThread"
        };
        thread.Start();
    }

    /// <summary>Штучний краш для перевірки звіту в панелі Crashlytics (п. 2.2.5).</summary>
    [RelayCommand]
    private async Task ForceCrashlyticsCrashAsync()
    {
        var page = Shell.Current?.CurrentPage;
        var message = _crashReporting.IsEnabled
            ? "Застосунок впаде. Перезапустіть його — звіт з'явиться в Firebase Console → Crashlytics протягом кількох хвилин."
            : "Crashlytics ще не налаштовано (немає google-services.json). Краш запишеться лише в локальний лог.";

        if (page is not null && !await page.DisplayAlertAsync("Тестовий краш", message, "Впасти", "Скасувати"))
            return;

        _crashReporting.SetCustomKey("screen", "Diagnostics");
        _crashReporting.ForceTestCrash();
    }

    /// <summary>Нефатальна помилка: перехоплений виняток, надісланий у Crashlytics як non-fatal.</summary>
    [RelayCommand]
    private void RecordNonFatal()
    {
        try
        {
            throw new FormatException("Некоректний формат відповіді API квитків (змодельовано)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Нефатальна помилка обробки відповіді API");
            _crashReporting.RecordException(ex);
            LastResult = _crashReporting.IsEnabled
                ? "Нефатальну помилку надіслано в Crashlytics (розділ Non-fatals)."
                : "Нефатальну помилку записано в лог (Crashlytics не налаштовано).";
        }
    }

    [RelayCommand]
    private async Task SendBugReportAsync()
    {
        try
        {
            await _bugReport.ShareReportAsync(BugComment);
            BugComment = string.Empty;
            LastResult = "Звіт сформовано.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не вдалося сформувати звіт про помилку");
            LastResult = "Не вдалося сформувати звіт: " + ex.Message;
        }
    }
}
