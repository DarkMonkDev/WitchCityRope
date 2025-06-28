namespace WitchCityRope.Infrastructure.Services;

public interface ISlugGenerator
{
    string GenerateSlug(string text);
}