using FluentValidation;
using WitchCityRope.Api.Features.Safety.Models;

namespace WitchCityRope.Api.Features.Safety.Validators;

/// <summary>
/// Validator for coordinator assignment request
/// </summary>
public class AssignCoordinatorRequestValidator : AbstractValidator<AssignCoordinatorRequest>
{
    public AssignCoordinatorRequestValidator()
    {
        // CoordinatorId is optional (null = unassign)
        // No additional validation needed for GUID type
    }
}
