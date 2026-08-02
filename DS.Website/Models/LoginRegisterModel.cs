using System.ComponentModel.DataAnnotations;

namespace DS.Website.Models;

public class LoginInputModel
{
    [Required(ErrorMessage = "Email skal udfyldes")]
    [EmailAddress(ErrorMessage = "Ugyldig email-adresse")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kodeord skal udfyldes")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterInputModel
{
    [Required(ErrorMessage = "Fornavn skal udfyldes")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Efternavn skal udfyldes")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email skal udfyldes")]
    [EmailAddress(ErrorMessage = "Ugyldig email-adresse")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kodeord skal udfyldes")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gentag kodeord skal udfyldes")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Kodeordene er ikke ens")]
    public string RepeatPassword { get; set; } = string.Empty;
}