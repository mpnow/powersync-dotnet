using PowerSync.Common.Client;
using MAUITodo.Data;
using MAUITodo.Models;
using MAUITodo.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MAUITodo.ViewModels;

public class ListsPageViewModel : ViewModelBase
{
    private readonly PowerSyncData _database;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private string _wifiIconSource = "wifi_off.png";

    public ObservableCollection<TodoList> TodoLists { get; }

    public string WifiIconSource
    {
        get => _wifiIconSource;
        set => SetProperty(ref _wifiIconSource, value);
    }

    public ICommand AddListCommand { get; }
    public ICommand DeleteListCommand { get; }
    public ICommand SelectListCommand { get; }

    // Expose database for navigation purposes (temporary until proper navigation service)
    //public PowerSyncData GetDatabase() => _database;

    public ListsPageViewModel(PowerSyncData database, IDialogService dialogService, INavigationService navigationService)
    {
        _database = database;
        _dialogService = dialogService;
        _navigationService = navigationService;
        TodoLists = new ObservableCollection<TodoList>();

        AddListCommand = new AsyncRelayCommand(ExecuteAddListAsync);
        DeleteListCommand = new AsyncRelayCommand(ExecuteDeleteListAsync);
        SelectListCommand = new AsyncRelayCommand(ExecuteSelectListAsync);
    }

    public async Task InitializeAsync()
    {
        try
        {
            _database.Db.RunListener((update) =>
            {
                if (update.StatusChanged != null)
                {
                    Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() =>
                    {
                        WifiIconSource = update.StatusChanged.Connected ? "wifi.png" : "wifi_off.png";
                    });
                }
            });

            await _database.Db.Watch("select * from lists ORDER BY created_at", null, new WatchHandler<TodoList>
            {
                OnResult = (results) =>
                {
                    Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() =>
                    {
                        UpdateTodoListsIncremental(results);
                    });
                },
                OnError = (error) =>
                {
                    Console.WriteLine("Error: " + error.Message);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in InitializeAsync: {ex.Message}");
            await _dialogService.DisplayAlertAsync("Error", $"Failed to initialize database: {ex.Message}");
        }
    }

    private void UpdateTodoListsIncremental(TodoList[] newResults)
    {
        try
        {
            var newLists = newResults.ToList();
            var newListsDict = newLists.ToDictionary(list => list.ID, list => list);
            
            // Remove lists that no longer exist
            var listsToRemove = TodoLists.Where(existing => !newListsDict.ContainsKey(existing.ID)).ToList();
            foreach (var listToRemove in listsToRemove)
            {
                TodoLists.Remove(listToRemove);
            }
            
            // Update existing lists that have changed
            foreach (var existingList in TodoLists.ToList())
            {
                if (newListsDict.TryGetValue(existingList.ID, out var newList))
                {
                    if (HasListChanged(existingList, newList))
                    {
                        UpdateListProperties(existingList, newList);
                    }
                }
            }
            
            // Add new lists
            var existingIds = new HashSet<string>(TodoLists.Select(list => list.ID));
            var newListsToAdd = newLists.Where(list => !existingIds.Contains(list.ID)).ToList();
            
            foreach (var newList in newListsToAdd)
            {
                var targetIndex = FindInsertPosition(newList, newLists);
                if (targetIndex >= TodoLists.Count)
                {
                    TodoLists.Add(newList);
                }
                else
                {
                    TodoLists.Insert(targetIndex, newList);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in incremental update: {ex.Message}");
            TodoLists.Clear();
            foreach (var list in newResults)
            {
                TodoLists.Add(list);
            }
        }
    }

    private bool HasListChanged(TodoList existing, TodoList updated)
    {
        return existing.Name != updated.Name ||
               existing.CreatedAt != updated.CreatedAt ||
               existing.OwnerId != updated.OwnerId;
    }

    private void UpdateListProperties(TodoList existing, TodoList updated)
    {
        existing.Name = updated.Name;
        existing.CreatedAt = updated.CreatedAt;
        existing.OwnerId = updated.OwnerId;
    }

    private int FindInsertPosition(TodoList newList, List<TodoList> serverOrderedLists)
    {
        var serverIndex = serverOrderedLists.FindIndex(list => list.ID == newList.ID);
        if (serverIndex == 0) return 0;
        if (serverIndex == -1) return TodoLists.Count;
        
        for (var i = serverIndex - 1; i >= 0; i--)
        {
            var previousListId = serverOrderedLists[i].ID;
            var existingIndex = TodoLists.ToList().FindIndex(list => list.ID == previousListId);
            if (existingIndex != -1)
            {
                return existingIndex + 1;
            }
        }
        return 0;
    }

    private async Task ExecuteAddListAsync(object? parameter)
    {
        try
        {
            var name = await _dialogService.DisplayPromptAsync("New List", "Enter list name:");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var list = new TodoList { Name = name };
                await _database.SaveListAsync(list);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding list: {ex.Message}");
            await _dialogService.DisplayAlertAsync("Error", "Failed to add list");
        }
    }

    private async Task ExecuteDeleteListAsync(object? parameter)
    {
        try
        {
            if (parameter is TodoList list)
            {
                var confirm = await _dialogService.DisplayConfirmAsync("Confirm Delete",
                    $"Are you sure you want to delete the list '{list.Name}'?");

                if (confirm)
                {
                    await _database.DeleteListAsync(list);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting list: {ex.Message}");
            await _dialogService.DisplayAlertAsync("Error", "Failed to delete list");
        }
    }

    private async Task ExecuteSelectListAsync(object? parameter)
    {
        if (parameter is TodoList selectedList)
        {
            await _navigationService.NavigateToTodoListPage(_database, selectedList, _dialogService);
        }
    }

    //private async Task NavigateToTodoListAsync(TodoList list)
    //{
    //    // Navigation would be handled by a proper navigation service
    //    await Task.Delay(0);
    //}
}