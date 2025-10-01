/**
 * AmplePack - Core JavaScript Functionality
 * Professional, clean implementation for production use
 */

// ==========================================================================
// CORE UTILITIES
// ==========================================================================

/**
 * Display toast notifications
 * @param {string} message - Message to display
 * @param {string} type - Type of notification (success, error, warning, info)
 * @param {number} duration - Duration in milliseconds
 */
function showToast(message, type = 'info', duration = 5000) {
    // Use SweetAlert2 for consistent notifications
    if (typeof Swal !== 'undefined') {
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: duration,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer);
                toast.addEventListener('mouseleave', Swal.resumeTimer);
            }
        });

        Toast.fire({
            icon: type === 'error' ? 'error' : type,
            title: message
        });
    } else {
        // Fallback to console
        console.log(`${type.toUpperCase()}: ${message}`);
    }
}

/**
 * Show loading state on element
 * @param {string|jQuery} element - Element selector or jQuery object
 * @param {string} text - Loading text
 */
function showLoading(element, text = 'Loading...') {
    const $el = $(element);
    if (!$el.length) return;
    
    $el.data('original-html', $el.html());
    $el.data('original-disabled', $el.prop('disabled'));
    $el.html(`<i class="fas fa-spinner fa-spin mr-1"></i>${text}`);
    $el.prop('disabled', true);
    $el.addClass('loading-state');
}

/**
 * Hide loading state from element
 * @param {string|jQuery} element - Element selector or jQuery object
 */
function hideLoading(element) {
    const $el = $(element);
    if (!$el.length) return;
    
    const originalHtml = $el.data('original-html');
    const originalDisabled = $el.data('original-disabled');
    
    if (originalHtml) {
        $el.html(originalHtml);
    }
    $el.prop('disabled', originalDisabled || false);
    $el.removeClass('loading-state');
}

// ==========================================================================
// ORDER MANAGEMENT
// ==========================================================================

/**
 * Change order status
 * @param {number} orderId - Order ID
 * @param {string} currentStatus - Current order status
 */
function changeOrderStatus(orderId, currentStatus) {
    const statusOptions = ['Pending', 'Processing', 'Completed', 'Delivered', 'Cancelled'];
    
    Swal.fire({
        title: 'Change Order Status',
        html: `
            <div class="form-group">
                <label for="swal-status">Select New Status:</label>
                <select id="swal-status" class="form-control">
                    ${statusOptions.map(status => 
                        `<option value="${status}" ${status === currentStatus ? 'selected' : ''}>${status}</option>`
                    ).join('')}
                </select>
            </div>
            <div class="alert alert-info mt-2">
                <small>Current status: <strong>${currentStatus}</strong></small>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: 'Update Status',
        confirmButtonColor: '#007bff',
        cancelButtonText: 'Cancel',
        preConfirm: () => {
            return document.getElementById('swal-status').value;
        }
    }).then((result) => {
        if (result.isConfirmed) {
            updateOrderStatus(orderId, result.value);
        }
    });
}

/**
 * Update order status via API
 * @param {number} orderId - Order ID
 * @param {string} newStatus - New status
 */
async function updateOrderStatus(orderId, newStatus) {
    try {
        const response = await fetch(`/Orders/ChangeStatus/${orderId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val(),
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ status: newStatus })
        });

        const data = await response.json();

        if (data.success) {
            showToast('Order status updated successfully', 'success');
            setTimeout(() => location.reload(), 1500);
        } else {
            showToast('Failed to update status: ' + (data.message || 'Unknown error'), 'error');
        }
    } catch (error) {
        console.error('Error updating status:', error);
        showToast('Error updating status', 'error');
    }
}

// ==========================================================================
// CUSTOMER MANAGEMENT
// ==========================================================================

/**
 * Create order for customer
 * @param {number} customerId - Customer ID
 */
function createOrderForCustomer(customerId) {
    window.location.href = `/Orders/CreateWorkflow?customerId=${customerId}`;
}

/**
 * Add product for customer
 * @param {number} customerId - Customer ID
 */
function addProductForCustomer(customerId) {
    window.location.href = `/CustomerProducts/Create?customerId=${customerId}`;
}

// ==========================================================================
// INVOICE GENERATION
// ==========================================================================

/**
 * Generate invoice for order
 * @param {number} orderId - Order ID
 */
function generateInvoice(orderId) {
    const button = event?.target?.closest('a, button');
    if (button) {
        showLoading(button, '');
        window.open(`/Orders/GenerateInvoice/${orderId}`, '_blank');
        setTimeout(() => hideLoading(button), 2000);
    }
}

// ==========================================================================
// DATA TABLES INITIALIZATION
// ==========================================================================

/**
 * Initialize DataTables with standard configuration
 */
function initializeDataTables() {
    if (!$.fn.DataTable) return;
    
    // Set error mode to none to prevent alerts
    $.fn.dataTable.ext.errMode = 'none';
    
    // Initialize tables with standard configuration
    $('.table:not(.no-datatable)').each(function() {
        const $table = $(this);
        
        // Skip if already initialized or explicitly excluded
        if ($.fn.DataTable.isDataTable(this) || $table.hasClass('no-datatable')) {
            return;
        }
        
        try {
            $table.DataTable({
                responsive: true,
                pageLength: 25,
                lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
                language: {
                    search: "Search:",
                    lengthMenu: "Show _MENU_ entries",
                    info: "Showing _START_ to _END_ of _TOTAL_ entries",
                    emptyTable: "No data available",
                    zeroRecords: "No matching records found"
                },
                columnDefs: [
                    { orderable: false, targets: 'no-sort' },
                    { orderable: false, targets: -1 } // Last column usually actions
                ],
                order: [],
                autoWidth: false,
                processing: true,
                stateSave: false
            });
        } catch (e) {
            console.warn('DataTable initialization failed:', e);
            $table.addClass('table-fallback');
        }
    });
}

// ==========================================================================
// INITIALIZATION
// ==========================================================================

$(document).ready(function() {
    // Initialize DataTables
    initializeDataTables();
    
    // Setup AJAX CSRF token
    $.ajaxSetup({
        beforeSend: function(xhr, settings) {
            if (!/^(GET|HEAD|OPTIONS|TRACE)$/i.test(settings.type) && !this.crossDomain) {
                const token = $('input[name=__RequestVerificationToken]').val();
                if (token) {
                    xhr.setRequestHeader("RequestVerificationToken", token);
                }
            }
        }
    });
    
    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    } else if ($.fn.tooltip) {
        $('[data-toggle="tooltip"]').tooltip();
    }
    
    // Page enhancement: Auto-refresh tables after AJAX operations
    $(document).on('ajaxSuccess', function() {
        setTimeout(() => {
            initializeDataTables();
        }, 500);
    });
});

// ==========================================================================
// GLOBAL EXPORTS
// ==========================================================================

// Make functions globally available
window.showToast = showToast;
window.showLoading = showLoading;
window.hideLoading = hideLoading;
window.changeOrderStatus = changeOrderStatus;
window.updateOrderStatus = updateOrderStatus;
window.createOrderForCustomer = createOrderForCustomer;
window.addProductForCustomer = addProductForCustomer;
window.generateInvoice = generateInvoice;

// Export main namespace
window.AmplePack = {
    showToast,
    showLoading,
    hideLoading,
    changeOrderStatus,
    updateOrderStatus,
    createOrderForCustomer,
    addProductForCustomer,
    generateInvoice,
    initializeDataTables
};
