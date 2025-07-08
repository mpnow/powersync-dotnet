using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MAUITodo.Data;
using MAUITodo.Views;
using MAUITodo.ViewModels;
using MAUITodo.Services;

namespace MAUITodo;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		builder.Logging.SetMinimumLevel(LogLevel.Debug);
		builder.Logging.AddDebug();
		
		// Register data services
		builder.Services.AddSingleton<PowerSyncData>();
		
		// Register services
		builder.Services.AddSingleton<IDialogService, DialogService>();
		builder.Services.AddSingleton<INavigationService, NavigationService>();
		
		// Register ViewModels
		builder.Services.AddTransient<ListsPageViewModel>();
		builder.Services.AddTransient<TodoListPageViewModel>();
		
		// Register Views
		builder.Services.AddTransient<ListsPage>();
		builder.Services.AddTransient<TodoListPage>();
		builder.Services.AddTransient<SqlConsolePage>();

		return builder.Build();
	}
}
