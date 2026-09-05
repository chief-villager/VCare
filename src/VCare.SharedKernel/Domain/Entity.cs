namespace VCare.SharedKernel.Domain;

public abstract class Entity<TId> where TId : notnull
{
    protected Entity(TId id) => Id = id;

    // Parameterless ctor for EF Core materialisation.
    protected Entity() { }

    public TId Id { get; protected set; } = default!;
}
