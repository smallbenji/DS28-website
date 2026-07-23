using System.ComponentModel.DataAnnotations;

namespace DS.Models;

public class Material
{
    [Key]
    public int Id { get; set; }
    public double Price { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
}

public class MaterialOrder
{
    [Key]
    public int Id { get; set; }
    public Activity Activity { get; set; }
    public Material Material { get; set; }
    public int Quantity { get; set; }
    public DateTime OrderedToDate { get; set; }
}