using MAUITodo.ViewModels;
using MAUITodo.Services;

namespace MAUITodo.Views;

public partial class ListsPage : ContentPage
{
    private readonly ListsPageViewModel _viewModel;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    public ListsPage(ListsPageViewModel viewModel, IDialogService dialogService, INavigationService navigationService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _dialogService = dialogService;
        _navigationService = navigationService;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}