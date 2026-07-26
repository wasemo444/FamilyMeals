using LinkNest.Shared.Models;
using LinkNest.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Groups hub: list memberships, create groups, and handle pending invites.
/// </summary>
public partial class Groups
{
    [Inject]
    private IGroupService GroupService { get; set; } = default!;

    [Inject]
    private IContentDataService DataService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private readonly GroupForm _groupForm = new();
    private string? _groupError;
    private string? _inviteActionError;
    private IReadOnlyList<GroupSummary> _groups = [];
    private IReadOnlyList<GroupInviteSummary> _pendingInvites = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _groups = await GroupService.GetMyGroupsAsync();
        _pendingInvites = await GroupService.GetPendingInvitesAsync();
    }

    private string RoleLabel(GroupRole role) =>
        role == GroupRole.Admin ? L["RoleAdmin"] : L["RoleMember"];

    private async Task CreateGroupAsync()
    {
        _groupError = null;

        if (string.IsNullOrWhiteSpace(_groupForm.Name))
        {
            _groupError = L["GroupNameRequired"];
            return;
        }

        try
        {
            var group = await GroupService.CreateAsync(_groupForm.Name);
            _groups = [.. _groups, group];
            _groupForm.Name = string.Empty;
            Navigation.NavigateTo($"/group/{group.Id}");
        }
        catch (HttpRequestException)
        {
            _groupError = L["RegisterFailed"];
        }
    }

    private async Task AcceptInviteAsync(Guid inviteId)
    {
        _inviteActionError = null;

        try
        {
            await GroupService.AcceptInviteAsync(inviteId);
            await DataService.ReloadAsync();
            await LoadAsync();
        }
        catch (ApiBadRequestException ex)
        {
            _inviteActionError = MapInviteAcceptError(ex.Code);
            await LoadAsync();
        }
        catch (HttpRequestException)
        {
            _inviteActionError = L["InviteAcceptFailed"];
        }
    }

    private async Task DeclineInviteAsync(Guid inviteId)
    {
        await GroupService.DeclineInviteAsync(inviteId);
        await LoadAsync();
    }

    private string MapInviteAcceptError(string code) => code switch
    {
        "group_full" => L["GroupFull"],
        "invitee_already_member" => L["InviteeAlreadyMember"],
        _ => L["InviteAcceptFailed"]
    };

    private sealed class GroupForm
    {
        public string Name { get; set; } = string.Empty;
    }
}
