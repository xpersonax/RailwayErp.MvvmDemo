using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Repositories;
using RailwayErp.MvvmDemo.Services.CrashReporting;

namespace RailwayErp.MvvmDemo.ViewModels;

/// <summary>
/// Список потягів і форма додавання (Лаб. 5, п. 2.1.2: логування ключових подій ViewModel).
/// </summary>
public partial class TrainsViewModel : ObservableObject
{
    private readonly ITrainRepository _repository;
    private readonly ICrashReportingService _crashReporting;
    private readonly ILogger<TrainsViewModel> _logger;

    public TrainsViewModel(ITrainRepository repository, ICrashReportingService crashReporting, ILogger<TrainsViewModel> logger)
    {
        _repository = repository;
        _crashReporting = crashReporting;
        _logger = logger;
    }

    public ObservableCollection<Train> Trains { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    public partial string NewNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewRoute { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewWagons { get; set; } = "10";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool StatusIsError { get; set; }

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Завантаження списку потягів розпочато");
        _crashReporting.Log("TrainsViewModel.Load");

        try
        {
            var trains = await _repository.GetAllAsync();
            Trains.Clear();
            foreach (var t in trains)
                Trains.Add(t);

            _logger.LogInformation("Список потягів завантажено: {Count} шт. за {Elapsed} мс", trains.Count, sw.ElapsedMilliseconds);
            SetStatus(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка завантаження списку потягів");
            _crashReporting.RecordException(ex);
            SetStatus("Не вдалося завантажити потяги. Спробуйте ще раз.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddTrainAsync()
    {
        if (IsBusy) return;

        if (!int.TryParse(NewWagons, out var wagons))
        {
            _logger.LogWarning("Некоректне значення кількості вагонів у формі: '{Value}'", NewWagons);
            SetStatus("Кількість вагонів має бути числом.", isError: true);
            return;
        }

        var train = new Train
        {
            Number = NewNumber.Trim(),
            Route = NewRoute.Trim(),
            Wagons = wagons,
            Status = TrainStatus.InDepot
        };

        IsBusy = true;
        _logger.LogInformation("Спроба зберегти новий потяг {Number}", train.Number);

        try
        {
            var saved = await _repository.SaveAsync(train);
            Trains.Add(saved);
            _logger.LogInformation("Потяг {Number} успішно збережено (Id={Id})", saved.Number, saved.Id);
            SetStatus($"Потяг {saved.Number} додано.");
            NewNumber = string.Empty;
            NewRoute = string.Empty;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            // Помилка вводу користувача — очікувана ситуація: Warning без стеку.
            _logger.LogWarning("Потяг не збережено: {Reason}", ex.Message);
            SetStatus(ex.Message, isError: true);
        }
        catch (Exception ex)
        {
            // Технічний збій — Error зі стеком + нефатальний звіт у Crashlytics.
            _logger.LogError(ex, "Невдале збереження потяга {Number}", train.Number);
            _crashReporting.RecordException(ex);
            SetStatus("Помилка сховища. Запис не збережено.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteTrainAsync(Train? train)
    {
        if (train is null || IsBusy) return;

        IsBusy = true;
        try
        {
            if (await _repository.DeleteAsync(train.Id))
            {
                Trains.Remove(train);
                SetStatus($"Потяг {train.Number} видалено.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка видалення потяга {Number}", train.Number);
            SetStatus("Не вдалося видалити потяг.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(string? message, bool isError = false)
    {
        StatusIsError = isError;
        StatusMessage = message;
    }
}
