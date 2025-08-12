using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Enums;
using WitchCityRope.Web.Services;

namespace WitchCityRope.Web.Pages.Admin.Users
{
    public partial class Index : ComponentBase, IDisposable
    {
        [Inject] private ApiClient ApiClient { get; set; } = null!;
        [Inject] private NavigationManager Navigation { get; set; } = null!;
        [Inject] private ILogger<Index> Logger { get; set; } = null!;
        
        protected List<AdminUserDto> Users = new();
        protected UserStatsDto Stats = new();
        protected List<RoleDto> AvailableRoles = new();
        protected bool IsLoadingUsers = true;
        protected bool IsLoadingStats = true;
        
        // Filter properties
        protected UserRole? SelectedRole = null;
        protected bool? IsActiveFilter = null;
        protected bool? IsVettedFilter = null;
        protected bool? EmailConfirmedFilter = null;
        protected bool? IsLockedOutFilter = null;
        protected string SearchTerm = "";
        protected int PageSize = 50;
        protected int CurrentPage = 1;
        protected int TotalCount = 0;
        protected string SortBy = "sceneName";
        protected string SortDirection = "asc";
        
        // Modal state
        protected bool ShowUserModal = false;
        protected bool ShowPasswordResetModal = false;
        protected bool ShowLockoutModal = false;
        protected AdminUserDto? SelectedUser = null;
        
        private System.Timers.Timer? _debounceTimer;
        
        protected override async Task OnInitializedAsync()
        {
            await Task.WhenAll(
                LoadUsersAsync(),
                LoadStatsAsync(),
                LoadRolesAsync()
            );
        }
        
        protected async Task LoadUsersAsync()
        {
            try
            {
                IsLoadingUsers = true;
                StateHasChanged();
                
                var queryParams = new Dictionary<string, string?>
                {
                    ["searchTerm"] = SearchTerm,
                    ["role"] = SelectedRole?.ToString(),
                    ["isActive"] = IsActiveFilter?.ToString(),
                    ["isVetted"] = IsVettedFilter?.ToString(),
                    ["emailConfirmed"] = EmailConfirmedFilter?.ToString(),
                    ["isLockedOut"] = IsLockedOutFilter?.ToString(),
                    ["page"] = CurrentPage.ToString(),
                    ["pageSize"] = PageSize.ToString(),
                    ["sortBy"] = SortBy,
                    ["sortDirection"] = SortDirection
                };
                
                var queryString = string.Join("&", queryParams
                    .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                    .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value!)}"));
                
                var result = await ApiClient.GetAdminAsync<PagedUserResult>($"api/admin/users?{queryString}");
                if (result != null)
                {
                    Users = result.Users ?? new List<AdminUserDto>();
                    TotalCount = result.TotalCount;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load users");
                // TODO: Show error notification
            }
            finally
            {
                IsLoadingUsers = false;
                StateHasChanged();
            }
        }
        
        protected async Task LoadStatsAsync()
        {
            try
            {
                IsLoadingStats = true;
                StateHasChanged();
                
                var stats = await ApiClient.GetAdminAsync<UserStatsDto>("api/admin/users/stats");
                if (stats != null)
                {
                    Stats = stats;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load user stats");
            }
            finally
            {
                IsLoadingStats = false;
                StateHasChanged();
            }
        }
        
        protected async Task LoadRolesAsync()
        {
            try
            {
                var roles = await ApiClient.GetAdminAsync<List<RoleDto>>("api/admin/users/roles");
                if (roles != null)
                {
                    AvailableRoles = roles;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load roles");
            }
        }
        
        protected void ApplyFilters()
        {
            // Cancel previous timer
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
            
            // Start new timer for debouncing
            _debounceTimer = new System.Timers.Timer(300);
            _debounceTimer.Elapsed += async (sender, e) =>
            {
                _debounceTimer.Stop();
                CurrentPage = 1; // Reset to first page
                await InvokeAsync(async () =>
                {
                    await LoadUsersAsync();
                });
            };
            _debounceTimer.AutoReset = false;
            _debounceTimer.Start();
        }
        
        protected async Task HandleSort(string columnName)
        {
            if (SortBy == columnName)
            {
                SortDirection = SortDirection == "asc" ? "desc" : "asc";
            }
            else
            {
                SortBy = columnName;
                SortDirection = "asc";
            }
            
            CurrentPage = 1;
            await LoadUsersAsync();
        }
        
        protected async Task HandlePageChange(int newPage)
        {
            CurrentPage = newPage;
            await LoadUsersAsync();
        }
        
        protected void NavigateToUser(AdminUserDto user)
        {
            Navigation.NavigateTo($"/admin/users/{user.Id}");
        }
        
        protected async Task HandleQuickAction((string action, AdminUserDto user) actionData)
        {
            var (action, user) = actionData;
            SelectedUser = user;
            
            switch (action.ToLower())
            {
                case "edit":
                    ShowUserModal = true;
                    break;
                case "reset-password":
                    ShowPasswordResetModal = true;
                    break;
                case "lockout":
                case "unlock":
                    ShowLockoutModal = true;
                    break;
                case "toggle-active":
                    await ToggleUserActive(user);
                    break;
                case "toggle-vetted":
                    await ToggleUserVetted(user);
                    break;
            }
            
            StateHasChanged();
        }
        
        protected async Task HandleUserSave(UpdateUserDto updateDto)
        {
            if (SelectedUser == null) return;
            
            try
            {
                await ApiClient.PutAdminAsync<UpdateUserDto, AdminUserDto>($"api/admin/users/{SelectedUser.Id}", updateDto);
                await LoadUsersAsync();
                CloseUserModal();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update user {UserId}", SelectedUser.Id);
                // TODO: Show error notification
            }
        }
        
        protected async Task HandlePasswordReset(ResetUserPasswordDto resetDto)
        {
            if (SelectedUser == null) return;
            
            try
            {
                await ApiClient.PostAdminAsync<ResetUserPasswordDto>($"api/admin/users/{SelectedUser.Id}/reset-password", resetDto);
                ClosePasswordResetModal();
                // TODO: Show success notification
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to reset password for user {UserId}", SelectedUser.Id);
                // TODO: Show error notification
            }
        }
        
        protected async Task HandleLockout(UserLockoutDto lockoutDto)
        {
            if (SelectedUser == null) return;
            
            try
            {
                await ApiClient.PostAdminAsync<UserLockoutDto>($"api/admin/users/{SelectedUser.Id}/lockout", lockoutDto);
                await LoadUsersAsync();
                CloseLockoutModal();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update lockout for user {UserId}", SelectedUser.Id);
                // TODO: Show error notification
            }
        }
        
        private async Task ToggleUserActive(AdminUserDto user)
        {
            try
            {
                var updateDto = new UpdateUserDto
                {
                    IsActive = !user.IsActive,
                    AdminNote = $"User {(user.IsActive ? "deactivated" : "reactivated")} via quick action"
                };
                
                await ApiClient.PutAdminAsync<UpdateUserDto, AdminUserDto>($"api/admin/users/{user.Id}", updateDto);
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to toggle active status for user {UserId}", user.Id);
                // TODO: Show error notification
            }
        }
        
        private async Task ToggleUserVetted(AdminUserDto user)
        {
            try
            {
                var updateDto = new UpdateUserDto
                {
                    IsVetted = !user.IsVetted,
                    AdminNote = $"User vetting status {(user.IsVetted ? "removed" : "granted")} via quick action"
                };
                
                await ApiClient.PutAdminAsync<UpdateUserDto, AdminUserDto>($"api/admin/users/{user.Id}", updateDto);
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to toggle vetted status for user {UserId}", user.Id);
                // TODO: Show error notification
            }
        }
        
        protected void CloseUserModal()
        {
            ShowUserModal = false;
            SelectedUser = null;
            StateHasChanged();
        }
        
        protected void ClosePasswordResetModal()
        {
            ShowPasswordResetModal = false;
            SelectedUser = null;
            StateHasChanged();
        }
        
        protected void CloseLockoutModal()
        {
            ShowLockoutModal = false;
            SelectedUser = null;
            StateHasChanged();
        }
        
        public void Dispose()
        {
            _debounceTimer?.Dispose();
        }
    }
}