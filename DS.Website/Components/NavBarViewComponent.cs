using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Components
{
    public class NavBarViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync() => View();
    }
}