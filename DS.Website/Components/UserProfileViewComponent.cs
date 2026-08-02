using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Components
{
    public class UserProfileViewComponent(UserManager<User> userManager) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new UserProfileViewModel();

            if (User.Identity.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                if (user != null)
                {
                    model.IsLoggedIn = true;
                    model.Name = user.GetFullName();
                }
            }

            return View(model);
        }
    }

    public class UserProfileViewModel
    {
        public string Name { get; set; }
        public bool IsLoggedIn { get; set; }
    }
}