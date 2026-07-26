using System.Security.Claims;
using LinkNest.Shared.Models;
using LinkNest.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Group members page: list members, invite by email (admin), remove (admin), and leave.
/// </summary>
public partial class GroupMembers
{
    [Parameter]
    public Guid GroupId { get; set; }

    [Inject]
    private IGroupService GroupService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private readonly InviteForm _inviteForm = new();
    private string _pageTitle = "Group";
    private string _groupName = string.Empty;
    private Guid _currentUserId;
    private bool _isAdmin;
    private bool _notFound;
    private string? _inviteError;
    private string? _inviteSuccess;
    private string? _actionError;
    private IReadOnlyList<GroupMemberSummary> _members = [];

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _currentUserId))
        {
            _notFound = true;
            return;
        }

        var groups = await GroupService.GetMyGroupsAsync();
        var group = groups.FirstOrDefault(g => g.Id == GroupId);
        if (group is null)
        {
            _notFound = true;
            return;
        }

        _groupName = group.Name;
        _isAdmin = group.CurrentUserRole == GroupRole.Admin;
        _pageTitle = L.Format("GroupMembersTitle", _groupName);
        await LoadMembersAsync();
    }

    private async Task LoadMembersAsync()
    {
        try
        {
            _members = await GroupService.GetMembersAsync(GroupId);
        }
        catch (HttpRequestException)
        {
            _notFound = true;
        }
    }

    private string RoleLabel(GroupRole role) =>
        role == GroupRole.Admin ? L["RoleAdmin"] : L["RoleMember"];

    private async Task InviteAsync()
    {
        _inviteError = null;
        _inviteSuccess = null;

        if (string.IsNullOrWhiteSpace(_inviteForm.Email))
        {
            _inviteError = L["InviteEmailRequired"];
            return;
        }

        try
        {
            await GroupService.InviteMemberAsync(GroupId, _inviteForm.Email);
            _inviteForm.Email = string.Empty;
            _inviteSuccess = L["InviteSent"];
        }
        catch (ApiBadRequestException ex)
        {
            _inviteError = MapInviteError(ex.Code);
        }
        catch (HttpRequestException)
        {
            _inviteError = L["InviteFailed"];
        }
    }

    private async Task RemoveMemberAsync(Guid userId)
    {
        _actionError = null;

        try
        {
            await GroupService.RemoveMemberAsync(GroupId, userId);
            await LoadMembersAsync();
        }
        catch (HttpRequestException)
        {
            _actionError = L["RemoveMemberFailed"];
        }
    }

    private async Task LeaveAsync()
    {
        _actionError = null;

        try
        {
            await GroupService.LeaveGroupAsync(GroupId);
            Navigation.NavigateTo("/", replace: true);
        }
        catch (HttpRequestException)
        {
            _actionError = L["LeaveGroupFailed"];
        }
    }

    private string MapInviteError(string code) => code switch
    {
        "invitee_not_found" => L["InviteeNotFound"],
        "invitee_email_unconfirmed" => L["InviteeEmailUnconfirmed"],
        "invitee_in_group" => L["InviteeInGroup"],
        "invitee_already_member" => L["InviteeAlreadyMember"],
        "group_full" => L["GroupFull"],
        "invite_already_pending" => L["InviteAlreadyPending"],
        _ => L["InviteFailed"]
    };

    private sealed class InviteForm
    {
        public string Email { get; set; } = string.Empty;
    }
}
