using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.HQ.Controllers
{
    [Authorize(Roles = Role.Admin)]
    [Route("/api/v1/group")]
    public class GroupApiController(DataDbContext dataDb, IKeycloakHelper keycloakHelper) : Controller
    {
        public class GroupDTO
        {
            public GroupDTO(List<Group> groups)
            {
                Groups = groups;
            }

            public List<Group> Groups { get; set; }
            public Dictionary<string, List<DSUser>> Users { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = dataDb.Groups.ToList();

            var retval = new GroupDTO(data)
            {
                Users = (await keycloakHelper.GetUsers()).Where(x => !string.IsNullOrEmpty(x.GroupNumber)).GroupBy(x => x.GroupNumber).ToDictionary(x => x.Key, x => x.ToList())
            };

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
        public async Task<IActionResult> UpdateGroup([FromBody] Group data, int id)
        {
            if (data.Id != id)
            {
                return BadRequest("ID mismatch");
            }

            dataDb.Groups.Update(data);

            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
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