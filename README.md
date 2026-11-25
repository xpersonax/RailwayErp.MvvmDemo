# RailwayErp.MvvmDemo — Лабораторна робота №5

**Логування, обробка помилок та аналітика** у .NET MAUI (C#, MVVM, CommunityToolkit.Mvvm).

Застосунок має три вкладки:

| Вкладка | Що демонструє |
|---|---|
| **Потяги** | ViewModel + репозиторій з логуванням завантаження/збереження на рівнях Information / Warning / Error |
| **Логи** | Перегляд останніх N записів лог-файлу з фільтрацією за рівнем (п. 2.2.6) |
| **Діагностика** | Моделювання необроблених винятків, тестовий краш Crashlytics, кнопка «Повідомити про помилку» |

## Що де реалізовано

| Пункт завдання | Файли |
|---|---|
| 2.1.1 Serilog: Console + File (щоденна ротація, 7 файлів) | `Services/Logging/LoggingConfig.cs`, `MauiProgram.cs` |
| 2.1.2 Логування в ViewModel і репозиторії | `ViewModels/TrainsViewModel.cs`, `Repositories/InMemoryTrainRepository.cs` |
| 2.1.3 Глобальні обробники `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` (+ Android/iOS/Windows) | `Services/Logging/GlobalExceptionHandler.cs` |
| 2.1.4 Моделювання необробленого винятку | `ViewModels/DiagnosticsViewModel.cs` → кнопки на вкладці «Діагностика» |
| 2.2.5 Firebase Crashlytics | `Services/CrashReporting/*`, `MauiProgram.RegisterCrashReporting`, `Platforms/Android/Resources/values/strings.xml` |
| 2.2.6 Екран логів із фільтром | `Services/Logging/LogReaderService.cs`, `ViewModels/LogsViewModel.cs`, `Views/LogsPage.xaml` |
| 1.6 Звіт про помилку (лог + інфо про пристрій) | `Services/BugReportService.cs` |

> **Чому Firebase, а не App Center:** Microsoft закрила Visual Studio App Center 31.03.2025, тому використано Firebase Crashlytics — другий варіант із завдання.

## Запуск

Потрібні .NET 10 SDK і workload `maui` (Android).

> ⚠️ Тримайте проєкт у **короткому шляху** (напр. `C:\src\RailwayErp.MvvmDemo`). Компілятор XAML для Windows падає, якщо шлях довший за 260 символів.

```powershell
dotnet build -f net10.0-android
dotnet build -t:Run -f net10.0-android   # запуск на емуляторі/пристрої
```

Без `google-services.json` застосунок повністю працює (логи, обробники, екран логів), але звіти в хмару не надсилаються — на вкладці «Діагностика» буде написано «не налаштовано».

## Підключення Firebase Crashlytics (п. 2.2.5)

1. Відкрийте [Firebase Console](https://console.firebase.google.com) → **Add project** (Google Analytics можна вимкнути).
2. **Add app → Android**, package name: `com.companyname.railwayerp.mvvmdemo` (як `ApplicationId` у `.csproj`).
3. Завантажте `google-services.json` і покладіть **у корінь проєкту** поруч із `.csproj`.
   Проєкт сам його підхопить і ввімкне константу `FIREBASE_CONFIGURED`.
4. У консолі Firebase: **Build → Crashlytics → Enable**.
5. Зберіть і запустіть **Release без дебагера** (дебагер перехоплює краші раніше за Crashlytics):
   ```powershell
   dotnet build -t:Run -f net10.0-android -c Release
   ```
6. Вкладка «Діагностика» → **Тестовий краш (Crashlytics)** → застосунок впаде.
7. **Запустіть застосунок ще раз** — саме тоді Crashlytics надсилає звіт.
8. За 2–5 хв звіт з'явиться в Firebase Console → Crashlytics. Зробіть скріншот для звіту.

> `google-services.json` додано в `.gitignore`: він прив'язаний до вашого Firebase-проєкту. Якщо викладач хоче бачити його в репозиторії — приберіть рядок із `.gitignore`.

## Як отримати матеріали для звіту

1. **Фрагмент лог-файлу з різними рівнями:** вкладка «Діагностика» → «Записати повідомлення всіх рівнів», потім «Виняток у фоновому Task». Файл лежить у `FileSystem.AppDataDirectory/logs/app-YYYYMMDD.log`. На Android:
   ```powershell
   adb shell run-as com.companyname.railwayerp.mvvmdemo ls files/logs
   adb shell run-as com.companyname.railwayerp.mvvmdemo cat files/logs/app-20261125.log
   ```
2. **Перевірка необробленого винятку (п. 2.1.4):** після кнопки «Виняток у фоновому Task» у лозі з'явиться `[ERR] : Необроблений виняток у Task (UnobservedTaskException)` зі стеком; після «Виняток у фоновому потоці» застосунок закриється, а після перезапуску на вкладці «Логи» буде запис `[FTL]`.
3. **Скріншот екрана логів:** вкладка «Логи», фільтр, наприклад, «Warning і вище».
4. **Скріншот Crashlytics:** див. розділ вище.

Відповіді на контрольні питання — у [`docs/Lab5-zvit.md`](docs/Lab5-zvit.md).
