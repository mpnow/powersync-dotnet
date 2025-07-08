using PowerSync.Common.Client;
using MAUITodo.Data;
using MAUITodo.Models;
using MAUITodo.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MAUITodo.ViewModels;

public class TodoListPageViewModel : ViewModelBase
{
    private readonly PowerSyncData _database;
    private readonly TodoList _selectedList;
    private readonly IDialogService _dialogService;

    public ObservableCollection<TodoItem> TodoItems { get; }

    #pragma warning disable IDE0051 // Remove unused private members
    public string ListName => _selectedList?.Name ?? "";
    #pragma warning restore IDE0051

    public ICommand AddTodoCommand { get; }
    public ICommand DeleteTodoCommand { get; }
    public ICommand ToggleCompletedCommand { get; }
    public ICommand EditTodoCommand { get; }

    public TodoListPageViewModel(PowerSyncData database, TodoList selectedList, IDialogService dialogService)
    {
        _database = database;
        _selectedList = selectedList;
        _dialogService = dialogService;
        TodoItems = new ObservableCollection<TodoItem>();

        AddTodoCommand = new AsyncRelayCommand(ExecuteAddTodoAsync);
        DeleteTodoCommand = new AsyncRelayCommand(ExecuteDeleteTodoAsync);
        ToggleCompletedCommand = new AsyncRelayCommand(ExecuteToggleCompletedAsync);
        EditTodoCommand = new AsyncRelayCommand(ExecuteEditTodoAsync);
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _database.Db.Watch("select * from todos where list_id = ? ORDER BY created_at DESC", 
                [_selectedList.ID], new WatchHandler<TodoItem>
            {
                OnResult = (results) =>
                {
                    Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() =>
                    {
                        UpdateTodoItemsIncremental(results);
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

    private void UpdateTodoItemsIncremental(TodoItem[] newResults)
    {
        try
        {
            var newItems = newResults.ToList();
            var newItemsDict = newItems.ToDictionary(item => item.ID, item => item);
            
            // Remove items that no longer exist
            var itemsToRemove = TodoItems.Where(existing => !newItemsDict.ContainsKey(existing.ID)).ToList();
            foreach (var itemToRemove in itemsToRemove)
            {
                TodoItems.Remove(itemToRemove);
            }
            
            // Update existing items that have changed
            foreach (var existingItem in TodoItems.ToList())
            {
                if (newItemsDict.TryGetValue(existingItem.ID, out var newItem))
                {
                    if (HasItemChanged(existingItem, newItem))
                    {
                        UpdateItemProperties(existingItem, newItem);
                    }
                }
            }
            
            // Add new items (maintaining order from server)
            var existingIds = new HashSet<string>(TodoItems.Select(item => item.ID));
            var newItemsToAdd = newItems.Where(item => !existingIds.Contains(item.ID)).ToList();
            
            foreach (var newItem in newItemsToAdd)
            {
                var targetIndex = FindInsertPosition(newItem, newItems);
                if (targetIndex >= TodoItems.Count)
                {
                    TodoItems.Add(newItem);
                }
                else
                {
                    TodoItems.Insert(targetIndex, newItem);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in incremental update: {ex.Message}");
            TodoItems.Clear();
            foreach (var item in newResults)
            {
                TodoItems.Add(item);
            }
        }
    }

    private bool HasItemChanged(TodoItem existing, TodoItem updated)
    {
        return existing.Description != updated.Description ||
               existing.Completed != updated.Completed ||
               existing.CompletedAt != updated.CompletedAt ||
               existing.CompletedBy != updated.CompletedBy;
    }

    private void UpdateItemProperties(TodoItem existing, TodoItem updated)
    {
        existing.Description = updated.Description;
        existing.Completed = updated.Completed;
        existing.CompletedAt = updated.CompletedAt;
        existing.CompletedBy = updated.CompletedBy;
        existing.CreatedAt = updated.CreatedAt;
        existing.CreatedBy = updated.CreatedBy;
        existing.ListId = updated.ListId;
    }

    private int FindInsertPosition(TodoItem newItem, List<TodoItem> serverOrderedItems)
    {
        var serverIndex = serverOrderedItems.FindIndex(item => item.ID == newItem.ID);
        if (serverIndex == 0) return 0;
        if (serverIndex == -1) return TodoItems.Count;
        
        for (var i = serverIndex - 1; i >= 0; i--)
        {
            var previousItemId = serverOrderedItems[i].ID;
            var existingIndex = TodoItems.ToList().FindIndex(item => item.ID == previousItemId);
            if (existingIndex != -1)
            {
                return existingIndex + 1;
            }
        }
        return 0;
    }

    private async Task ExecuteAddTodoAsync(object? parameter)
    {
        try
        {
            var description = await _dialogService.DisplayPromptAsync("New Todo", "Enter todo description:");
            if (!string.IsNullOrWhiteSpace(description))
            {
                var todo = new TodoItem
                {
                    Description = description,
                    ListId = _selectedList.ID
                };
                await _database.SaveItemAsync(todo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding todo: {ex.Message}");
            await _dialogService.DisplayAlertAsync("Error", "Failed to add todo");
        }
    }

    private async Task ExecuteDeleteTodoAsync(object? parameter)
    {
        try
        {
            if (parameter is TodoItem todo)
            {
                var confirm = await _dialogService.DisplayConfirmAsync("Confirm Delete",
                    $"Are you sure you want to delete '{todo.Description}'?");

                if (confirm)
                {
                    await _database.DeleteItemAsync(todo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting todo: {ex.Message}");
            await _dialogService.DisplayAlertAsync("Error", "Failed to delete todo");
        }
    }

    private async Task ExecuteToggleCompletedAsync(object? parameter)
    {
        try
        {
            if (parameter is TodoItem todo)
            {
                if (!todo.Completed && todo.CompletedAt == null)
                {
                    todo.Completed = true;
                    todo.CompletedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    await _database.SaveItemAsync(todo);
                }
                else if (todo.Completed && todo.CompletedAt != null)
                {
                    todo.Completed = false;
                    todo.CompletedAt = null; // Uncheck, clear completed time
                    await _database.SaveItemAsync(todo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating todo: {ex.Message}");
            await _dialogService.DisplayAlertAsync("Error", "Failed to update todo");
        }
    }

    private async Task ExecuteEditTodoAsync(object? parameter)
    {
        try
        {
            if (parameter is TodoItem selectedItem)
            {
                var newDescription = await _dialogService.DisplayPromptAsync("Edit Todo",
                    "Enter new description:", selectedItem.Description);

                if (!string.IsNullOrWhiteSpace(newDescription))
                {
                    selectedItem.Description = newDescription;
                    await _database.SaveItemAsync(selectedItem);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error editing todo: {ex.Message}");
            await _dialogService.DisplayAlertAsync("Error", "Failed to edit todo");
        }
    }
}