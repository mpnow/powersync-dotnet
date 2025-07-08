using MAUITodo.ViewModels;
using MAUITodo.Models;
using MAUITodo.Data;
using MAUITodo.Services;

namespace MAUITodo.Views;

public partial class TodoListPage : ContentPage
{
    private readonly TodoListPageViewModel _viewModel;

    public TodoListPage(PowerSyncData database, TodoList selectedList, IDialogService dialogService)
    {
        InitializeComponent();
        _viewModel = new TodoListPageViewModel(database, selectedList, dialogService);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is TodoItem selectedItem)
        {
            if (_viewModel.EditTodoCommand is AsyncRelayCommand asyncCommand)
            {
                await asyncCommand.ExecuteAsync(selectedItem);
            }
            else
            {
                _viewModel.EditTodoCommand.Execute(selectedItem);
            }
            TodoItemsCollection.SelectedItem = null;
        }
    }

    private async void OnCheckBoxChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && 
            checkBox.Parent?.Parent?.BindingContext is TodoItem todo)
        {
            if (_viewModel.ToggleCompletedCommand is AsyncRelayCommand asyncCommand)
            {
                await asyncCommand.ExecuteAsync(todo);
            }
            else
            {
                _viewModel.ToggleCompletedCommand.Execute(todo);
            }
        }
    }
}

