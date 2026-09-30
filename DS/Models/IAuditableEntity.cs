namespace DS.Models;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

public interface ISoftDeleteable
{
    DateTime? DeletedAt { get; set; }
}
