using DS.Application.Services.Email;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace DS.Application.Services.Accounts
{
    public class AccountService(UserManager<User> userManager, EmailService emailService)
    {
        public async Task<IdentityResult> Register(RegisterDto data)
        {
            var user = new User
            {
                FirstName = data.FirstName,
                LastName = data.LastName,
                UserName = data.Email,
                Email = data.Email,
                EmailConfirmed = true
            };

            return await userManager.CreateAsync(user, data.Password);
        }

        public async Task ForgotPassword(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null) return;

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            emailService.QueueResetPasswordMail(user, token);
        }

        public async Task<PasswordResetResult> ResetPassword(ResetPasswordDto data)
        {
            var user = await userManager.FindByIdAsync(data.UserId);
            if (user == null) return new PasswordResetResult(true);

            var result = await userManager.ResetPasswordAsync(user, data.Token, data.NewPassword);

            return new PasswordResetResult(false, result);
        }
    }
}
