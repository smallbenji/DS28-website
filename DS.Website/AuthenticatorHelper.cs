using Microsoft.AspNetCore.Identity;

namespace DS.Website
{
    public static class AuthenticatorHelper
    {
        public static async Task EnsureAuthenticatorKeyAsync(UserManager<User> userManager, User user)
        {
            if (string.IsNullOrEmpty(await userManager.GetAuthenticatorKeyAsync(user)))
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
            }
        }

        public static async Task<string> GetAuthenticatorUriAsync(UserManager<User> userManager, User user)
        {
            var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                return null;
            }

            var issuer = userManager.Options.Tokens.AuthenticatorIssuer ?? "DS HQ";
            var email = await userManager.GetEmailAsync(user) ?? user.UserName ?? string.Empty;

            return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={unformattedKey}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";
        }
    }
}
