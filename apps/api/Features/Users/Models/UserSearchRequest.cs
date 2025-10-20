namespace WitchCityRope.Api.Features.Users.Models;

/// <summary>
/// Request model for searching/filtering users
/// Follows the simplified vertical slice architecture pattern
/// </summary>
public class UserSearchRequest
{
    public string? SearchTerm { get; set; }
    public string? Role { get; set; }
    public string[]? RoleFilters { get; set; }  // Array of roles for OR filtering
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "SceneName";
    public bool SortDescending { get; set; } = false;
}