using System.Security.Claims;
using DS.Domain.Models;
using DS.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DS.Infrastructure.Identity
{
    public class LoginTrackingSignInManager : SignInManager<User>
    {
        private readonly DataDbContext context;
        private bool isRefreshingSignIn;

        public LoginTrackingSignInManager(
            UserManager<User> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<User> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<User>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<User> confirmation,
            DataDbContext context)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            this.context = context;
        }

        public override async Task SignInWithClaimsAsync(User user, AuthenticationProperties authenticationProperties, IEnumerable<Claim> additionalClaims)
        {
            await base.SignInWithClaimsAsync(user, authenticationProperties, additionalClaims);

            if (isRefreshingSignIn) return;

            var lastLoginAt = DateTimeOffset.UtcNow;
            await context.Users
                .Where(candidate => candidate.Id == user.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.LastLoginAt, lastLoginAt));

            user.LastLoginAt = lastLoginAt;
        }

        public override async Task RefreshSignInAsync(User user)
        {
            isRefreshingSignIn = true;
            try
            {
                await base.RefreshSignInAsync(user);
            }
            finally
            {
                isRefreshingSignIn = false;
            }
        }
    }
}
