using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Aktibasen.Controllers;

[Authorize]
[Route("/api/v1/activity")]
public class ActivityApiController(DataDbContext dataDb) : Controller
{
    public class ActivityDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CreateActivityDTO
    {
        public string Name { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var activities = await dataDb.Activities.ToListAsync();

        var retval = activities.Select(a => new ActivityDTO
        {
            Id = a.Id,
            Name = a.Name
        });

        return Ok(retval);
    }

    [HttpPost]
    public async Task<IActionResult> CreateActivity([FromBody] CreateActivityDTO data)
    {
        await dataDb.Activities.AddAsync(new Models.Activity { Name = data.Name });
        await dataDb.SaveChangesAsync();

        return Ok();
    }
}