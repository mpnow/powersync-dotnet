using PowerSync.Common.Client;
using MAUITodo.Models;
using MAUITodo.Data;
using System.Collections.ObjectModel;

namespace MAUITodo.Views;

public partial class ListsPage
{
    private readonly PowerSyncData database;
    private readonly ObservableCollection<TodoList> todoLists;

    public ListsPage(PowerSyncData powerSyncData)
    {
        InitializeComponent();
        database = powerSyncData;
        todoLists = new ObservableCollection<TodoList>();
        ListsCollection.ItemsSource = todoLists;
        WifiStatusItem.IconImageSource = "wifi_off.png";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            database.Db.RunListener((update) =>
            {
                if (update.StatusChanged != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        WifiStatusItem.IconImageSource = update.StatusChanged.Connected ? "wifi.png" : "wifi_off.png";
                    });

                }
            });
            
            await database.Db.Watch("select * from lists ORDER BY created_at", null, new WatchHandler<TodoList>
            {
                OnResult = (results) =>
                {
                    MainThread.BeginInvokeOnMainThread(() => { 
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
            Console.WriteLine($"Error in OnAppearing: {ex.Message}");
            await DisplayAlert("Error", $"Failed to initialize database: {ex.Message}", "OK");
        }
    }

    private void UpdateTodoListsIncremental(TodoList[] newResults)
    {
        try
        {
            var newLists = newResults.ToList();
            var newListsDict = newLists.ToDictionary(list => list.ID, list => list);
            var listsToRemove = todoLists.Where(existing => !newListsDict.ContainsKey(existing.ID)).ToList();
            foreach (var listToRemove in listsToRemove)
            {
                todoLists.Remove(listToRemove);
            }
            foreach (var existingList in todoLists.ToList())
            {
                if (newListsDict.TryGetValue(existingList.ID, out var newList))
                {
                    if (HasListChanged(existingList, newList))
                    {
                        UpdateListProperties(existingList, newList);
                    }
                }
            }
            var existingIds = new HashSet<string>(todoLists.Select(list => list.ID));
            var newListsToAdd = newLists.Where(list => !existingIds.Contains(list.ID)).ToList();
            foreach (var newList in newListsToAdd)
            {
                var targetIndex = FindInsertPosition(newList, newLists);
                if (targetIndex >= todoLists.Count)
                {
                    todoLists.Add(newList);
                }
                else
                {
                    todoLists.Insert(targetIndex, newList);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in incremental update: {ex.Message}");
            todoLists.Clear();
            foreach (var list in newResults)
            {
                todoLists.Add(list);
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
        if (serverIndex == -1) return todoLists.Count;
        for (var i = serverIndex - 1; i >= 0; i--)
        {
            var previousListId = serverOrderedLists[i].ID;
            var existingIndex = todoLists.ToList().FindIndex(list => list.ID == previousListId);
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
            var name = await DisplayPromptAsync("New List", "Enter list name:");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var list = new TodoList { Name = name };
                await database.SaveListAsync(list);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding list: {ex.Message}");
            await DisplayAlert("Error", "Failed to add list", "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is TodoList list)
            {
                var confirm = await DisplayAlert("Confirm Delete",
                    $"Are you sure you want to delete the list '{list.Name}'?",
                    "Yes", "No");

                if (confirm)
                {
                    await database.DeleteListAsync(list);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting list: {ex.Message}");
            await DisplayAlert("Error", "Failed to delete list", "OK");
        }
    }

    private async void OnListSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is TodoList selectedList)
        {
            await Navigation.PushAsync(new TodoListPage(database, selectedList));
            ListsCollection.SelectedItem = null;
        }
    }
}