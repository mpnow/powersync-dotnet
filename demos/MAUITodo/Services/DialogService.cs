namespace MAUITodo.Services;

public interface IDialogService
{
    Task<string?> DisplayPromptAsync(string title, string message, string? initialValue = null);
    Task<bool> DisplayConfirmAsync(string title, string message);
    Task DisplayAlertAsync(string title, string message);
}

public class DialogService : IDialogService
{
    public async Task<string?> DisplayPromptAsync(string title, string message, string? initialValue = null)
    {
        if (Application.Current?.MainPage != null)
        {
            return await Application.Current.MainPage.DisplayPromptAsync(title, message, initialValue: initialValue);
        }
        return null;
    }

    public async Task<bool> DisplayConfirmAsync(string title, string message)
    {
        if (Application.Current?.MainPage != null)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
        }
        return false;
    }

    public async Task DisplayAlertAsync(string title, string message)
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }
    }
}