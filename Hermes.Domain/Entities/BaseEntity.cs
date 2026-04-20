namespace Hermes.Domain.Entities;

public interface IBaseEntity
{
    Guid Guid { get; set; }
    int Id { get; set; }
}

public abstract class BaseEntity : IBaseEntity
{
    public int Id { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}