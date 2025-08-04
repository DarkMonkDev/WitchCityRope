using System.Collections.Generic;
using System.Threading.Tasks;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Core.Services;

public interface IContentPageService
{
    Task<ContentPage?> GetBySlugAsync(string slug);
    Task<ContentPage?> GetByIdAsync(int id);
    Task<IEnumerable<ContentPage>> GetAllAsync();
    Task<ContentPage> UpdateAsync(int id, string content, string title, string userId, string? metaDescription = null, string? metaKeywords = null);
    Task<ContentPage> GetOrCreateBySlugAsync(string slug, string defaultTitle);
}