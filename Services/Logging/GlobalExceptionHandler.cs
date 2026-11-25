using RailwayErp.MvvmDemo.Services.CrashReporting;
using Serilog;

namespace RailwayErp.MvvmDemo.Services.Logging;

/// <summary>
/// Глобальна обробка необроблених винятків (Лаб. 5, п. 2.1.3).
/// Перехоплює помилки, що вилетіли за межі try/catch, і встигає записати їх у лог
/// (та передати в сервіс збору крашів) до того, як застосунок завершиться.
/// </summary>
public static class GlobalExceptionHandler
{
    private static bool _registered;
    private static ICrashReportingService? _crashReporting;

    public static void Register(ICrashReportingService? crashReporting = null)
    {
        _crashReporting = crashReporting;
        if (_registered)
            return;
        _registered = true;

        // 1) Будь-який необроблений виняток у будь-якому потоці .NET.
        //    Після цієї події процес, як правило, завершується, тому лог треба
        //    одразу скинути на диск (CloseAndFlush).
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception
                     ?? new Exception($"Невідомий об'єкт винятку: {e.ExceptionObject}");
            Log.Fatal(ex, "Необроблений виняток у AppDomain (IsTerminating={IsTerminating})", e.IsTerminating);
            _crashReporting?.RecordException(ex, fatal: true);
            Log.CloseAndFlush();
        };

        // 2) Виняток у Task, результат якого ніхто не дочекався (await/Wait/Result).
        //    Подія спрацьовує, коли збирач сміття фіналізує такий Task.
        //    SetObserved() не дає процесу впасти — помилку лише логуємо.
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Необроблений виняток у Task (UnobservedTaskException)");
            _crashReporting?.RecordException(e.Exception, fatal: false);
            e.SetObserved();
        };

        // 3) Платформо-специфічні обробники.
#if ANDROID
        // Виняток, що вилетів з .NET-коду в Java-світ (напр. в обробнику події UI).
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
        {
            Log.Fatal(e.Exception, "Необроблений виняток Android (UnhandledExceptionRaiser)");
            _crashReporting?.RecordException(e.Exception, fatal: true);
            Log.CloseAndFlush();
            // e.Handled = false — даємо застосунку впасти, щоб Crashlytics зафіксував фатальний краш.
        };
#elif IOS || MACCATALYST
        ObjCRuntime.Runtime.MarshalManagedException += (_, e) =>
        {
            Log.Fatal(e.Exception, "Необроблений виняток iOS (MarshalManagedException)");
            e.ExceptionMode = ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode;
        };
#elif WINDOWS
        if (Microsoft.UI.Xaml.Application.Current is { } winApp)
            winApp.UnhandledException += (_, e) =>
        {
            Log.Fatal(e.Exception, "Необроблений виняток WinUI: {Message}", e.Message);
            Log.CloseAndFlush();
        };
#endif

        Log.Information("Глобальні обробники необроблених винятків зареєстровано");
    }
}
