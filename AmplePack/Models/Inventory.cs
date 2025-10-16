using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class Inventory
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(100, ErrorMessage = "Item name cannot be longer than 100 characters")]
        [Display(Name = "Item Name")]
        public string ItemName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Available quantity is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Available quantity cannot be negative")]
        [Display(Name = "Available Quantity")]
        public decimal AvailableQuantity { get; set; }
        
        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Unit Price (Rs.)")]
        public decimal UnitPrice { get; set; }
        
        [Required(ErrorMessage = "Unit is required")]
        [StringLength(20, ErrorMessage = "Unit cannot be longer than 20 characters")]
        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; } = string.Empty;
        
        [StringLength(50, ErrorMessage = "Category cannot be longer than 50 characters")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Reorder level is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Reorder level cannot be negative")]
        [Display(Name = "Reorder Level")]
        public decimal ReorderLevel { get; set; } = 10;
        
        // Material Specifications (for corrugated materials)
        [Range(0, 1000, ErrorMessage = "GSM must be between 0 and 1000")]
        [Display(Name = "GSM", Description = "Grams per square meter (for paper materials)")]
        public int? GSM { get; set; }
        
        [StringLength(50)]
        [Display(Name = "Material Type", Description = "Type of corrugated material")]
        public string MaterialType { get; set; } = string.Empty; // e.g., "Top Paper", "Liner", "Corrugated Medium"
        
        [StringLength(30)]
        [Display(Name = "Grade", Description = "Material grade or quality")]
        public string Grade { get; set; } = string.Empty;
        
        // Sheet Dimensions (for sheet materials in inches)
        [Range(0, 100, ErrorMessage = "Sheet length must be between 0 and 100 inches")]
        [Display(Name = "Sheet Length (inches)", Description = "Standard sheet length in inches")]
        public decimal? SheetLengthInches { get; set; }
        
        [Range(0, 100, ErrorMessage = "Sheet width must be between 0 and 100 inches")]
        [Display(Name = "Sheet Width (inches)", Description = "Standard sheet width in inches")]
        public decimal? SheetWidthInches { get; set; }
        
        // Computed properties
        [Display(Name = "Low Stock")]
        public bool IsLowStock => AvailableQuantity <= ReorderLevel;
        
        [Display(Name = "Total Value")]
        public decimal TotalValue => AvailableQuantity * UnitPrice;
        
        [Display(Name = "Sheet Area")]
        public decimal? SheetAreaSquareInches => SheetLengthInches.HasValue && SheetWidthInches.HasValue 
            ? SheetLengthInches * SheetWidthInches : null;
        
        [Display(Name = "Sheet Size")]
        public string SheetSizeDisplay => SheetLengthInches.HasValue && SheetWidthInches.HasValue 
            ? $"{SheetLengthInches}\" × {SheetWidthInches}\"" : "N/A";
        
        [Display(Name = "Full Description")]
        public string FullDescription => !string.IsNullOrEmpty(MaterialType) && GSM.HasValue
            ? $"{ItemName} - {MaterialType} - {GSM} GSM"
            : ItemName;
        
        [Display(Name = "Per Unit Cost")]
        public string PerUnitDisplay => $"Rs. {UnitPrice:F2} per {Unit}";
        
        [Display(Name = "Stock Status")]
        public string StockStatusDisplay => IsLowStock 
            ? $"Low Stock ({AvailableQuantity:F1} {Unit})" 
            : $"In Stock ({AvailableQuantity:F1} {Unit})";
        
        // Industry Standard Categories
        public static class InventoryCategories
        {
            public const string TopPaper = "Top Paper";
            public const string CorrugatedMedium = "Corrugated Medium";
            public const string Liner = "Liner";
            public const string Adhesive = "Adhesive";
            public const string Ink = "Printing Ink";
            public const string Coating = "Coating Material";
            public const string PackagingSupplies = "Packaging Supplies";
            public const string Tools = "Tools & Equipment";
            public const string Other = "Other";
        }
        
        // Industry Standard Units
        public static class InventoryUnits
        {
            public const string Kilograms = "kg";
            public const string Sheets = "sheets";
            public const string SquareMeters = "sq.m";
            public const string SquareInches = "sq.in";
            public const string Liters = "liters";
            public const string Pieces = "pieces";
            public const string Rolls = "rolls";
        }
        
        // For backward compatibility - simplified
        [Display(Name = "Quantity")]
        public decimal Quantity 
        { 
            get => AvailableQuantity; 
            set => AvailableQuantity = value; 
        }
    }
}