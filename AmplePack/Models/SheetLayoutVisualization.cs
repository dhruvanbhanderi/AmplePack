namespace AmplePack.Models
{
    /// <summary>
    /// Model for sheet layout visualization data
    /// </summary>
    public class SheetLayoutVisualization
    {
        // Sheet dimensions
        public decimal SheetLengthMm { get; set; }
        public decimal SheetWidthMm { get; set; }
        public decimal SheetLengthInches { get; set; }
        public decimal SheetWidthInches { get; set; }

        // Blank dimensions
        public decimal BlankLengthMm { get; set; }
        public decimal BlankWidthMm { get; set; }
        public decimal BlankLengthInches { get; set; }
        public decimal BlankWidthInches { get; set; }

        // Layout information
        public int BlanksPerRow { get; set; }
        public int BlanksPerColumn { get; set; }
        public int TotalBlanksPerSheet { get; set; }
        
        // Waste areas
        public decimal UnusedLengthMm { get; set; }
        public decimal UnusedWidthMm { get; set; }
        public decimal WastedAreaSqM { get; set; }
        public decimal UsedAreaSqM { get; set; }
        public decimal EfficiencyPercentage { get; set; }
        public decimal WastePercentage { get; set; }

        // Visualization coordinates (for front-end rendering)
        public List<BlankPosition> BlankPositions { get; set; } = new();
        public List<WasteArea> WasteAreas { get; set; } = new();

        // Layout description
        public string LayoutDescription { get; set; } = string.Empty;
        public bool IsOptimal { get; set; }
        public List<string> OptimizationSuggestions { get; set; } = new();
    }

    /// <summary>
    /// Position of a blank on the sheet
    /// </summary>
    public class BlankPosition
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public decimal StartXMm { get; set; }
        public decimal StartYMm { get; set; }
        public decimal EndXMm { get; set; }
        public decimal EndYMm { get; set; }
        public decimal WidthMm { get; set; }
        public decimal HeightMm { get; set; }
        
        // For CSS/JavaScript rendering (percentages)
        public decimal StartXPercent { get; set; }
        public decimal StartYPercent { get; set; }
        public decimal WidthPercent { get; set; }
        public decimal HeightPercent { get; set; }
    }

    /// <summary>
    /// Waste area on the sheet
    /// </summary>
    public class WasteArea
    {
        public string AreaType { get; set; } = string.Empty; // "RightMargin", "BottomMargin", "Corner"
        public decimal StartXMm { get; set; }
        public decimal StartYMm { get; set; }
        public decimal EndXMm { get; set; }
        public decimal EndYMm { get; set; }
        public decimal WidthMm { get; set; }
        public decimal HeightMm { get; set; }
        public decimal AreaSqM { get; set; }
        
        // For CSS/JavaScript rendering (percentages)
        public decimal StartXPercent { get; set; }
        public decimal StartYPercent { get; set; }
        public decimal WidthPercent { get; set; }
        public decimal HeightPercent { get; set; }
    }

    /// <summary>
    /// Request model for layout visualization API
    /// </summary>
    public class LayoutVisualizationRequest
    {
        /// <summary>
        /// Box length in inches
        /// </summary>
        public decimal Length { get; set; }

        /// <summary>
        /// Box width in inches
        /// </summary>
        public decimal Width { get; set; }

        /// <summary>
        /// Box height in inches
        /// </summary>
        public decimal Height { get; set; }

        /// <summary>
        /// Sheet length in inches
        /// </summary>
        public decimal SheetLength { get; set; }

        /// <summary>
        /// Sheet width in inches
        /// </summary>
        public decimal SheetWidth { get; set; }

        /// <summary>
        /// Board GSM (Grams per Square Meter)
        /// </summary>
        public decimal BoardGSM { get; set; } = 200;

        /// <summary>
        /// Compression ratio for blank calculation
        /// </summary>
        public decimal CompressionRatio { get; set; } = 1.32m;

        /// <summary>
        /// Quantity of boxes to produce
        /// </summary>
        public int Quantity { get; set; } = 1000;
    }
}