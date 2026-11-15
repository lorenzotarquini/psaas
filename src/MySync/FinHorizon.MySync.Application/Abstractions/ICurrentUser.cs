using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Abstractions;

/// <summary>The signed-in user. Every use case reads and writes only this user's data (RNF2).</summary>
public interface ICurrentUser
{
    OwnerId Id { get; }
}
