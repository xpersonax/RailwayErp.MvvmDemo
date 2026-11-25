namespace RailwayErp.MvvmDemo.Services.CrashReporting;

/// <summary>
/// Абстракція над сервісом автоматичного збору крашів (Лаб. 5, п. 2.2.5).
/// ViewModel'и залежать від інтерфейсу, а не від Firebase напряму —
/// тож сервіс можна замінити (Sentry, App Center тощо) без змін у решті коду.
/// </summary>
public interface ICrashReportingService
{
    /// <summary>Чи підключено реальний хмарний сервіс (на Android з google-services.json).</summary>
    bool IsEnabled { get; }

    string ProviderName { get; }

    /// <summary>Хлібна крихта — потрапить у звіт поруч зі стеком.</summary>
    void Log(string message);

    void SetCustomKey(string key, string value);

    /// <summary>Записати виняток як нефатальну (або фатальну) помилку.</summary>
    void RecordException(Exception exception, bool fatal = false);

    /// <summary>Чи впав застосунок під час попереднього запуску.</summary>
    bool DidCrashOnPreviousExecution();

    /// <summary>Штучний краш для перевірки, що звіт доходить до панелі сервісу.</summary>
    void ForceTestCrash();
}
