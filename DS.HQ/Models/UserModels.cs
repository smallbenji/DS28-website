using NETCore.Keycloak.Client.Models.Groups;
using NETCore.Keycloak.Client.Models.Users;

namespace DS.HQ.Controllers
{
    public class DSUser
    {
        public KcUser User { get; set; }
        public string GroupNumber { get; set; }
        public List<KcGroup> Roles { get; set; }
    }
}