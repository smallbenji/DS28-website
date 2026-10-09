namespace DS.DTOs;

public class ScoutTableDto
{
    [Column("ID", Sortable = true)]
    public int Id { get; set; }
    [Column("Navn", Sortable = true, Filterable = true)]
    public string Name { get; set; }
    [Column("Fødselsdag", Type = "date")]
    public DateTime Birthday { get; set; }
    [Column("Køn", Type = "badge")]
    public string Gender { get; set; }
    [Column("Gruppe")]
    public string GroupName { get; set; }
}