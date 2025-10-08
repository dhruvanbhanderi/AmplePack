/**
 * Simple Box Layout Visualizer
 * Renders basic sheet layout visualization without zoom/pan controls
 * Shows clear symbols and color-coded summary
 */

class BoxLayoutVisualizer {
    constructor(containerId, options = {}) {
        console.log('Creating BoxLayoutVisualizer for container:', containerId);
        
        this.container = document.getElementById(containerId);
        if (!this.container) {
            console.error('Container not found:', containerId);
            throw new Error(`Container element '${containerId}' not found`);
        }
        
        this.options = {
            width: options.width || 800,
            height: options.height || 400,
            padding: options.padding || 30,
            colors: {
                sheet: '#f8f9fa',
                sheetBorder: '#6c757d',
                blank: '#007bff',
                blankBorder: '#0056b3',
                waste: '#dc3545',
                wasteBorder: '#c82333',
                text: '#495057'
            },
            ...options
        };
        
        this.visualization = null;
        this.initializeContainer();
        console.log('BoxLayoutVisualizer created successfully');
    }

    initializeContainer() {
        this.container.innerHTML = '';
        this.container.className = 'simple-layout-visualizer';
        this.container.style.cssText = `
            display: flex;
            flex-direction: row;
            border: 1px solid #ddd;
            border-radius: 4px;
            background: white;
            overflow: hidden;
            width: 100%;
            height: 450px;
            margin: 10px 0;
        `;
        
        // Create canvas container
        this.canvasContainer = document.createElement('div');
        this.canvasContainer.style.cssText = `
            flex: 2;
            position: relative;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
            background: #fafafa;
        `;
        
        // Create canvas
        this.canvas = document.createElement('canvas');
        this.canvas.width = 480;
        this.canvas.height = 320;
        this.canvas.style.cssText = `
            max-width: 100%;
            max-height: 100%;
            border: 1px solid #ccc;
            background: white;
            border-radius: 3px;
        `;
        this.ctx = this.canvas.getContext('2d');
        
        // Create summary panel
        this.createSummaryPanel();
        
        this.canvasContainer.appendChild(this.canvas);
        this.container.appendChild(this.canvasContainer);
        this.container.appendChild(this.summaryPanel);
    }

    createSummaryPanel() {
        this.summaryPanel = document.createElement('div');
        this.summaryPanel.className = 'layout-summary';
        this.summaryPanel.style.cssText = `
            flex: 1;
            min-width: 250px;
            max-width: 300px;
            background: #f8f9fa;
            border-left: 1px solid #ddd;
            overflow-y: auto;
        `;
    }

    updateVisualization(visualizationData) {
        this.visualization = visualizationData;
        this.render();
        this.updateSummaryPanel();
    }

    render() {
        if (!this.visualization) {
            this.renderPlaceholder();
            return;
        }

        console.log('Rendering visualization with data:', this.visualization);

        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

        // Calculate rendering dimensions with proper margins for dimensions
        const topMargin = 35;     // Space for top dimension
        const bottomMargin = 25;  // Bottom clearance
        const leftMargin = 30;    // Left clearance
        const rightMargin = 70;   // Space for right dimension
        
        const availableWidth = this.canvas.width - leftMargin - rightMargin;
        const availableHeight = this.canvas.height - topMargin - bottomMargin;
        
        const sheetRatio = this.visualization.sheetLengthMm / this.visualization.sheetWidthMm;
        
        let sheetWidth, sheetHeight;
        if (sheetRatio > availableWidth / availableHeight) {
            sheetWidth = availableWidth;
            sheetHeight = availableWidth / sheetRatio;
        } else {
            sheetHeight = availableHeight;
            sheetWidth = availableHeight * sheetRatio;
        }

        // Center the sheet within the available area
        const sheetX = leftMargin + (availableWidth - sheetWidth) / 2;
        const sheetY = topMargin + (availableHeight - sheetHeight) / 2;

        // Draw diagram elements
        this.drawSheet(sheetX, sheetY, sheetWidth, sheetHeight);
        this.drawWasteAreas(sheetX, sheetY, sheetWidth, sheetHeight);
        this.drawBlanks(sheetX, sheetY, sheetWidth, sheetHeight);
        this.drawCleanDimensions(sheetX, sheetY, sheetWidth, sheetHeight);
    }

    drawSheet(x, y, width, height) {
        // Clean sheet background
        this.ctx.fillStyle = '#ffffff';
        this.ctx.fillRect(x, y, width, height);

        // Simple sheet border
        this.ctx.strokeStyle = '#8e8e8e';
        this.ctx.lineWidth = 2;
        this.ctx.strokeRect(x, y, width, height);
    }

    drawWasteAreas(sheetX, sheetY, sheetWidth, sheetHeight) {
        if (!this.visualization.wasteAreas || this.visualization.wasteAreas.length === 0) {
            return;
        }
        
        this.visualization.wasteAreas.forEach(waste => {
            const startXPercent = Math.max(0, Math.min(100, waste.startXPercent));
            const startYPercent = Math.max(0, Math.min(100, waste.startYPercent));
            const widthPercent = Math.max(0, Math.min(100 - startXPercent, waste.widthPercent));
            const heightPercent = Math.max(0, Math.min(100 - startYPercent, waste.heightPercent));
            
            const x = sheetX + (startXPercent / 100) * sheetWidth;
            const y = sheetY + (startYPercent / 100) * sheetHeight;
            const w = (widthPercent / 100) * sheetWidth;
            const h = (heightPercent / 100) * sheetHeight;

            if (x >= sheetX && y >= sheetY && 
                (x + w) <= (sheetX + sheetWidth) && 
                (y + h) <= (sheetY + sheetHeight) &&
                w > 1 && h > 1) {
                
                // Simple waste fill
                this.ctx.fillStyle = '#ffe6e6';
                this.ctx.fillRect(x, y, w, h);
                
                // Simple border
                this.ctx.strokeStyle = '#e57373';
                this.ctx.lineWidth = 1;
                this.ctx.strokeRect(x, y, w, h);

                // Clean diagonal pattern
                this.ctx.strokeStyle = '#e57373';
                this.ctx.lineWidth = 0.8;
                this.ctx.setLineDash([3, 3]);
                
                const step = 10;
                for (let i = 0; i < w + h; i += step) {
                    this.ctx.beginPath();
                    this.ctx.moveTo(x + Math.min(i, w), y);
                    this.ctx.lineTo(x, y + Math.min(i, h));
                    this.ctx.stroke();
                }
                
                this.ctx.setLineDash([]);
            }
        });
    }

    drawBlanks(sheetX, sheetY, sheetWidth, sheetHeight) {
        if (!this.visualization.blankPositions || this.visualization.blankPositions.length === 0) {
            return;
        }
        
        this.visualization.blankPositions.forEach((blank, index) => {
            const startXPercent = Math.max(0, Math.min(100, blank.startXPercent));
            const startYPercent = Math.max(0, Math.min(100, blank.startYPercent));
            const widthPercent = Math.max(0, Math.min(100 - startXPercent, blank.widthPercent));
            const heightPercent = Math.max(0, Math.min(100 - startYPercent, blank.heightPercent));
            
            const x = sheetX + (startXPercent / 100) * sheetWidth;
            const y = sheetY + (startYPercent / 100) * sheetHeight;
            const w = (widthPercent / 100) * sheetWidth;
            const h = (heightPercent / 100) * sheetHeight;

            if (x >= sheetX && y >= sheetY && 
                (x + w) <= (sheetX + sheetWidth) && 
                (y + h) <= (sheetY + sheetHeight) &&
                w > 5 && h > 5) {

                // Clean box background
                this.ctx.fillStyle = '#e8f4fd';
                this.ctx.fillRect(x, y, w, h);

                // Clean border
                this.ctx.strokeStyle = '#1976d2';
                this.ctx.lineWidth = 1.5;
                this.ctx.strokeRect(x, y, w, h);

                // Simple number centered
                if (w > 15 && h > 12) {
                    this.ctx.fillStyle = '#1976d2';
                    this.ctx.font = 'bold 10px Arial';
                    this.ctx.textAlign = 'center';
                    this.ctx.textBaseline = 'middle';
                    this.ctx.fillText((index + 1).toString(), x + w/2, y + h/2);
                }
            }
        });
    }

    drawCleanDimensions(sheetX, sheetY, sheetWidth, sheetHeight) {
        this.ctx.strokeStyle = '#555';
        this.ctx.fillStyle = '#555';
        this.ctx.lineWidth = 1;
        this.ctx.font = '10px Arial';
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';
        
        // Top dimension (width) - clean and positioned properly
        const topY = sheetY - 20;
        
        // Main dimension line
        this.ctx.beginPath();
        this.ctx.moveTo(sheetX, topY);
        this.ctx.lineTo(sheetX + sheetWidth, topY);
        this.ctx.stroke();
        
        // End marks
        this.ctx.beginPath();
        this.ctx.moveTo(sheetX, topY - 4);
        this.ctx.lineTo(sheetX, topY + 4);
        this.ctx.moveTo(sheetX + sheetWidth, topY - 4);
        this.ctx.lineTo(sheetX + sheetWidth, topY + 4);
        this.ctx.stroke();
        
        // Width text - using simple double quote character
        this.ctx.fillText(
            `${this.visualization.sheetLengthInches.toFixed(1)}"`,
            sheetX + sheetWidth/2,
            topY - 8
        );
        
        // Right dimension (height) - clean and positioned properly
        const rightX = sheetX + sheetWidth + 20;
        
        // Main dimension line
        this.ctx.beginPath();
        this.ctx.moveTo(rightX, sheetY);
        this.ctx.lineTo(rightX, sheetY + sheetHeight);
        this.ctx.stroke();
        
        // End marks
        this.ctx.beginPath();
        this.ctx.moveTo(rightX - 4, sheetY);
        this.ctx.lineTo(rightX + 4, sheetY);
        this.ctx.moveTo(rightX - 4, sheetY + sheetHeight);
        this.ctx.lineTo(rightX + 4, sheetY + sheetHeight);
        this.ctx.stroke();
        
        // Height text (rotated) - using simple double quote character
        this.ctx.save();
        this.ctx.translate(rightX + 12, sheetY + sheetHeight/2);
        this.ctx.rotate(-Math.PI/2);
        this.ctx.fillText(
            `${this.visualization.sheetWidthInches.toFixed(1)}"`,
            0, 0
        );
        this.ctx.restore();
    }

    updateSummaryPanel() {
        if (!this.visualization) {
            this.summaryPanel.innerHTML = `
                <div style="padding: 20px; text-align: center; color: #6c757d;">
                    <div style="width: 40px; height: 40px; background: #dee2e6; border-radius: 8px; margin: 0 auto 15px; display: flex; align-items: center; justify-content: center;">
                        <span style="color: #6c757d; font-size: 20px; font-weight: bold;">??</span>
                    </div>
                    <h5 style="color: #495057; margin-bottom: 10px;">Layout Analysis</h5>
                    <p style="font-size: 14px; margin-bottom: 20px;">Enter box and sheet dimensions to see layout analysis</p>
                    <div style="background: #e9ecef; padding: 15px; border-radius: 6px; text-align: left;">
                        <div style="font-weight: bold; margin-bottom: 8px; color: #495057;">Required:</div>
                        <div style="font-size: 12px; line-height: 1.4;">
                            <div style="margin-bottom: 4px;">• Box dimensions</div>
                            <div style="margin-bottom: 4px;">• Sheet size</div>
                            <div>• Board specifications</div>
                        </div>
                    </div>
                </div>
            `;
            return;
        }

        const efficiency = this.visualization.efficiencyPercentage;
        let efficiencyColor = '#f44336';
        let efficiencyStatus = 'Low';
        
        if (efficiency >= 80) {
            efficiencyColor = '#4caf50';
            efficiencyStatus = 'Excellent';
        } else if (efficiency >= 70) {
            efficiencyColor = '#8bc34a';
            efficiencyStatus = 'Good';
        } else if (efficiency >= 60) {
            efficiencyColor = '#ff9800';
            efficiencyStatus = 'Fair';
        }

        this.summaryPanel.innerHTML = `
            <div style="padding: 15px; font-family: Arial, sans-serif; font-size: 13px;">
                <!-- Efficiency -->
                <div style="text-align: center; margin-bottom: 20px; padding: 15px; background: ${efficiencyColor}15; border-radius: 8px; border-left: 4px solid ${efficiencyColor};">
                    <div style="font-size: 28px; font-weight: bold; color: ${efficiencyColor}; margin-bottom: 5px;">${efficiency.toFixed(1)}%</div>
                    <div style="font-size: 12px; color: #666; font-weight: 500;">${efficiencyStatus} Efficiency</div>
                </div>

                <!-- Layout Info -->
                <div style="margin-bottom: 18px;">
                    <div style="font-weight: bold; margin-bottom: 10px; color: #333; border-bottom: 1px solid #eee; padding-bottom: 5px;">Layout</div>
                    <div style="color: #666; line-height: 1.5;">
                        <div style="margin-bottom: 5px; display: flex; justify-content: space-between;">
                            <span>Boxes per sheet:</span>
                            <strong style="color: #333;">${this.visualization.totalBlanksPerSheet}</strong>
                        </div>
                        <div style="display: flex; justify-content: space-between;">
                            <span>Arrangement:</span>
                            <strong style="color: #333;">${this.visualization.blanksPerRow} × ${this.visualization.blanksPerColumn}</strong>
                        </div>
                    </div>
                </div>

                <!-- Dimensions -->
                <div style="margin-bottom: 18px;">
                    <div style="font-weight: bold; margin-bottom: 10px; color: #333; border-bottom: 1px solid #eee; padding-bottom: 5px;">Dimensions</div>
                    <div style="color: #666; line-height: 1.5; font-size: 12px;">
                        <div style="margin-bottom: 5px;">
                            <div style="color: #888; font-size: 11px;">Sheet Size:</div>
                            <strong style="color: #333;">${this.visualization.sheetLengthInches.toFixed(1)}" × ${this.visualization.sheetWidthInches.toFixed(1)}"</strong>
                        </div>
                        <div>
                            <div style="color: #888; font-size: 11px;">Box Size:</div>
                            <strong style="color: #333;">${this.visualization.blankLengthInches.toFixed(1)}" × ${this.visualization.blankWidthInches.toFixed(1)}"</strong>
                        </div>
                    </div>
                </div>

                <!-- Waste -->
                <div>
                    <div style="font-weight: bold; margin-bottom: 10px; color: #333; border-bottom: 1px solid #eee; padding-bottom: 5px;">Waste Area</div>
                    <div style="display: flex; justify-content: space-between; align-items: center;">
                        <span style="color: #666;">Waste area:</span>
                        <div style="font-weight: bold; color: #f44336; background: #ffebee; padding: 3px 8px; border-radius: 4px; font-size: 12px;">
                            ${this.visualization.wastePercentage.toFixed(1)}%
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderPlaceholder() {
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        this.ctx.fillStyle = '#f8f9fa';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        
        this.ctx.fillStyle = '#9e9e9e';
        this.ctx.font = '14px Arial';
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';
        this.ctx.fillText(
            'Enter dimensions to view layout',
            this.canvas.width / 2,
            this.canvas.height / 2
        );
        
        this.summaryPanel.innerHTML = `
            <div style="padding: 20px; text-align: center; color: #6c757d;">
                <div style="width: 40px; height: 40px; background: #dee2e6; border-radius: 8px; margin: 0 auto 15px; display: flex; align-items: center; justify-content: center;">
                    <span style="color: #6c757d; font-size: 20px; font-weight: bold;">??</span>
                </div>
                <h5 style="color: #495057; margin-bottom: 10px;">Layout Analysis</h5>
                <p style="font-size: 14px; margin-bottom: 20px;">Enter box and sheet dimensions to see layout analysis</p>
                <div style="background: #e9ecef; padding: 15px; border-radius: 6px; text-align: left;">
                    <div style="font-weight: bold; margin-bottom: 8px; color: #495057;">Required:</div>
                    <div style="font-size: 12px; line-height: 1.4;">
                        <div style="margin-bottom: 4px;">• Box dimensions</div>
                        <div style="margin-bottom: 4px;">• Sheet size</div>
                        <div>• Board specifications</div>
                    </div>
                </div>
            </div>
        `;
    }

    exportAsImage() {
        return this.canvas.toDataURL('image/png');
    }

    printVisualization() {
        const imageData = this.exportAsImage();
        const newWindow = window.open();
        newWindow.document.write(`
            <html>
                <head><title>Box Layout Visualization</title></head>
                <body style="margin: 0; text-align: center;">
                    <h2>Box Layout Analysis</h2>
                    <img src="${imageData}" style="max-width: 100%; height: auto;" />
                    <div style="margin: 20px; font-family: Arial, sans-serif;">
                        <h3>Layout Details</h3>
                        <p>${this.visualization?.layoutDescription || 'No layout data'}</p>
                        <p>Efficiency: ${this.visualization?.efficiencyPercentage?.toFixed(1) || 0}%</p>
                    </div>
                </body>
            </html>
        `);
        newWindow.document.close();
        newWindow.print();
    }
}

// Export for use - ensure it's available globally
window.BoxLayoutVisualizer = BoxLayoutVisualizer;

// Also try to export if in a module environment
if (typeof module !== 'undefined' && module.exports) {
    module.exports = BoxLayoutVisualizer;
}

console.log('BoxLayoutVisualizer class loaded and exported to window');