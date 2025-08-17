namespace FGI.Models
{
    public class Owner
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Email { get; set; }  // Made nullable
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public IEnumerable<Unit> Units { get; set; } = new List<Unit>();
    }
}
