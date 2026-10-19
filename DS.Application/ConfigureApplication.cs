using DS.Application.FileStorage;
using DS.Application.Seeders;
using DS.Application.Services.Email;
using DS.Application.Services.Groups;
using DS.Application.Services.Activities;
using DS.Application.Services.Invitations;
using DS.Application.Services.Files;
using DS.Application.Services.Organization;
using DS.Application.Services.Registration;
using DS.Application.Services.Users;
using DS.Application.Services.Boards;
using DS.Application.Services.Profile;
using DS.Application.Services.Exports;
using DS.Application.Services.Accounts;
using Microsoft.Extensions.DependencyInjection;
using DS.Application.Services.Materials;

public static class ConfigureApplication
{
    public static IServiceCollection ConfigureDsApplication(this IServiceCollection services)
    {
        services.AddScoped<ActivityService>();
        services.AddScoped<ActivityTeamService>();
        services.AddScoped<EmailService>();
        services.AddScoped<InvitationService>();
        services.AddScoped<GroupInvitationService>();
        services.AddScoped<MaterialService>();
        services.AddScoped<FileService>();
        services.AddScoped<OrgService>();
        services.AddScoped<RegistrationSettingsService>();
        services.AddScoped<UserService>();
        services.AddScoped<BoardService>();
        services.AddScoped<MeService>();
        services.AddScoped<ExportService>();
        services.AddScoped<AccountService>();
        services.AddScoped<GroupService>();
        services.AddScoped<GroupPreSignupService>();
        services.AddScoped<GroupInfoService>();
        services.AddScoped<GroupAdministrationService>();
        services.AddScoped<GroupMembersService>();
        services.AddScoped<RoleSeeder>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        return services;
    }
}
