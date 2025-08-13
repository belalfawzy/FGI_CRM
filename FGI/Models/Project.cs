using System.ComponentModel.DataAnnotations;

namespace FGI.Models
{
    public class Project
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="Project Name is Required")]
        public string Name { get; set; }

        public ICollection<Unit>? Units { get; set; }
        public ICollection<Lead>? Leads { get; set; }
    }
    
}
