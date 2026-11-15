using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Preferences;

namespace FinHorizon.MySync.Application.Abstractions.Persistence;

/// <summary>One <see cref="UserPreferences"/> per user, missing until the user chooses (UC13-R4).</summary>
public interface IPreferencesRepository
{
    Task<UserPreferences?> GetAsync(OwnerId owner, CancellationToken cancellationToken = default);

    void Add(UserPreferences preferences);
}
