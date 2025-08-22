namespace WitchCityRope.Api.Exceptions;

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "Unauthorized") : base(message, 401)
    {
    }
}