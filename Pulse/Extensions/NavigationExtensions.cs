using Pulse.Services;

namespace Pulse.Extensions;

public static class NavigationExtensions
{
    public static async Task NavigateTo(this INavigationService navigation, string route, bool animate = true)
    {
        await navigation.NavigateToAsync(route, animate);
    }

    public static async Task NavigateTo(this INavigationService navigation, string route, Dictionary<string, object> parameters, bool animate = true)
    {
        await navigation.NavigateToAsync(route, parameters, animate);
    }

    public static async Task NavigateBack(this INavigationService navigation, bool animate = true)
    {
        await navigation.NavigateBackAsync(animate);
    }

    public static async Task NavigateToRoot(this INavigationService navigation, bool animate = true)
    {
        await navigation.NavigateBackToRootAsync(animate);
    }
}