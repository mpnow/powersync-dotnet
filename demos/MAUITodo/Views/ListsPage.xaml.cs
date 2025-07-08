using MAUITodo.ViewModels;
using MAUITodo.Services;

namespace MAUITodo.Views;

public partial class ListsPage : ContentPage
{
    private readonly ListsPageViewModel _viewModel;
    private readonly IDialogService _dialogService;

    public ListsPage(ListsPageViewModel viewModel, IDialogService dialogService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _dialogService = dialogService;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnListSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is MAUITodo.Models.TodoList selectedList)
        {
            // This navigation logic would need to be moved to a navigation service
            // For now, keeping the direct navigation but this should be refactored
            await Navigation.PushAsync(new TodoListPage(_viewModel.GetDatabase(), selectedList, _dialogService));
            ListsCollection.SelectedItem = null;
        }
    }
}