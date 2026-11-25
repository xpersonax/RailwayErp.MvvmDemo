using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Repositories;

/// <summary>
/// Репозиторій потягів у пам'яті з імітацією затримки "сховища".
/// Логує ключові події на відповідних рівнях:
///  Debug — технічні деталі, Information — успішні операції,
///  Warning — некоректні дані / нештатні, але оброблені ситуації,
///  Error — збій сховища.
/// </summary>
public class InMemoryTrainRepository : ITrainRepository
{
    private readonly ILogger<InMemoryTrainRepository> _logger;
    private readonly List<Train> _trains;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int _nextId;

    public InMemoryTrainRepository(ILogger<InMemoryTrainRepository> logger)
    {
        _logger = logger;
        _trains =
        [
            new Train { Id = 1, Number = "743К", Route = "Київ — Львів", Wagons = 9, Status = TrainStatus.OnRoute },
            new Train { Id = 2, Number = "091К", Route = "Київ — Одеса", Wagons = 14, Status = TrainStatus.InDepot },
            new Train { Id = 3, Number = "063П", Route = "Харків — Ужгород", Wagons = 16, Status = TrainStatus.Maintenance },
            new Train { Id = 4, Number = "715К", Route = "Київ — Харків", Wagons = 8, Status = TrainStatus.OnRoute },
        ];
        _nextId = _trains.Max(t => t.Id) + 1;
    }

    public async Task<IReadOnlyList<Train>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Запит списку потягів до сховища");
        await Task.Delay(400, ct); // імітація звернення до БД / API

        await _lock.WaitAsync(ct);
        try
        {
            var result = _trains.Select(Clone).ToList();
            _logger.LogInformation("Зі сховища завантажено {Count} потягів", result.Count);
            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Train> SaveAsync(Train train, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(train);
        _logger.LogDebug("Збереження потяга {Number} (Id={Id})", train.Number, train.Id);

        // Валідація: некоректні дані — це очікувана ситуація, тому Warning, а не Error.
        if (string.IsNullOrWhiteSpace(train.Number) || string.IsNullOrWhiteSpace(train.Route))
        {
            _logger.LogWarning("Відхилено збереження: не заповнено номер або маршрут (Number='{Number}', Route='{Route}')",
                train.Number, train.Route);
            throw new ArgumentException("Номер і маршрут потяга обов'язкові.");
        }

        if (train.Wagons is <= 0 or > 30)
        {
            _logger.LogWarning("Відхилено збереження потяга {Number}: некоректна кількість вагонів {Wagons}",
                train.Number, train.Wagons);
            throw new ArgumentOutOfRangeException(nameof(train), "Кількість вагонів має бути від 1 до 30.");
        }

        await Task.Delay(300, ct);

        // Імітація збою сховища: номер, що починається з "ERR", "ламає" запис.
        if (train.Number.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
        {
            var ex = new IOException("Сховище недоступне: таймаут запису (імітація).");
            _logger.LogError(ex, "Помилка сховища під час збереження потяга {Number}", train.Number);
            throw ex;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_trains.Any(t => t.Id != train.Id &&
                                 string.Equals(t.Number, train.Number, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Відхилено збереження: потяг з номером {Number} уже існує", train.Number);
                throw new InvalidOperationException($"Потяг {train.Number} уже є в парку.");
            }

            if (train.Id == 0)
            {
                train.Id = _nextId++;
                _trains.Add(Clone(train));
                _logger.LogInformation("Додано потяг {Number} (Id={Id}), маршрут {Route}", train.Number, train.Id, train.Route);
            }
            else
            {
                var index = _trains.FindIndex(t => t.Id == train.Id);
                if (index < 0)
                {
                    _logger.LogWarning("Потяг Id={Id} не знайдено для оновлення", train.Id);
                    throw new KeyNotFoundException($"Потяг Id={train.Id} не знайдено.");
                }

                _trains[index] = Clone(train);
                _logger.LogInformation("Оновлено потяг {Number} (Id={Id})", train.Number, train.Id);
            }

            return Clone(train);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await Task.Delay(200, ct);
        await _lock.WaitAsync(ct);
        try
        {
            var removed = _trains.RemoveAll(t => t.Id == id) > 0;
            if (removed)
                _logger.LogInformation("Видалено потяг Id={Id}", id);
            else
                _logger.LogWarning("Спроба видалити неіснуючий потяг Id={Id}", id);
            return removed;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static Train Clone(Train t) => new()
    {
        Id = t.Id,
        Number = t.Number,
        Route = t.Route,
        Wagons = t.Wagons,
        Status = t.Status
    };
}
