namespace RailwayErp.MvvmDemo.Models;

/// <summary>Потяг у парку залізничної компанії.</summary>
public class Train
{
    public int Id { get; set; }

    /// <summary>Номер потяга, напр. "743К".</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>Маршрут у форматі "Київ — Львів".</summary>
    public string Route { get; set; } = string.Empty;

    public int Wagons { get; set; }

    public TrainStatus Status { get; set; } = TrainStatus.InDepot;

    public string StatusText => Status switch
    {
        TrainStatus.OnRoute => "У рейсі",
        TrainStatus.InDepot => "У депо",
        TrainStatus.Maintenance => "На ТО",
        _ => Status.ToString()
    };
}

public enum TrainStatus
{
    InDepot,
    OnRoute,
    Maintenance
}
