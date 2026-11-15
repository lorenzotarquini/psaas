namespace FinHorizon.MySync.Domain.Common;

/// <summary>Field error codes shared with the Admin Portal (RNF5); the frontend translates them.</summary>
public static class ErrorCodes
{
    public const string Required = "Required";
    public const string InvalidFormat = "InvalidFormat";
    public const string MaxLengthExceeded = "MaxLengthExceeded";
    public const string NotUnique = "NotUnique";
    public const string InvalidValue = "InvalidValue";
    public const string LimitReached = "LimitReached";
}
