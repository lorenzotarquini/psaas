using FinHorizon.MySync.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FinHorizon.MySync.Infrastructure.Data.Configurations;

/// <summary>Money is stored as numeric(28,8).</summary>
public sealed class MoneyConverter() : ValueConverter<Money, decimal>(money => money.Amount, amount => new Money(amount));

/// <summary>The owner is stored as the Keycloak subject.</summary>
public sealed class OwnerIdConverter() : ValueConverter<OwnerId, string>(owner => owner.Value, value => new OwnerId(value));
