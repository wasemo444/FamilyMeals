using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Components.Pages;

/// <summary>
/// Error page shown when an unhandled exception occurs outside development.
/// </summary>
/// <remarks>
/// Displays the current activity or HTTP trace identifier to help correlate logs with user reports.
/// </remarks>
public partial class Error : ComponentBase
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }

    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized() =>
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
