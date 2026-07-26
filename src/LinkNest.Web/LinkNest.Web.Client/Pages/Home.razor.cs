using LinkNest.Shared.Models;
using LinkNest.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Home page for browsing, filtering, and creating categories and groups.
/// </summary>
public partial class Home : IDisposable
{
    [Inject]
    private IContentDataService DataService { get; set; } = default!;

    [Inject]
    private IGroupService GroupService { get; set; } = default!;

    private readonly CategoryForm _form = new();
    private string? _error;
    private string? _inviteActionError;
    private string _searchTerm = string.Empty;
    private string _selectedOwnerKey = "personal";
    private HomeContentFilter _filter = HomeContentFilter.All;
    private IReadOnlyList<GroupSummary> _groups = [];
    private IReadOnlyList<GroupInviteSummary> _pendingInvites = [];
    private IReadOnlyList<ContentCategory> _favoriteCategories = [];
    private IReadOnlyList<ContentCategory> _allCategories = [];
    private bool _hasAnyCategories;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadGroupsAsync();
        DataService.DataChanged += Refresh;
        Refresh();
    }

    private async Task LoadGroupsAsync()
    {
        _groups = await GroupService.GetMyGroupsAsync();
        _pendingInvites = await GroupService.GetPendingInvitesAsync();
    }

    private void Refresh()
    {
        _hasAnyCategories = DataService.GetActiveCategories(HomeContentFilter.All).Count > 0;
        _favoriteCategories = DataService.GetFavoriteCategories(_filter)
            .Where(category => MatchesSearch(category.Name))
            .ToList();
        _allCategories = DataService.GetActiveCategories(_filter)
            .Where(category => MatchesSearch(category.Name))
            .ToList();
        StateHasChanged();
    }

    private void OnSearchChanged()
    {
        Refresh();
    }

    private void SetFilter(HomeContentFilter filter)
    {
        _filter = filter;
        Refresh();
    }

    private string FilterClass(HomeContentFilter filter) =>
        _filter == filter ? "filter-btn active" : "filter-btn";

    private bool MatchesSearch(string categoryName) =>
        string.IsNullOrWhiteSpace(_searchTerm)
        || categoryName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase);

    private ContentOwner GetSelectedOwner()
    {
        if (_selectedOwnerKey != "personal" && Guid.TryParse(_selectedOwnerKey, out var groupId))
        {
            return ContentOwner.ForGroup(groupId);
        }

        return ContentOwner.Personal;
    }

    private async Task CreateCategoryAsync()
    {
        _error = null;

        if (string.IsNullOrWhiteSpace(_form.Name))
        {
            _error = L["CategoryNameRequired"];
            return;
        }

        var owner = GetSelectedOwner();

        if (DataService.IsCategoryNameTaken(_form.Name, owner))
        {
            _error = L["CategoryNameDuplicate"];
            return;
        }

        await DataService.AddCategoryAsync(_form.Name, owner);
        _form.Name = string.Empty;
    }

    private async Task AcceptInviteAsync(Guid inviteId)
    {
        _inviteActionError = null;

        try
        {
            await GroupService.AcceptInviteAsync(inviteId);
            await DataService.ReloadAsync();
            await LoadGroupsAsync();
            _filter = HomeContentFilter.Shared;
            Refresh();
        }
        catch (ApiBadRequestException ex)
        {
            _inviteActionError = MapInviteAcceptError(ex.Code);
            await LoadGroupsAsync();
        }
        catch (HttpRequestException)
        {
            _inviteActionError = L["InviteAcceptFailed"];
        }
    }

    private string MapInviteAcceptError(string code) => code switch
    {
        "group_full" => L["GroupFull"],
        "invitee_already_member" => L["InviteeAlreadyMember"],
        _ => L["InviteAcceptFailed"]
    };

    private async Task DeclineInviteAsync(Guid inviteId)
    {
        await GroupService.DeclineInviteAsync(inviteId);
        await LoadGroupsAsync();
    }

    public new void Dispose()
    {
        DataService.DataChanged -= Refresh;
        base.Dispose();
    }

    private sealed class CategoryForm
    {
        public string Name { get; set; } = string.Empty;
    }
}
