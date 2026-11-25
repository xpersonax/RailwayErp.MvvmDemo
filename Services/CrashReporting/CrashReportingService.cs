using Microsoft.Extensions.Logging;
#if ANDROID && FIREBASE_CONFIGURED
using Plugin.Firebase.Crashlytics;
#endif

namespace RailwayErp.MvvmDemo.Services.CrashReporting;

/// <summary>
/// Реалізація через Firebase Crashlytics (Plugin.Firebase.Crashlytics).
/// Crashlytics працює на Android/iOS. На інших платформах, а також поки в проєкт
/// не додано google-services.json, сервіс працює у "локальному" режимі:
/// все пишеться лише в лог-файл, у хмару нічого не надсилається.
/// </summary>
public class CrashReportingService : ICrashReportingService
{
    private readonly ILogger<CrashReportingService> _logger;

    public CrashReportingService(ILogger<CrashReportingService> logger)
    {
        _logger = logger;
        _logger.LogInformation("Сервіс збору крашів: {Provider} (активний: {Enabled})", ProviderName, IsEnabled);
    }

#if ANDROID && FIREBASE_CONFIGURED
    public bool IsEnabled => true;

    public string ProviderName => "Firebase Crashlytics";

    public void Log(string message) => CrossFirebaseCrashlytics.Current.Log(message);

    public void SetCustomKey(string key, string value) => CrossFirebaseCrashlytics.Current.SetCustomKey(key, value);

    public void RecordException(Exception exception, bool fatal = false)
    {
        // Фатальні краші Crashlytics фіксує сам (нативним обробником) і надсилає при наступному запуску.
        // Тут додаємо позначку й записуємо .NET-стек як окрему подію — так у звіті видно C#-рядки коду.
        CrossFirebaseCrashlytics.Current.SetCustomKey("fatal", fatal);
        CrossFirebaseCrashlytics.Current.RecordException(exception);
    }

    public bool DidCrashOnPreviousExecution() => CrossFirebaseCrashlytics.Current.DidCrashOnPreviousExecution();
#else
    public bool IsEnabled => false;

    public string ProviderName =>
#if ANDROID
        "Firebase Crashlytics (не налаштовано: немає google-services.json)";
#else
        "лише локальний лог (Crashlytics недоступний на цій платформі)";
#endif

    public void Log(string message) => _logger.LogDebug("[crash-breadcrumb] {Message}", message);

    public void SetCustomKey(string key, string value) =>
        _logger.LogDebug("[crash-key] {Key}={Value}", key, value);

    public void RecordException(Exception exception, bool fatal = false) =>
        _logger.LogDebug("Виняток {Type} не надіслано в хмару: сервіс крашів не підключено", exception.GetType().Name);

    public bool DidCrashOnPreviousExecution() => false;
#endif

    public void ForceTestCrash()
    {
        _logger.LogWarning("Користувач ініціював тестовий краш для перевірки {Provider}", ProviderName);
        Log("ForceTestCrash натиснуто на екрані Діагностика");

        // Кидаємо виняток у головному потоці поза будь-яким try/catch —
        // застосунок падає, а Crashlytics надсилає звіт при наступному запуску.
        MainThread.BeginInvokeOnMainThread(() =>
            throw new InvalidOperationException("Тестовий краш RailwayErp (Лаб. 5): перевірка Crashlytics"));
    }
}
