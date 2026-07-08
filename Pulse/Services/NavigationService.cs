using Pulse.Utils;
using System.Collections.ObjectModel;

namespace Pulse.Services;

public interface INavigationService
{
    Task NavigateToAsync(string route, bool animate = true);
    Task NavigateToAsync(string route, Dictionary<string, object> parameters, bool animate = true);
    Task NavigateBackAsync(bool animate = true);
    Task NavigateBackToRootAsync(bool animate = true);
    Task GoToAsync(string route, bool animate = true);
    Task GoToAsync(string route, Dictionary<string, object> parameters, bool animate = true);
    Task PopModalAsync(bool animate = true);
    Task PushModalAsync(string route, bool animate = true);
    Task PushModalAsync(string route, Dictionary<string, object> parameters, bool animate = true);
    Task ShowShellAsync();
    string GetCurrentRoute();
}

public class NavigationService : INavigationService
{
    private readonly SemaphoreSlim _navigationLock = new SemaphoreSlim(1, 1);
    private bool _isNavigating;

    public async Task NavigateToAsync(string route, bool animate = true)
    {
        await NavigateSafely(async () =>
        {
            await Shell.Current.GoToAsync(route, animate);
        });
    }

    public async Task NavigateToAsync(string route, Dictionary<string, object> parameters, bool animate = true)
    {
        await NavigateSafely(async () =>
        {
            await Shell.Current.GoToAsync(route, animate, parameters);
        });
    }

    public async Task NavigateBackAsync(bool animate = true)
    {
        await NavigateSafely(async () =>
        {
            await Shell.Current.GoToAsync("..", animate);
        });
    }

    public async Task NavigateBackToRootAsync(bool animate = true)
    {
        await NavigateSafely(async () =>
        {
            await Shell.Current.GoToAsync("///", animate);
        });
    }

    public async Task GoToAsync(string route, bool animate = true)
    {
        await NavigateSafely(async () =>
        {
            await Shell.Current.GoToAsync(route, animate);
        });
    }

    public async Task GoToAsync(string route, Dictionary<string, object> parameters, bool animate = true)
    {
        await NavigateSafely(async () =>
        {
            await Shell.Current.GoToAsync(route, animate, parameters);
        });
    }

    public async Task PushModalAsync(string route, bool animate = true)
    {
        await NavigateSafely(async () =>
        {
            await Shell.Current.GoToAsync(route, animate, new Dictionary<string, object> { { "modal", true } });
        });
    }

    public async Task PushModalAsync(string route, Dictionary<string, object> parameters, bool animate = true)
    {
        await NavigateSafely(async () =>
        {
            parameters["modal"] = true;
            await Shell.Current.GoToAsync(route, animate, parameters);
        });
    }

    public async Task PopModalAsync(bool animate = true)
    {
        await NavigateSafely(async () =>
        {
            await Shell.Current.GoToAsync("..", animate);
        });
    }

    public async Task ShowShellAsync()
    {
        await NavigateSafely(async () =>
        {
            await Shell.Current.GoToAsync("///MainPage");
        });
    }

    public string GetCurrentRoute()
    {
        return Shell.Current?.CurrentState?.Location?.ToString() ?? string.Empty;
    }

    private async Task NavigateSafely(Func<Task> navigationAction)
    {
        await _navigationLock.WaitAsync();
        try
        {
            if (_isNavigating)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Navigazione già in corso, salto l'azione.");
                return;
            }

            _isNavigating = true;
            await navigationAction();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Errore durante la navigazione: {ex.Message}");
            await AlertPopup.ShowError("Errore durante la navigazione: " + ex.Message);
            throw;
        }
        finally
        {
            _isNavigating = false;
            _navigationLock.Release();
        }
    }
}