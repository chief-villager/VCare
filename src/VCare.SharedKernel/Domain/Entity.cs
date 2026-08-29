namespace VCare.SharedKernel.Domain;

public abstract class Entity
{
    protected Entity(Guid id) => Id = id;

    // Parameterless ctor for EF Core materialisation.
    protected Entity() { }

    public Guid Id { get; protected set; }
}
