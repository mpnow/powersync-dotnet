using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MAUITodo.Models;

public class TodoList : INotifyPropertyChanged
{
    private string _id = "";
    private string _createdAt = null!;
    private string _name = null!;
    private string _ownerId = null!;
    [JsonProperty("id")]
    public string ID 
    { 
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    [JsonProperty("created_at")]
    public string CreatedAt 
    { 
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }
    
    [JsonProperty("name")]
    public string Name 
    { 
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    [JsonProperty("owner_id")]
    public string OwnerId 
    { 
        get => _ownerId;
        set => SetProperty(ref _ownerId, value);
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