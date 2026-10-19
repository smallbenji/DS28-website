using Microsoft.AspNetCore.Identity;

namespace DS.Application.Services.Accounts
{
    public record PasswordResetResult(bool UserNotFound, IdentityResult Result = null);
}
