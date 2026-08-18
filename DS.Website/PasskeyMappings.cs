using DS.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace DS.Website;

public static class PasskeyMappings
{
    public static PasskeyDto ToDto(this UserPasskeyInfo passkey)
    {
        return new PasskeyDto
        {
            Id = Base64UrlTextEncoder.Encode(passkey.CredentialId),
            Name = passkey.Name,
            CreatedAt = passkey.CreatedAt,
            Transports = passkey.Transports ?? [],
            IsBackedUp = passkey.IsBackedUp
        };
    }

    public static List<PasskeyDto> ToDtoList(this IList<UserPasskeyInfo> passkeys)
    {
        return passkeys.Select(p => p.ToDto()).ToList();
    }
}
