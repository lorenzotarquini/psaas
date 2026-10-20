namespace PSAAS.Application.Abstractions.Tenants;

public sealed class ProductiveUnitNameConflictException : Exception
{
    public ProductiveUnitNameConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
