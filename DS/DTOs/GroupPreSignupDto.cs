using System.ComponentModel.DataAnnotations;

namespace DS.DTOs;

public class GroupPreSignupDto
{
    [Range(1, int.MaxValue)]
    public int GroupId { get; set; }
    [Range(0, int.MaxValue)]
    public int Beaver { get; set; }
    [Range(0, int.MaxValue)]
    public int Wolf { get; set; }
    [Range(0, int.MaxValue)]
    public int Junior { get; set; }
    [Range(0, int.MaxValue)]
    public int Trop { get; set; }
    [Range(0, int.MaxValue)]
    public int Senior { get; set; }
    [Range(0, int.MaxValue)]
    public int Rover { get; set; }
    [Range(0, int.MaxValue)]
    public int Leader { get; set; }
    [Required]
    public string FirstName { get; set; }
    [Required]
    public string LastName { get; set; }
    [Required, EmailAddress]
    public string Email { get; set; }
    [Required, MinLength(4)]
    public string Password { get; set; }
}
