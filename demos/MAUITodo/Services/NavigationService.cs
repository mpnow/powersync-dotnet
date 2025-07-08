namespace MAUITodo.Services;

using MAUITodo.Data;
using MAUITodo.Models;

public interface INavigationService
{
    Task NavigateToTodoListPage(PowerSyncData db, TodoList selectedList, IDialogService dialogService);
    Task GoBackAsync();
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task NavigateToTodoListPage(PowerSyncData db, TodoList selectedList, IDialogService dialogService)
    {
        var page = new MAUITodo.Views.TodoListPage(db, selectedList, dialogService);
        await Application.Current.MainPage.Navigation.PushAsync(page);
    }

    public async Task GoBackAsync()
    {
        await Application.Current.MainPage.Navigation.PopAsync();
    }
}