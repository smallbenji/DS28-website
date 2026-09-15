using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DS.DTOs;

public class UpdateGroupPreSignupDto
{
    [JsonRequired, Range(0, int.MaxValue)]
    public int Beaver { get; set; }
    [JsonRequired, Range(0, int.MaxValue)]
    public int Wolf { get; set; }
    [JsonRequired, Range(0, int.MaxValue)]
    public int Junior { get; set; }
    [JsonRequired, Range(0, int.MaxValue)]
    public int Trop { get; set; }
    [JsonRequired, Range(0, int.MaxValue)]
    public int Senior { get; set; }
    [JsonRequired, Range(0, int.MaxValue)]
    public int Rover { get; set; }
    [JsonRequired, Range(0, int.MaxValue)]
    public int Leader { get; set; }
}
