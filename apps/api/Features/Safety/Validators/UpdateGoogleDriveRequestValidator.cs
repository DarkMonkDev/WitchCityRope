using FluentValidation;
using WitchCityRope.Api.Features.Safety.Models;

namespace WitchCityRope.Api.Features.Safety.Validators;

/// <summary>
/// Validator for Google Drive links update request
/// </summary>
public class UpdateGoogleDriveRequestValidator : AbstractValidator<UpdateGoogleDriveRequest>
{
    public UpdateGoogleDriveRequestValidator()
    {
        RuleFor(x => x.GoogleDriveFolderUrl)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.GoogleDriveFolderUrl))
            .WithMessage("Valid Google Drive folder URL required")
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.GoogleDriveFolderUrl))
            .WithMessage("URL cannot exceed 500 characters");

        RuleFor(x => x.GoogleDriveFinalReportUrl)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.GoogleDriveFinalReportUrl))
            .WithMessage("Valid Google Drive report URL required")
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.GoogleDriveFinalReportUrl))
            .WithMessage("URL cannot exceed 500 characters");
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
