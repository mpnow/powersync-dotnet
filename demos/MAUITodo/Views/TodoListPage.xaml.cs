using PowerSync.Common.Client;
using MAUITodo.Data;
using MAUITodo.Models;
using System.Collections.ObjectModel;

namespace MAUITodo.Views;

public partial class TodoListPage
{
    private readonly PowerSyncData database;
    private readonly TodoList selectedList;
    private readonly ObservableCollection<TodoItem> todoItems;

    public TodoListPage(PowerSyncData powerSyncData, TodoList list)
    {
        InitializeComponent();
        database = powerSyncData;
        selectedList = list;
        todoItems = new ObservableCollection<TodoItem>();
        TodoItemsCollection.ItemsSource = todoItems;
        BindingContext = this;
    }

    public string ListName => selectedList?.Name ?? "";

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            await database.Db.Watch("select * from todos where list_id = ? ORDER BY created_at DESC", [selectedList.ID], new WatchHandler<TodoItem>
            {
                OnResult = (results) =>
                {
                    MainThread.BeginInvokeOnMainThread(() => { 
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
            Console.WriteLine($"Error in OnAppearing: {ex.Message}");
            await DisplayAlert("Error", $"Failed to initialize database: {ex.Message}", "OK");
        }
    }

    private void UpdateTodoItemsIncremental(TodoItem[] newResults)
    {
        try
        {
            var newItems = newResults.ToList();
            var newItemsDict = newItems.ToDictionary(item => item.ID, item => item);
            var itemsToRemove = todoItems.Where(existing => !newItemsDict.ContainsKey(existing.ID)).ToList();
            foreach (var itemToRemove in itemsToRemove)
            {
                todoItems.Remove(itemToRemove);
            }
            foreach (var existingItem in todoItems.ToList())
            {
                if (newItemsDict.TryGetValue(existingItem.ID, out var newItem))
                {
                    if (HasItemChanged(existingItem, newItem))
                    {
                        UpdateItemProperties(existingItem, newItem);
                    }
                }
            }
            var existingIds = new HashSet<string>(todoItems.Select(item => item.ID));
            var newItemsToAdd = newItems.Where(item => !existingIds.Contains(item.ID)).ToList();
            foreach (var newItem in newItemsToAdd)
            {
                var targetIndex = FindInsertPosition(newItem, newItems);
                if (targetIndex >= todoItems.Count)
                {
                    todoItems.Add(newItem);
                }
                else
                {
                    todoItems.Insert(targetIndex, newItem);
                }
            }
        }
        catch (Exception ex)
        {
            todoItems.Clear();
            foreach (var item in newResults)
            {
                todoItems.Add(item);
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
        if (serverIndex == -1) return todoItems.Count;
        for (var i = serverIndex - 1; i >= 0; i--)
        {
            var previousItemId = serverOrderedItems[i].ID;
            var existingIndex = todoItems.ToList().FindIndex(item => item.ID == previousItemId);
            if (existingIndex != -1)
            {
                return existingIndex + 1;
            }
        }
        return 0;
    }
    
    private async void OnAddClicked(object sender, EventArgs e)
    {
        try
        {
            var description = await DisplayPromptAsync("New Todo", "Enter todo description:");
            if (!string.IsNullOrWhiteSpace(description))
            {
                var todo = new TodoItem
                {
                    Description = description,
                    ListId = selectedList.ID
                };
                await database.SaveItemAsync(todo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding todo: {ex.Message}");
            await DisplayAlert("Error", "Failed to add todo", "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is TodoItem todo)
            {
                var confirm = await DisplayAlert("Confirm Delete",
                    $"Are you sure you want to delete '{todo.Description}'?",
                    "Yes", "No");

                if (confirm)
                {
                    await database.DeleteItemAsync(todo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting todo: {ex.Message}");
            await DisplayAlert("Error", "Failed to delete todo", "OK");
        }
    }

    private async void OnCheckBoxChanged(object sender, CheckedChangedEventArgs e)
    {
        try
        {
            if (sender is CheckBox checkBox && checkBox.Parent?.Parent?.BindingContext is TodoItem todo)
            {
                if (e.Value && todo.CompletedAt == null)
                {
                    todo.Completed = e.Value;
                    todo.CompletedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    await database.SaveItemAsync(todo);
                }
                else if (e.Value == false && todo.CompletedAt != null)
                {
                    todo.Completed = e.Value;
                    todo.CompletedAt = null; // Uncheck, clear completed time
                    await database.SaveItemAsync(todo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating todo: {ex.Message}");
            await DisplayAlert("Error", "Failed to update todo", "OK");
        }
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.CurrentSelection.FirstOrDefault() is TodoItem selectedItem)
            {
                var newDescription = await DisplayPromptAsync("Edit Todo",
                    "Enter new description:",
                    initialValue: selectedItem.Description);

                if (!string.IsNullOrWhiteSpace(newDescription))
                {
                    selectedItem.Description = newDescription;
                    await database.SaveItemAsync(selectedItem);
                }

                TodoItemsCollection.SelectedItem = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error editing todo: {ex.Message}");
            await DisplayAlert("Error", "Failed to edit todo", "OK");
        }
    }
}

