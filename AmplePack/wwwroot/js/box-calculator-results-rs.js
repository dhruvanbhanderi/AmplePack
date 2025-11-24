// Box Calculator Results Display Functions - CURRENCY SYMBOL: Rs.

function displayResults(result) {
    const html = `
        <div class="row">
            <div class="col-12">
                <!-- Main Price Card -->
                <div class="main-price-display">
                    <div class="main-price-value">Rs. ${result.finalPricePerBoxWithGST.toFixed(2)}</div>
                    <div class="main-price-label">Final Price Per Box (with GST)</div>
                </div>
            </div>
        </div>

        <!-- Metrics Grid -->
        <div class="metrics-grid">
            <div class="metric-card success">
                <div class="metric-value">Rs. ${result.totalOrderValueWithGST.toLocaleString('en-IN', {minimumFractionDigits: 2, maximumFractionDigits: 2})}</div>
                <div class="metric-label">Total Order Value</div>
            </div>
            <div class="metric-card info">
                <div class="metric-value">${result.sheetAnalysis.totalApps}</div>
                <div class="metric-label">Apps per Sheet</div>
            </div>
            <div class="metric-card warning">
                <div class="metric-value">${result.materialEfficiency.toFixed(1)}%</div>
                <div class="metric-label">Material Efficiency</div>
            </div>
            <div class="metric-card">
                <div class="metric-value">Rs. ${result.profitPerBox.toFixed(2)}</div>
                <div class="metric-label">Profit per Box</div>
            </div>
        </div>

        <!-- Sheet Analysis Card -->
        <div class="card">
            <div class="card-header breakdown-header">
                <i class="fas fa-ruler-combined mr-2"></i>Sheet Analysis
            </div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-6">
                        <p><strong>Sheet Size:</strong> ${result.sheetAnalysis.sheetLength}" × ${result.sheetAnalysis.sheetWidth}"</p>
                        <p><strong>Sheet Area:</strong> ${result.sheetAnalysis.sheetArea.toFixed(2)} sq in</p>
                        <p><strong>Box Layout:</strong> ${result.sheetAnalysis.boxLayoutLength.toFixed(1)}" × ${result.sheetAnalysis.boxLayoutWidth.toFixed(1)}"</p>
                    </div>
                    <div class="col-md-6">
                        <p><strong>Apps:</strong> ${result.sheetAnalysis.appsLength} × ${result.sheetAnalysis.appsWidth} = ${result.sheetAnalysis.totalApps}</p>
                        <p><strong>Utilization:</strong> ${result.sheetAnalysis.utilizationPercentage.toFixed(1)}%</p>
                        <p><strong>Waste:</strong> ${result.wastagePercentage.toFixed(1)}%</p>
                    </div>
                </div>
            </div>
        </div>

        <!-- Cost Breakdown Table - CURRENCY SYMBOL: Rs. -->
        <div class="card">
            <div class="card-header breakdown-header">
                <i class="fas fa-money-bill-wave mr-2"></i>Detailed Cost Breakdown
                <small class="float-right text-muted">Quantity: ${result.quantity.toLocaleString()} boxes</small>
            </div>
            <div class="card-body">
                <table class="breakdown-table table table-sm">
                    <thead>
                        <tr>
                            <th>Cost Component</th>
                            <th class="text-center">Unit</th>
                            <th class="text-right">Per Sheet</th>
                            <th class="text-right">Per Box</th>
                            <th class="text-right">Total (${result.quantity} boxes)</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr class="table-info">
                            <td colspan="5"><strong>MATERIAL COSTS</strong></td>
                        </tr>
                        <tr>
                            <td>Top Liner (Paper 1)</td>
                            <td class="text-center">Rs./kg</td>
                            <td class="text-right">Rs. ${result.sheetAnalysis.paper1CostPerSheet.toFixed(2)}</td>
                            <td class="text-right">Rs. ${result.costBreakdown.paper1CostPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.paper1CostPerBox * result.quantity)}</td>
                        </tr>
                        <tr>
                            <td>Bottom Liner (Paper 2)</td>
                            <td class="text-center">Rs./kg</td>
                            <td class="text-right">Rs. ${result.sheetAnalysis.paper2CostPerSheet.toFixed(2)}</td>
                            <td class="text-right">Rs. ${result.costBreakdown.paper2CostPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.paper2CostPerBox * result.quantity)}</td>
                        </tr>
                        <tr>
                            <td>Medium (Flute)</td>
                            <td class="text-center">Rs./kg</td>
                            <td class="text-right">Rs. ${result.sheetAnalysis.mediumCostPerSheet.toFixed(2)}</td>
                            <td class="text-right">Rs. ${result.costBreakdown.mediumCostPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.mediumCostPerBox * result.quantity)}</td>
                        </tr>
                        <tr class="table-active">
                            <td><strong>Subtotal - Material</strong></td>
                            <td class="text-center">-</td>
                            <td class="text-right"><strong>Rs. ${result.sheetAnalysis.totalMaterialCostPerSheet.toFixed(2)}</strong></td>
                            <td class="text-right"><strong>Rs. ${result.costBreakdown.totalMaterialCostPerBox.toFixed(2)}</strong></td>
                            <td class="text-right"><strong>Rs. ${formatIndianCurrency(result.costBreakdown.totalMaterialCostPerBox * result.quantity)}</strong></td>
                        </tr>
                        <tr>
                            <td>Wastage (${((result.costBreakdown.wastageCost / result.costBreakdown.totalMaterialCostPerBox) * 100).toFixed(1)}%)</td>
                            <td class="text-center">-</td>
                            <td class="text-right">-</td>
                            <td class="text-right">Rs. ${result.costBreakdown.wastageCost.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.wastageCost * result.quantity)}</td>
                        </tr>
                        
                        <tr class="table-info">
                            <td colspan="5"><strong>PROCESSING COSTS</strong></td>
                        </tr>
                        <tr>
                            <td>Printing</td>
                            <td class="text-center">Rs./sheet</td>
                            <td class="text-right">Rs. ${(result.costBreakdown.printingCostPerBox * result.sheetAnalysis.totalApps).toFixed(2)}</td>
                            <td class="text-right">Rs. ${result.costBreakdown.printingCostPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.printingCostPerBox * result.quantity)}</td>
                        </tr>
                        <tr>
                            <td>Die Cutting</td>
                            <td class="text-center">Rs./sheet</td>
                            <td class="text-right">Rs. ${(result.costBreakdown.dieCuttingCostPerBox * result.sheetAnalysis.totalApps).toFixed(2)}</td>
                            <td class="text-right">Rs. ${result.costBreakdown.dieCuttingCostPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.dieCuttingCostPerBox * result.quantity)}</td>
                        </tr>
                        <tr>
                            <td>Labor</td>
                            <td class="text-center">Rs./box</td>
                            <td class="text-right">-</td>
                            <td class="text-right">Rs. ${result.costBreakdown.laborCostPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.laborCostPerBox * result.quantity)}</td>
                        </tr>
                        <tr>
                            <td>Pin Cost</td>
                            <td class="text-center">Rs./box</td>
                            <td class="text-right">-</td>
                            <td class="text-right">Rs. ${result.costBreakdown.pinCostPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.pinCostPerBox * result.quantity)}</td>
                        </tr>
                        <tr>
                            <td>Transport</td>
                            <td class="text-center">Rs./box</td>
                            <td class="text-right">-</td>
                            <td class="text-right">Rs. ${result.costBreakdown.transportCostPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.transportCostPerBox * result.quantity)}</td>
                        </tr>
                        
                        <tr class="table-active">
                            <td><strong>Subtotal - Before Overhead</strong></td>
                            <td class="text-center">-</td>
                            <td class="text-right">-</td>
                            <td class="text-right"><strong>Rs. ${result.costBreakdown.subtotalPerBox.toFixed(2)}</strong></td>
                            <td class="text-right"><strong>Rs. ${formatIndianCurrency(result.costBreakdown.subtotalPerBox * result.quantity)}</strong></td>
                        </tr>
                        
                        <tr class="table-info">
                            <td colspan="5"><strong>BUSINESS COSTS</strong></td>
                        </tr>
                        <tr>
                            <td>Overhead (% of subtotal)</td>
                            <td class="text-center">%</td>
                            <td class="text-right">-</td>
                            <td class="text-right">Rs. ${result.costBreakdown.overheadCostPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.overheadCostPerBox * result.quantity)}</td>
                        </tr>
                        <tr class="table-active">
                            <td><strong>Total Cost</strong></td>
                            <td class="text-center">-</td>
                            <td class="text-right">-</td>
                            <td class="text-right"><strong>Rs. ${result.costBreakdown.totalCostPerBox.toFixed(2)}</strong></td>
                            <td class="text-right"><strong>Rs. ${formatIndianCurrency(result.costBreakdown.totalCostPerBox * result.quantity)}</strong></td>
                        </tr>
                        <tr>
                            <td>Profit Margin (% of cost)</td>
                            <td class="text-center">%</td>
                            <td class="text-right">-</td>
                            <td class="text-right">Rs. ${result.costBreakdown.profitPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.profitPerBox * result.quantity)}</td>
                        </tr>
                        <tr class="table-success">
                            <td><strong>Selling Price (Before GST)</strong></td>
                            <td class="text-center">-</td>
                            <td class="text-right">-</td>
                            <td class="text-right"><strong>Rs. ${result.costBreakdown.sellingPricePerBox.toFixed(2)}</strong></td>
                            <td class="text-right"><strong>Rs. ${formatIndianCurrency(result.costBreakdown.sellingPricePerBox * result.quantity)}</strong></td>
                        </tr>
                        <tr>
                            <td>GST (18%)</td>
                            <td class="text-center">%</td>
                            <td class="text-right">-</td>
                            <td class="text-right">Rs. ${result.costBreakdown.gstAmountPerBox.toFixed(2)}</td>
                            <td class="text-right">Rs. ${formatIndianCurrency(result.costBreakdown.gstAmountPerBox * result.quantity)}</td>
                        </tr>
                        <tr class="table-primary">
                            <td><strong>FINAL PRICE (With GST)</strong></td>
                            <td class="text-center">-</td>
                            <td class="text-right">-</td>
                            <td class="text-right"><strong>Rs. ${result.finalPricePerBoxWithGST.toFixed(2)}</strong></td>
                            <td class="text-right"><strong>Rs. ${formatIndianCurrency(result.totalOrderValueWithGST)}</strong></td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- Summary Card -->
        <div class="card">
            <div class="card-header breakdown-header bg-success text-white">
                <i class="fas fa-check-circle mr-2"></i>Order Summary
            </div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-4">
                        <h5>Box Specifications</h5>
                        <p><strong>Dimensions:</strong> ${result.length}" × ${result.width}" × ${result.height}"</p>
                        <p><strong>Board Type:</strong> ${result.boardType}</p>
                        <p><strong>Quantity:</strong> ${result.quantity.toLocaleString()} boxes</p>
                    </div>
                    <div class="col-md-4">
                        <h5>Production Details</h5>
                        <p><strong>Sheets Required:</strong> ${Math.ceil(result.quantity / result.sheetAnalysis.totalApps).toLocaleString()}</p>
                        <p><strong>Apps per Sheet:</strong> ${result.sheetAnalysis.totalApps}</p>
                        <p><strong>Efficiency:</strong> ${result.materialEfficiency.toFixed(1)}%</p>
                    </div>
                    <div class="col-md-4">
                        <h5>Financial Summary</h5>
                        <p><strong>Cost per Box:</strong> Rs. ${result.costBreakdown.totalCostPerBox.toFixed(2)}</p>
                        <p><strong>Profit per Box:</strong> Rs. ${result.profitPerBox.toFixed(2)}</p>
                        <p><strong>Total Order Value:</strong> Rs. ${formatIndianCurrency(result.totalOrderValueWithGST)}</p>
                    </div>
                </div>
            </div>
        </div>

        <!-- Action Buttons -->
        <div class="text-center mt-4 mb-4">
            <button class="btn btn-success btn-lg" onclick="window.print()">
                <i class="fas fa-print mr-2"></i>Print Results
            </button>
            <button class="btn btn-primary btn-lg ml-2" onclick="exportToPDF()">
                <i class="fas fa-file-pdf mr-2"></i>Export to PDF
            </button>
            <button class="btn btn-secondary btn-lg ml-2" onclick="$('#resultsSection').removeClass('show'); $('html, body').animate({scrollTop: 0}, 500);">
                <i class="fas fa-calculator mr-2"></i>New Calculation
            </button>
        </div>
    `;
    
    $('#resultsSection').html(html);
}

// Indian currency formatting helper function
function formatIndianCurrency(amount) {
    return amount.toLocaleString('en-IN', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

function exportToPDF() {
    // Future implementation: Export to PDF
    if (typeof toastr !== 'undefined') {
        toastr.info('PDF export feature coming soon!');
    } else {
        alert('PDF export feature coming soon!');
    }
}

// Validation error display
function displayValidationErrors(errors) {
    let errorHtml = '<div class="alert alert-danger alert-dismissible fade show" role="alert">';
    errorHtml += '<h5><i class="fas fa-exclamation-triangle mr-2"></i>Validation Errors</h5>';
    errorHtml += '<ul class="mb-0">';
    
    if (Array.isArray(errors)) {
        errors.forEach(error => {
            if (error.errors && error.errors.length > 0) {
                error.errors.forEach(err => {
                    errorHtml += `<li><strong>${error.field}:</strong> ${err}</li>`;
                });
            }
        });
    } else if (typeof errors === 'string') {
        errorHtml += `<li>${errors}</li>`;
    }
    
    errorHtml += '</ul>';
    errorHtml += '<button type="button" class="close" data-dismiss="alert" aria-label="Close">';
    errorHtml += '<span aria-hidden="true">&times;</span>';
    errorHtml += '</button>';
    errorHtml += '</div>';
    
    // Display at top of form
    $('.container-fluid').prepend(errorHtml);
    
    // Scroll to error
    $('html, body').animate({
        scrollTop: 0
    }, 500);
}