using DS.Models;

namespace DS.DTOs
{
    public class ActivityDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ActivityTeamDto
    {
        public ActivityTeamDto() { }

        public ActivityTeamDto(ActivityTeam model)
        {
            Id = model.Id;
            Name = model.Name;
        }

        public int Id { get; set; }
        public string Name { get; set; }
    }
}