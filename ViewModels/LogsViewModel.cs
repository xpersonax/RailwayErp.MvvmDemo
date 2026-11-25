using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services.Logging;

namespace RailwayErp.MvvmDemo.ViewModels;

/// <summary>Варіант фільтра рівня для Picker.</summary>
public record LevelFilterOption(string Title, LogLevel MinLevel)
{
    public override string ToString() => Title;
}

/// <summary>
/// Екран перегляду локальних логів (Лаб. 5, п. 2.2.6):
/// останні N записів із файлу з фільтрацією за рівнем.
/// </summary>
public partial class LogsViewModel : ObservableObject
{
    private readonly ILogReaderService _logReader;
    private readonly ILogger<LogsViewModel> _logger;

    public LogsViewModel(ILogReaderService logReader, ILogger<LogsViewModel> logger)
    {
        _logReader = logReader;
        _logger = logger;
        SelectedLevel = LevelOptions[0];
        SelectedCount = CountOptions[1];
    }

    public ObservableCollection<LogEntry> Entries { get; } = [];

    public IReadOnlyList<LevelFilterOption> LevelOptions { get; } =
    [
        new("Усі рівні", LogLevel.Trace),
        new("Debug і вище", LogLevel.Debug),
        new("Information і вище", LogLevel.Information),
        new("Warning і вище", LogLevel.Warning),
        new("Error і вище", LogLevel.Error),
        new("Лише Critical (Fatal)", LogLevel.Critical),
    ];

    public IReadOnlyList<int> CountOptions { get; } = [50, 100, 200, 500];

    [ObservableProperty]
    public partial LevelFilterOption SelectedLevel { get; set; }

    [ObservableProperty]
    public partial int SelectedCount { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    public string LogDirectory => LoggingConfig.LogDirectory;

    // Зміна фільтра одразу перечитує журнал.
    partial void OnSelectedLevelChanged(LevelFilterOption value) => RefreshCommand.Execute(null);

    partial void OnSelectedCountChanged(int value) => RefreshCommand.Execute(null);

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            var entries = await _logReader.ReadLatestAsync(SelectedCount, SelectedLevel.MinLevel);

            Entries.Clear();
            foreach (var e in entries)
                Entries.Add(e);

            var files = _logReader.GetLogFiles();
            Summary = $"Показано {entries.Count} запис(ів) · фільтр: {SelectedLevel.Title} · файлів логу: {files.Count}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не вдалося прочитати лог-файли");
            Summary = "Помилка читання логів: " + ex.Message;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private static async Task ShowDetailsAsync(LogEntry? entry)
    {
        if (entry is null) return;

        var text = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\n[{entry.LevelShort}] {entry.Source}\n\n{entry.Message}";
        if (entry.HasException)
            text += "\n\n" + entry.Exception;

        var page = Shell.Current?.CurrentPage;
        if (page is not null)
            await page.DisplayAlertAsync("Запис журналу", text, "OK");
    }
}
