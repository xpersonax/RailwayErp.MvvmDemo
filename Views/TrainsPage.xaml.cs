using RailwayErp.MvvmDemo.ViewModels;

namespace RailwayErp.MvvmDemo.Views;

public partial class TrainsPage : ContentPage
{
    private readonly TrainsViewModel _viewModel;

    public TrainsPage(TrainsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Trains.Count == 0)
            _viewModel.LoadCommand.Execute(null);
    }
}
