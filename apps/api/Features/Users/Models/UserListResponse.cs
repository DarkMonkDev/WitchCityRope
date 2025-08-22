namespace WitchCityRope.Api.Features.Users.Models;

/// <summary>
/// Response model for paginated user list
/// Follows the simplified vertical slice architecture pattern
/// </summary>
public class UserListResponse
{
    public List<UserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}