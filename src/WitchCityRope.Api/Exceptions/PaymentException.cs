namespace WitchCityRope.Api.Exceptions;

public class PaymentException : ApiException
{
    public PaymentException(string message) : base(message, 400)
    {
    }
}