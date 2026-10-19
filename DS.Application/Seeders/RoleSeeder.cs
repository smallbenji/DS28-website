using DS.Domain;
using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DS.Application.Seeders
{
    public class RoleSeeder(ILogger<RoleSeeder> logger, RoleManager<Role> roleManager)
    {
        public async Task Seed()
        {
            try
            {
                var groupNames = Enum.GetNames<AppGroups>();

                foreach (var groupName in groupNames)
                {
                    var groupExists = await roleManager.RoleExistsAsync(groupName);
                    if (!groupExists)
                    {
                        var newRole = new Role { Name = groupName };
                        var result = await roleManager.CreateAsync(newRole);

                        if (result.Succeeded)
                        {
                            logger.LogInformation("Seedede rollen/gruppen: {GroupName}", groupName);
                        }
                        else
                        {
                            logger.LogError("Fejl under seeding af rollen {GroupName}: {Errors}",
                                groupName, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Der opstod en fejl under seeding af databasen.");
            }
        }
    }
}