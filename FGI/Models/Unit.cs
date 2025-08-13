using System.ComponentModel.DataAnnotations;

namespace FGI.Models
{
    public class Unit
    {
        public int Id { get; set; }

        public string? UnitCode { get; set; }  // Made nullable and not required

        public bool IsAvailable { get; set; } = true;

        public int? ProjectId { get; set; }  // Made nullable
        public Project? Project { get; set; }

        // Unit type enum
        [Required]
        public UnitType Type { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Area must be at least 1 sqm")]
        public int Area { get; set; } // in square meters

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Must have at least 1 bedroom")]
        public int Bedrooms { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Must have at least 1 bathroom")]
        public int Bathrooms { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public int? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [Phone]
        [StringLength(20, ErrorMessage = "Owner phone number cannot exceed 20 characters")]
        public string? OwnerPhone { get; set; }
    }

    public enum UnitType
    {
        Apartment,
        Villa,
        Chalet,
        Duplex
    }
}