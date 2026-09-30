namespace DS.Models;

public class Material : IAuditableEntity, ISoftDeleteable
{
    public int Id { get; set; }
    public double Price { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class MaterialOrder : IAuditableEntity, ISoftDeleteable
{
    public int Id { get; set; }
    public Activity Activity { get; set; }
    public Material Material { get; set; }
    public int Quantity { get; set; }
    public DateTime OrderedToDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
