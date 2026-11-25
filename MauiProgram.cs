using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using RailwayErp.MvvmDemo.Repositories;
using RailwayErp.MvvmDemo.Services;
using RailwayErp.MvvmDemo.Services.CrashReporting;
using RailwayErp.MvvmDemo.Services.Logging;
using RailwayErp.MvvmDemo.ViewModels;
using RailwayErp.MvvmDemo.Views;
using Serilog;
#if ANDROID && FIREBASE_CONFIGURED
using Plugin.Firebase.Core.Platforms.Android;
using Plugin.Firebase.Crashlytics;
#endif

namespace RailwayErp.MvvmDemo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // 1) Serilog створюємо якомога раніше, щоб залогувати навіть помилки старту.
        Log.Logger = LoggingConfig.CreateLogger();
        // 2) Глобальні обробники — теж до побудови застосунку.
        GlobalExceptionHandler.Register();
        Log.Information("Запуск RailwayErp {Version} на {Platform} {OsVersion}",
            AppInfo.Current.VersionString, DeviceInfo.Current.Platform, DeviceInfo.Current.VersionString);

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .RegisterCrashReporting();

        // 3) Serilog як провайдер ILogger<T>: усі ILogger<T> з DI пишуть у Serilog-sink'и.
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        // DI: репозиторій, сервіси, ViewModel'и, сторінки
        builder.Services.AddSingleton<ITrainRepository, InMemoryTrainRepository>();
        builder.Services.AddSingleton<ICrashReportingService, CrashReportingService>();
        builder.Services.AddSingleton<ILogReaderService, LogReaderService>();
        builder.Services.AddSingleton<IBugReportService, BugReportService>();

        builder.Services.AddSingleton<TrainsViewModel>();
        builder.Services.AddTransient<LogsViewModel>();
        builder.Services.AddTransient<DiagnosticsViewModel>();

        builder.Services.AddSingleton<TrainsPage>();
        builder.Services.AddTransient<LogsPage>();
        builder.Services.AddTransient<DiagnosticsPage>();

        var app = builder.Build();

        // Тепер, коли DI готовий, передаємо обробникам сервіс крашів.
        GlobalExceptionHandler.Register(app.Services.GetRequiredService<ICrashReportingService>());

        return app;
    }

    /// <summary>
    /// Ініціалізація Firebase Crashlytics (Лаб. 5, п. 2.2.5).
    /// Firebase стартує в OnCreate головної Activity. Без google-services.json цей крок пропускається.
    /// </summary>
    private static MauiAppBuilder RegisterCrashReporting(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID && FIREBASE_CONFIGURED
            events.AddAndroid(android => android.OnCreate((activity, _) =>
            {
                CrossFirebase.Initialize(activity, () => Platform.CurrentActivity!);
                CrossFirebaseCrashlytics.Current.SetCrashlyticsCollectionEnabled(true);
                CrossFirebaseCrashlytics.Current.SetCustomKey("app_version", AppInfo.Current.VersionString);
                Log.Information("Firebase Crashlytics ініціалізовано");
            }));
#endif
        });
        return builder;
    }
}
