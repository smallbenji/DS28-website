using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DS.Models
{
    public class RegistrationSettings
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public bool IsPreSignupOpen { get; set; }
        public bool IsSignupOpen { get; set; }
    }
}
