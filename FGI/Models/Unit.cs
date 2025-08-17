using FGI.Enums;
using System.ComponentModel.DataAnnotations;

namespace FGI.Models
{
    public class Unit
    {
        public int Id { get; set; }

        public string? UnitCode { get; set; }

        public bool IsAvailable { get; set; } = true;

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public UnitType Type { get; set; }

        public UnitSaleType UnitType { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = false)]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Area must be at least 1 sqm")]
        public int Area { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Must have at least 1 bedroom")]
        public int Bedrooms { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Must have at least 1 bathroom")]
        public int Bathrooms { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        public int? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // أزل السمات [Phone] و[StringLength] من OwnerId لأنها من نوع int
        public int? OwnerId { get; set; }
        public Owner? Owner { get; set; } // جعلتها nullable أيضاً
        [Required]
        public Currency Currency { get; set; } = Currency.EGP;

    }

    public enum UnitType
    {
        Apartment,
        Penthouse,
        IVilla,
        StandAloneVilla,
        TownHouse,
        TwinHouse,
        Studio,
        ParkVilla,
        Chalet,
        Duplex
    }
    public enum Currency
    {
        [Display(Name = "EGP")]
        EGP,
        [Display(Name = "USD")]
        USD
    }
}