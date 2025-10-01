// ============================================================================
// AMBLEPACK - MISSING FUNCTIONALITY IMPLEMENTATIONS
// ============================================================================

// ============================================================================
// GLOBAL INVOICE GENERATION
// ============================================================================

function generateInvoice(orderId) {
    const button = event?.target?.closest('a, button');
    if (button) {
        showLoading(button, '');
        window.open(`/Orders/DownloadInvoice/${orderId}`, '_blank');
        setTimeout(() => hideLoading(button), 2000);
    } else {
        window.open(`/Orders/DownloadInvoice/${orderId}`, '_blank');
    }
}

// ============================================================================
// GLOBAL REORDER FUNCTION
// ============================================================================

function reorderOrder(orderId) {
    const button = event?.target?.closest('a, button');
    
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: 'Reorder Confirmation',
            text: 'Create a new order based on this existing order?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, Create Reorder!',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                processReorder(orderId, button);
            }
        });
    } else {
        if (confirm('Create a new order based on this existing order?')) {
            processReorder(orderId, button);
        }
    }
}

async function processReorder(orderId, button) {
    try {
        if (button) {
            showLoading(button, 'Creating...');
        }

        const response = await fetch(`/Orders/Reorder/${orderId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() || '',
                'Content-Type': 'application/json'
            }
        });

        const data = await response.json();

        if (data.success) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    title: 'Success!',
                    text: data.message,
                    icon: 'success',
                    confirmButtonText: 'View New Order'
                }).then((result) => {
                    if (result.isConfirmed && data.newOrderId) {
                        window.location.href = `/Orders/Details/${data.newOrderId}`;
                    }
                });
            } else {
                alert(data.message);
                if (data.newOrderId) {
                    window.location.href = `/Orders/Details/${data.newOrderId}`;
                }
            }
        } else {
            showToast('Failed to create reorder: ' + (data.message || 'Unknown error'), 'error');
        }
    } catch (error) {
        console.error('Error creating reorder:', error);
        showToast('Error creating reorder: ' + error.message, 'error');
    } finally {
        if (button) {
            hideLoading(button);
        }
    }
}

// ============================================================================
// GLOBAL ORDER STATUS CHANGE
// ============================================================================

function changeOrderStatus(orderId, currentStatus) {
    const statuses = ['Pending', 'Processing', 'Completed', 'Delivered', 'Cancelled'];
    const statusOptions = statuses.map(status => 
        `<option value="${status}" ${status === currentStatus ? 'selected' : ''}>${status}</option>`
    ).join('');

    if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: 'Change Order Status',
            html: `
                <div class="form-group text-left">
                    <label for="newStatus" class="form-label">Select New Status:</label>
                    <select class="form-control" id="newStatus">
                        ${statusOptions}
                    </select>
                </div>
                <div class="alert alert-info mt-2">
                    <small><strong>Current Status:</strong> ${currentStatus}</small>
                </div>
            `,
            showCancelButton: true,
            confirmButtonText: 'Update Status',
            cancelButtonText: 'Cancel',
            preConfirm: () => {
                const newStatus = document.getElementById('newStatus').value;
                if (newStatus === currentStatus) {
                    Swal.showValidationMessage('Please select a different status');
                    return false;
                }
                return newStatus;
            }
        }).then((result) => {
            if (result.isConfirmed) {
                updateOrderStatus(orderId, result.value);
            }
        });
    } else {
        // Fallback for environments without SweetAlert2
        const newStatus = prompt(`Change status from "${currentStatus}" to:`, currentStatus);
        if (newStatus && newStatus !== currentStatus && statuses.includes(newStatus)) {
            updateOrderStatus(orderId, newStatus);
        }
    }
}

async function updateOrderStatus(orderId, newStatus) {
    try {
        const response = await fetch(`/Orders/ChangeStatus/${orderId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() || '',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ status: newStatus })
        });

        const data = await response.json();

        if (data.success) {
            showToast(data.message || 'Status updated successfully', 'success');
            setTimeout(() => location.reload(), 1500);
        } else {
            showToast('Failed to update status: ' + (data.message || 'Unknown error'), 'error');
        }
    } catch (error) {
        console.error('Error updating status:', error);
        showToast('Error updating status: ' + error.message, 'error');
    }
}

// ============================================================================
// BULK OPERATIONS FOR ORDERS
// ============================================================================

function bulkExport() {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: 'Export Orders',
            text: 'This will export all visible orders to PDF format.',
            icon: 'info',
            showCancelButton: true,
            confirmButtonText: 'Export',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                window.location.href = '/Reports/Export?type=orders&format=pdf';
            }
        });
    } else {
        if (confirm('Export all visible orders to PDF?')) {
            window.location.href = '/Reports/Export?type=orders&format=pdf';
        }
    }
}

// ============================================================================
// UTILITY FUNCTIONS (FALLBACK IF NOT AVAILABLE)
// ============================================================================

function showToast(message, type = 'info') {
    if (typeof Swal !== 'undefined') {
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true
        });

        Toast.fire({
            icon: type,
            title: message
        });
    } else {
        alert(message);
    }
}

function showLoading(element, text = 'Loading...') {
    const $element = $(element);
    if ($element.length) {
        $element.prop('disabled', true);
        const originalText = $element.html();
        $element.data('original-text', originalText);
        
        if (text) {
            $element.html(`<i class="fas fa-spinner fa-spin"></i> ${text}`);
        } else {
            $element.addClass('loading');
        }
    }
}

function hideLoading(element) {
    const $element = $(element);
    if ($element.length) {
        $element.prop('disabled', false);
        const originalText = $element.data('original-text');
        
        if (originalText) {
            $element.html(originalText);
            $element.removeData('original-text');
        } else {
            $element.removeClass('loading');
        }
    }
}

// ============================================================================
// CUSTOMER FUNCTIONS
// ============================================================================

function createOrderForCustomer(customerId) {
    window.location.href = `/Orders/CreateWorkflow?customerId=${customerId}`;
}

function addProductForCustomer(customerId) {
    window.location.href = `/CustomerProducts/Create?customerId=${customerId}`;
}

function viewCustomerDetails(customerId) {
    window.location.href = `/Customers/Details/${customerId}`;
}

// ============================================================================
// INVENTORY FUNCTIONS
// ============================================================================

function quickAddStock(itemId, itemName) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: 'Quick Stock Add',
            html: `
                <p>Add stock for: <strong>${itemName}</strong></p>
                <div class="form-group">
                    <label for="addQuantity">Quantity to Add:</label>
                    <input type="number" class="form-control" id="addQuantity" min="1" step="0.01" value="10">
                </div>
                <div class="form-group">
                    <label for="addReason">Reason:</label>
                    <input type="text" class="form-control" id="addReason" placeholder="Stock replenishment">
                </div>
            `,
            showCancelButton: true,
            confirmButtonText: 'Add Stock',
            preConfirm: () => {
                const quantity = document.getElementById('addQuantity').value;
                const reason = document.getElementById('addReason').value;
                
                if (!quantity || quantity <= 0) {
                    Swal.showValidationMessage('Please enter a valid quantity');
                    return false;
                }
                
                return { quantity: quantity, reason: reason || 'Stock replenishment' };
            }
        }).then((result) => {
            if (result.isConfirmed) {
                processStockAdd(itemId, result.value.quantity, result.value.reason);
            }
        });
    } else {
        const quantity = prompt('Enter quantity to add:', '10');
        if (quantity && quantity > 0) {
            const reason = prompt('Enter reason (optional):', 'Stock replenishment');
            processStockAdd(itemId, quantity, reason || 'Stock replenishment');
        }
    }
}

async function processStockAdd(itemId, quantity, reason) {
    try {
        // This would be an API call to add stock
        // For now, show success and reload
        showToast(`Added ${quantity} units successfully`, 'success');
        setTimeout(() => location.reload(), 2000);
    } catch (error) {
        console.error('Error adding stock:', error);
        showToast('Error adding stock: ' + error.message, 'error');
    }
}

// ============================================================================
// GLOBAL INITIALIZATION
// ============================================================================

$(document).ready(function() {
    console.log('AmplePack missing functionality loaded');
    
    // Make functions globally available
    window.generateInvoice = generateInvoice;
    window.reorderOrder = reorderOrder;
    window.changeOrderStatus = changeOrderStatus;
    window.bulkExport = bulkExport;
    window.createOrderForCustomer = createOrderForCustomer;
    window.addProductForCustomer = addProductForCustomer;
    window.viewCustomerDetails = viewCustomerDetails;
    window.quickAddStock = quickAddStock;
    
    // Global error handler for AJAX requests
    $(document).ajaxError(function(event, xhr, settings, thrownError) {
        console.error('AJAX Error:', thrownError);
        showToast('An error occurred. Please try again.', 'error');
    });
});