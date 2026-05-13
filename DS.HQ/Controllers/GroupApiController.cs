using DS.Models;
using Microsoft.AspNetCore.Mvc;

namespace DS.HQ.Controllers
{
    [Route("/api/v1/group")]
    public class GroupApiController : Controller
    {
        private readonly DataDbContext dataDb;

        public GroupApiController(DataDbContext dataDb)
        {
            this.dataDb = dataDb;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var retval = dataDb.Groups.ToList();

            return Ok(retval);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] Group data)
        {
            dataDb.Groups.Add(data);

            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGroup([FromBody] Group data)
        {
            dataDb.Groups.Update(data);

            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Index(int id)
        {
            var group = await dataDb.Groups.FindAsync(id);

            if (group == null)
            {
                return NotFound($"Group with ID {id} not found.");
            }

            dataDb.Groups.Remove(group);
            await dataDb.SaveChangesAsync();

            return Ok();
        }
    }
}