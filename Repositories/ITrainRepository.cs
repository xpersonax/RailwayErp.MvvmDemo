using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Repositories;

public interface ITrainRepository
{
    Task<IReadOnlyList<Train>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Додає новий потяг (Id == 0) або оновлює існуючий.</summary>
    Task<Train> SaveAsync(Train train, CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
