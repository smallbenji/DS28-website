using DS.Domain.Models;

namespace DS.Domain.DTOs;

public class CreateMaterialDto
{
    public double Price { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }

    public Material ToModel()
    {
        return new Material
        {
            Price = Price,
            Name = Name,
            Url = Url
        };
    }
}

public class CreateMaterialOrderDto
{
    public Activity Activity { get; set; }
    public Material Material { get; set; }

    public int Quantity { get; set; }
    public DateTime OrderedToDate { get; set; }

    public MaterialOrder ToModel()
    {
        return new MaterialOrder
        {
            Activity = Activity,
            Material = Material,
            Quantity = Quantity,
            OrderedToDate = OrderedToDate
        };
    }
}

public class MaterialDto
{
    public MaterialDto() { }
    public MaterialDto(Material material)
    {
        Id = material.Id;
        Price = material.Price;
        Name = material.Name;
        Url = material.Url;
    }

    public int Id { get; set; }
    public double Price { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
}