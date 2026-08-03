using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers
{
    [Authorize(Roles = nameof(AppRoles.UsersView))]
    public class UserManagementController(UserManager<User> userManager, SignInManager<User> signInManager) : Controller
    {
        public async Task<IActionResult> Index(string id = null)
        {
            var users = await userManager.Users
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var model = new UserManagementIndexViewModel
            {
                Users = users,
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var user = await userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Beskytter mod CSRF-angreb
        public async Task<IActionResult> Details(string id, [Bind("Id,GroupNumber")] User inputModel)
        {
            if (id != inputModel.Id)
            {
                return BadRequest("ID-match mislykkedes.");
            }

            // 1. Hent den reelle bruger op fra databasen
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // 2. Opdater de tilladte felter (f.eks. GroupNumber)
            user.GroupNumber = inputModel.GroupNumber;

            // 3. Gem ændringerne i databasen via Identity
            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                // Hvis databasen afviser det, smider vi fejlene på modellen og viser siden igen
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(user);
            }

            // 4. OPFRISK RETTIGHEDER: Tjek om brugeren redigerer sig selv eller en anden
            var currentUserId = userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                // Hvis jeg redigerer mig selv, opdater min cookie her og nu
                await signInManager.RefreshSignInAsync(user);
            }
            else
            {
                // Hvis jeg redigerer en anden, tving deres browser til at genindlæse ved næste klik
                await userManager.UpdateSecurityStampAsync(user);
            }

            // 5. Send administratoren tilbage til oversigten med en succesoplevelse
            // Du kan også vælge at returnere 'RedirectToAction(nameof(Details), new { id })' hvis du vil blive på siden
            return RedirectToAction(nameof(Index));
        }
    }

    public class UserManagementIndexViewModel
    {
        public List<User> Users { get; set; } = new();
    }
}