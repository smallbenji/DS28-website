using System.ComponentModel.DataAnnotations;

namespace DS.DTOs
{
    public class OpenIddictClientUpdateDto
    {
        [Required, MaxLength(100)]
        public string ClientId { get; set; }

        [Required, MaxLength(200)]
        public string DisplayName { get; set; }

        [Required]
        public string ClientType { get; set; }

        public string[] RedirectUris { get; set; } = [];
        public string[] Scopes { get; set; } = [];
        public string[] RequiredRoles { get; set; } = [];
        public bool RequirePkce { get; set; }
    }

    public class OpenIddictClientDto : OpenIddictClientUpdateDto
    {
        public string Id { get; set; }
    }

    public class OpenIddictClientSavedDto
    {
        public OpenIddictClientDto Client { get; set; }
        public string ClientSecret { get; set; }
    }
}
