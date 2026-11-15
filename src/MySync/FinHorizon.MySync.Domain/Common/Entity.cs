namespace FinHorizon.MySync.Domain.Common;

/// <summary>Base of every entity: an identity and the user it belongs to (RNF2).</summary>
public abstract class Entity
{
    protected Entity(Guid id, OwnerId ownerId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An entity needs an identity.", nameof(id));
        }

        Id = id;
        OwnerId = ownerId;
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    protected Entity()
    {
    }

    public Guid Id { get; private set; }

    public OwnerId OwnerId { get; private set; }
}
