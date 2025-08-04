using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Web.Services;

namespace WitchCityRope.Web.Pages.Admin.Members
{
    public partial class MemberDetail : ComponentBase
    {
        [Parameter] public Guid MemberId { get; set; }
        
        [Inject] private ApiClient ApiClient { get; set; } = null!;
        [Inject] private NavigationManager Navigation { get; set; } = null!;
        [Inject] private ILogger<MemberDetail> Logger { get; set; } = null!;
        
        private MemberDetailDto? Member;
        private List<UserNoteDto> Notes = new();
        private List<MemberEventHistoryDto> EventHistory = new();
        private List<IncidentSummaryDto> Incidents = new();
        
        private bool IsLoading = true;
        private string ActiveTab = "overview";
        
        private string PageTitle => Member != null ? $"{Member.SceneName} - Member Details" : "Member Details";
        
        protected override async Task OnInitializedAsync()
        {
            await LoadMemberData();
        }
        
        private async Task LoadMemberData()
        {
            try
            {
                IsLoading = true;
                StateHasChanged();
                
                var memberDetails = await ApiClient.GetAsync<MemberDetailViewModel>($"api/admin/members/{MemberId}");
                if (memberDetails != null)
                {
                    Member = memberDetails.Member;
                    Notes = memberDetails.Notes ?? new List<UserNoteDto>();
                    EventHistory = memberDetails.EventHistory ?? new List<MemberEventHistoryDto>();
                    Incidents = memberDetails.Incidents ?? new List<IncidentSummaryDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load member details for {MemberId}", MemberId);
                // TODO: Show error notification
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
        
        private async Task LoadNotes()
        {
            try
            {
                var notes = await ApiClient.GetAsync<List<UserNoteDto>>($"api/admin/members/{MemberId}/notes");
                Notes = notes ?? new List<UserNoteDto>();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load notes for member {MemberId}", MemberId);
            }
        }
        
        private void SetActiveTab(string tab)
        {
            ActiveTab = tab;
        }
        
        private void SetOverviewTab() => SetActiveTab("overview");
        private void SetEventsTab() => SetActiveTab("events");
        private void SetNotesTab() => SetActiveTab("notes");
        private void SetIncidentsTab() => SetActiveTab("incidents");
        private void SetSettingsTab() => SetActiveTab("settings");
        
        private async Task HandleMemberUpdate(UpdateMemberDto dto)
        {
            try
            {
                await ApiClient.PutAsync<UpdateMemberDto, object>($"api/admin/members/{MemberId}", dto);
                await LoadMemberData(); // Reload to get updated data
                // TODO: Show success notification
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update member {MemberId}", MemberId);
                // TODO: Show error notification
            }
        }
    }
}