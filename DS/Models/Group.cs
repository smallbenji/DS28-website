using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DS.Models;

public class Group
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Patrol> Patrols { get; set; }
}

public class Patrol
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
}