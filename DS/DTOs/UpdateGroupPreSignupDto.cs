using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DS.Models;

namespace DS.DTOs;

public class UpdateGroupPreSignupDto
{
    public UpdateGroupPreSignupDto(GroupPreSignup data)
    {
        Beaver = data.Beaver;
        Wolf = data.Wolf;
        Junior = data.Junior;
        Trop = data.Trop;
        Senior = data.Senior;
        Rover = data.Rover;
        Leader = data.Leader;
    }

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

    public void ApplyTo(GroupPreSignup signup)
    {
        signup.Beaver = Beaver;
        signup.Wolf = Wolf;
        signup.Junior = Junior;
        signup.Trop = Trop;
        signup.Senior = Senior;
        signup.Rover = Rover;
        signup.Leader = Leader;
    }
}
