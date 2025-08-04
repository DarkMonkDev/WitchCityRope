using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Web.Services;

namespace WitchCityRope.Web.Pages.Admin.Members
{
    public partial class Index : ComponentBase, IDisposable
    {
        [Inject] private ApiClient ApiClient { get; set; } = null!;
        [Inject] private NavigationManager Navigation { get; set; } = null!;
        [Inject] private ILogger<Index> Logger { get; set; } = null!;
        
        protected List<MemberListDto> Members = new();
        protected MemberStatsDto Stats = new();
        protected bool IsLoadingMembers = true;
        protected bool IsLoadingStats = true;
        
        protected string VettingStatus = "vetted";
        protected string SearchTerm = "";
        protected int PageSize = 100;
        protected int CurrentPage = 1;
        protected int TotalCount = 0;
        protected string SortBy = "sceneName";
        protected string SortDirection = "asc";
        
        private System.Timers.Timer? _debounceTimer;
        
        protected override async Task OnInitializedAsync()
        {
            await Task.WhenAll(
                LoadMembersAsync(),
                LoadStatsAsync()
            );
        }
        
        protected async Task LoadMembersAsync()
        {
            try
            {
                IsLoadingMembers = true;
                StateHasChanged();
                
                var queryParams = new Dictionary<string, string?>
                {
                    ["vettingStatus"] = VettingStatus,
                    ["searchTerm"] = SearchTerm,
                    ["page"] = CurrentPage.ToString(),
                    ["pageSize"] = PageSize.ToString(),
                    ["sortBy"] = SortBy,
                    ["sortDirection"] = SortDirection
                };
                
                var queryString = string.Join("&", queryParams
                    .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                    .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value!)}"));
                
                var result = await ApiClient.GetAsync<PagedMemberResult>($"api/admin/members?{queryString}");
                if (result != null)
                {
                    Members = result.Members ?? new List<MemberListDto>();
                    TotalCount = result.TotalCount;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load members");
                // TODO: Show error notification
            }
            finally
            {
                IsLoadingMembers = false;
                StateHasChanged();
            }
        }
        
        protected async Task LoadStatsAsync()
        {
            try
            {
                IsLoadingStats = true;
                StateHasChanged();
                
                var stats = await ApiClient.GetAsync<MemberStatsDto>("api/admin/members/stats");
                if (stats != null)
                {
                    Stats = stats;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load member stats");
            }
            finally
            {
                IsLoadingStats = false;
                StateHasChanged();
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
                    await LoadMembersAsync();
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
            await LoadMembersAsync();
        }
        
        protected async Task HandlePageChange(int newPage)
        {
            CurrentPage = newPage;
            await LoadMembersAsync();
        }
        
        protected void NavigateToMember(MemberListDto member)
        {
            Navigation.NavigateTo($"/admin/members/{member.Id}", forceLoad: true);
        }
        
        public void Dispose()
        {
            _debounceTimer?.Dispose();
        }
    }
}