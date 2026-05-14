namespace DS.HQ.Controllers
{
    public class DSUser
    {
        public NETCore.Keycloak.Client.Models.Users.KcUser User { get; set; }
        public string GroupNumber { get; set; }
        public List<NETCore.Keycloak.Client.Models.Groups.KcGroup> Roles { get; set; }
    }
 
    public class UserIndexViewModel
    {
        public List<DSUser> Users { get; set; }
    }
 
    public class UserDetailsViewModel
    {
        public DSUser DSUser { get; set; }
        public List<NETCore.Keycloak.Client.Models.Groups.KcGroup> AvailableGroups { get; set; }
        public List<string> SelectedGroups { get; set; }
    }
}