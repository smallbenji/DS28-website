using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DS.Website.Models;
using Microsoft.EntityFrameworkCore;
using DS.Models;

namespace DS.Website.Controllers;

public class HomeViewModel
{
    public List<DS.Models.Activity> Activities { get; set; }
}

public class HomeController(DataDbContext dataDb) : Controller
{

    public async Task<IActionResult> Index()
    {
        var Activities = await dataDb.Activities.ToListAsync();

        var retval = new HomeViewModel
        {
            Activities = Activities
        };

        return View(retval);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost("activities/create-simple")]
    public async Task<IActionResult> CreateSimple([FromForm] string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return BadRequest("Navn må ikke være tomt");
        }

        // 1. Opret og gem den nye aktivitet
        var newActivity = new DS.Models.Activity
        {
            Name = name,
        };

        dataDb.Activities.Add(newActivity);
        await dataDb.SaveChangesAsync();

        // 2. Hent hele den opdaterede liste fra databasen
        var allActivities = await dataDb.Activities
            .ToListAsync();

        // 3. Returner den nye liste som HTML
        return PartialView("_ActivityList", allActivities);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
