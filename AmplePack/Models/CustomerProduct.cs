using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class CustomerProduct
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot be longer than 100 characters")]
        public string ProductName { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string Description { get; set; } = string.Empty;
        
        // Box Dimensions (in inches - industry standard)
        [Required(ErrorMessage = "Length is required")]
        [Range(2, 72, ErrorMessage = "Length must be between 2 and 72 inches")]
        [Display(Name = "Length (inches)", Description = "Box length in inches")]
        public decimal Length { get; set; }
        
        [Required(ErrorMessage = "Width is required")]
        [Range(2, 72, ErrorMessage = "Width must be between 2 and 72 inches")]
        [Display(Name = "Width (inches)", Description = "Box width in inches")]
        public decimal Width { get; set; }
        
        [Required(ErrorMessage = "Height is required")]
        [Range(1, 48, ErrorMessage = "Height must be between 1 and 48 inches")]
        [Display(Name = "Height (inches)", Description = "Box height in inches")]
        public decimal Height { get; set; }
        
        // Box Specifications
        [Required(ErrorMessage = "GSM is required")]
        [Range(80, 1000, ErrorMessage = "GSM must be between 80 and 1000")]
        [Display(Name = "GSM", Description = "Paper weight in grams per square meter")]
        public int GSM { get; set; }
        
        [Required(ErrorMessage = "Paper type is required")]
        [StringLength(50, ErrorMessage = "Paper type cannot be longer than 50 characters")]
        [Display(Name = "Paper Type", Description = "Type of corrugated paper")]
        public string PaperType { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Printing type is required")]
        [StringLength(50, ErrorMessage = "Printing type cannot be longer than 50 characters")]
        [Display(Name = "Printing Type", Description = "Type of printing process")]
        public string PrintingType { get; set; } = string.Empty;
        
        // Construction Type (Industry Standard)
        [StringLength(20, ErrorMessage = "Construction type cannot be longer than 20 characters")]
        [Display(Name = "Construction", Description = "Single-wall (3-ply) or Double-wall (5-ply)")]
        public string ConstructionType { get; set; } = "single-wall";
        
        // Pricing
        [Required(ErrorMessage = "Price per box is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price per box must be greater than 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Price per Box (Rs.)", Description = "Final price per box in Indian Rupees")]
        public decimal PricePerBox { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Default quantity must be at least 1")]
        [Display(Name = "Default Quantity", Description = "Standard order quantity")]
        public int DefaultQuantity { get; set; } = 1;
        
        // Metadata
        [StringLength(20, ErrorMessage = "Category cannot be longer than 20 characters")]
        [Display(Name = "Category", Description = "Product category")]
        public string Category { get; set; } = string.Empty; // e.g., "Standard", "Premium", "Special"
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? LastOrderDate { get; set; }
        
        public int TotalOrdersCount { get; set; } = 0;
        
        // Computed Properties
        [Display(Name = "Size")]
        public string SizeDisplay => $"{Length}\" × {Width}\" × {Height}\"";
        
        [Display(Name = "Dimensions")]
        public string DimensionsDisplayInches => $"L:{Length}\" W:{Width}\" H:{Height}\"";
        
        [Display(Name = "Box Area")]
        public decimal BoxAreaSquareInches => Length * Width;
        
        [Display(Name = "Box Volume")]
        public decimal BoxVolumeCubicInches => Length * Width * Height;
        
        [Display(Name = "Specifications")]
        public string SpecificationDisplay => $"{GSM} GSM, {PaperType}, {PrintingType}, {ConstructionType}";
        
        [Display(Name = "Full Description")]
        public string FullDescriptionInches => $"{ProductName} - {DimensionsDisplayInches} - {ConstructionType} - {GSM} GSM";
        
        // Industry Standard Properties
        [Display(Name = "Surface Area")]
        public decimal SurfaceAreaSquareInches => 2 * (Length * Width + Width * Height + Height * Length);
        
        [Display(Name = "Corrugated Area")]
        public decimal CorrugatedAreaSquareInches => Length * Width; // Base area for material calculation
        
        [Display(Name = "Perimeter")]
        public decimal PerimeterInches => 2 * (Length + Width);
    }
}