using System.Reflection;
using System.Text.Json;
using DS.Application.Services.BackgroundJobs;
using DS.Backend.Controllers.ApiControllers;
using DS.Domain;
using DS.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;

var failures = new List<string>();

void Require(bool condition, string message)
{
    if (!condition) failures.Add(message);
}

var mail = JsonSerializer.SerializeToElement(new
{
    ToEmail = "anna.hansen@nordika.dk",
    ToName = "Anna Hansen",
    Subject = "Bekræft din tilmelding til DS28",
    Body = "<p>Hej Anna, klik her: https://hq.example/invitation/abc</p>"
});

var arguments = BackgroundJobArgumentMasker.Mask("data", mail).ToDictionary(argument => argument.Name);

Require(arguments.Keys.SequenceEqual(["toEmail", "toName", "subject", "body"]), "MailData skal foldes ud til camelCase-felter.");
Require(arguments["toEmail"].Value == "a•••••@nordika.dk" && arguments["toEmail"].Masked, $"Mailadressen skal maskeres: {arguments["toEmail"].Value}");
Require(arguments["toName"].Value == "A••••• H•••••" && arguments["toName"].Masked, $"Navnet skal maskeres: {arguments["toName"].Value}");
Require(arguments["subject"].Value == "Bekræft din tilmelding til DS28" && !arguments["subject"].Masked, "Emnet skal vises uændret.");
Require(arguments["body"].Hidden && arguments["body"].Value == null && arguments["body"].Size > 0, "Mailens indhold skal skjules med størrelse.");

foreach (var key in new[] { "password", "resetToken", "clientSecret", "htmlContent" })
{
    var hidden = BackgroundJobArgumentMasker.Mask(key, JsonSerializer.SerializeToElement("hemmelig")).Single();
    Require(hidden.Hidden && hidden.Value == null, $"{key} skal skjules.");
}

var number = BackgroundJobArgumentMasker.Mask("fileId", JsonSerializer.SerializeToElement(4821)).Single();
Require(number.Value == "4821" && !number.Masked && !number.Hidden, "Tal skal vises uændret.");

var embedded = BackgroundJobArgumentMasker.Mask("note", JsonSerializer.SerializeToElement("Sendt til jens@nordika.dk i går")).Single();
Require(embedded.Value == "Sendt til j•••••@nordika.dk i går" && embedded.Masked, $"Mailadresser i fri tekst skal maskeres: {embedded.Value}");

var nested = BackgroundJobArgumentMasker.Mask("data", JsonSerializer.SerializeToElement(new { Recipients = new[] { "a@nordika.dk" } })).Single();
Require(nested.Hidden && nested.Value == null, "Lister og indlejrede objekter skal skjules.");

var stackTrace = BackgroundJobArgumentMasker.MaskText("SmtpCommandException: 5.1.1 <ole@nordika.dk>: Mailbox unavailable");
Require(!stackTrace.Contains("ole@nordika.dk") && stackTrace.Contains("o•••••@nordika.dk"), $"Fejltekster skal maskeres: {stackTrace}");

var controller = typeof(BackgroundJobApiController);
var viewRoles = controller.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Roles).ToList();
Require(viewRoles.SequenceEqual([nameof(AppRoles.HangfireView)]), "Controlleren skal kræve HangfireView.");

foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
{
    var isRead = action.GetCustomAttributes<HttpMethodAttribute>().All(attribute => attribute.HttpMethods.SequenceEqual(["GET"]));
    var requiresManage = action.GetCustomAttributes<AuthorizeAttribute>().Any(attribute => attribute.Roles == nameof(AppRoles.HangfireManage));

    Require(isRead != requiresManage, $"{action.Name}: handlinger skal kræve HangfireManage, og læsning må ikke.");
}

Require(AppAccess.Matrix[AppGroups.SysAdmin].Contains(nameof(AppRoles.HangfireManage)), "SysAdmin skal have HangfireManage.");
Require(AppAccess.Matrix.Where(entry => entry.Key != AppGroups.SysAdmin).All(entry => !entry.Value.Contains(nameof(AppRoles.HangfireManage))), "Kun SysAdmin må have HangfireManage.");

if (failures.Count > 0)
{
    foreach (var failure in failures) Console.Error.WriteLine($"FEJL: {failure}");

    return 1;
}

Console.WriteLine("Alle baggrundsjob-checks bestået.");

return 0;
