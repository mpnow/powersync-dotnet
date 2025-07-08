using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MAUITodo.Models;

public class TodoItem : INotifyPropertyChanged
{
    private string _id = "";
    private string _listId = null!;
    private string _createdAt = null!;
    private string? _completedAt;
    private string _description = null!;
    private string _createdBy = null!;
    private string _completedBy = null!;
    private bool _completed = false;
    [JsonProperty("id")]
    public string ID 
    { 
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    [JsonProperty("list_id")] 
    public string ListId 
    { 
        get => _listId;
        set => SetProperty(ref _listId, value);
    }
    
    [JsonProperty("created_at")]
    public string CreatedAt 
    { 
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }
    
    [JsonProperty("completed_at")]
    public string? CompletedAt 
    { 
        get => _completedAt;
        set => SetProperty(ref _completedAt, value);
    }
    
    [JsonProperty("description")]
    public string Description 
    { 
        get => _description;
        set => SetProperty(ref _description, value);
    }
    
    [JsonProperty("created_by")]
    public string CreatedBy 
    { 
        get => _createdBy;
        set => SetProperty(ref _createdBy, value);
    }
    
    [JsonProperty("completed_by")]
    public string CompletedBy 
    { 
        get => _completedBy;
        set => SetProperty(ref _completedBy, value);
    }
    
    [JsonProperty("completed")]
    public bool Completed 
    { 
        get => _completed;
        set => SetProperty(ref _completed, value);
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
