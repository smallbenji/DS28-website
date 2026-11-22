using DS.Application.Data;
using DS.Application.Services.Groups;
using DS.Domain.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Registration
{
    public class RegistrationSettingsService(IDataDbContext dataDb)
    {
        public async Task<RegistrationSettingsDto> GetSettings()
        {
            return await dataDb.RegistrationSettings
                .AsNoTracking()
                .Where(s => s.Id == GroupLookup.RegistrationSettingsId)
                .Select(s => new RegistrationSettingsDto
                {
                    IsPreSignupOpen = s.IsPreSignupOpen,
                    IsSignupOpen = s.IsSignupOpen,
                    IsActivityWishRoundOpen = s.IsActivityWishRoundOpen,
                    IsActivitySignupOpen = s.IsActivitySignupOpen
                })
                .SingleOrDefaultAsync();
        }

        public async Task<RegistrationSettingsDto> UpdateSettings(RegistrationSettingsDto data)
        {
            var settings = await dataDb.RegistrationSettings
                .SingleOrDefaultAsync(s => s.Id == GroupLookup.RegistrationSettingsId);
            if (settings == null) return null;

            if (data.IsPreSignupOpen != null) settings.IsPreSignupOpen = data.IsPreSignupOpen.Value;
            if (data.IsSignupOpen != null) settings.IsSignupOpen = data.IsSignupOpen.Value;
            if (data.IsActivityWishRoundOpen != null) settings.IsActivityWishRoundOpen = data.IsActivityWishRoundOpen.Value;
            if (data.IsActivitySignupOpen != null) settings.IsActivitySignupOpen = data.IsActivitySignupOpen.Value;

            await dataDb.SaveChangesAsync();

            return new RegistrationSettingsDto
            {
                IsPreSignupOpen = settings.IsPreSignupOpen,
                IsSignupOpen = settings.IsSignupOpen,
                IsActivityWishRoundOpen = settings.IsActivityWishRoundOpen,
                IsActivitySignupOpen = settings.IsActivitySignupOpen
            };
        }
    }
}
