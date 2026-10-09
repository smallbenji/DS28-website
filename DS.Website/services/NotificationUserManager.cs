using DS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DS.Website.Services
{
    public class NotificationUserManager : UserManager<User>
    {
        private readonly EmailService emailService;

        public NotificationUserManager(
            IUserStore<User> store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<User> passwordHasher,
            IEnumerable<IUserValidator<User>> userValidators,
            IEnumerable<IPasswordValidator<User>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<User>> logger,
            EmailService emailService)
            : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            this.emailService = emailService;
        }

        public override async Task<IdentityResult> CreateAsync(User user)
        {
            var result = await base.CreateAsync(user);
            if (result.Succeeded) emailService.QueueNewUserNotificationMail(user);

            return result;
        }
    }
}
