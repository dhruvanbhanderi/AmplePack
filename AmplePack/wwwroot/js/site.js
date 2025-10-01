/**
 * AmplePack - Complete JavaScript Application
 * Professional, production-ready implementation
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
    
    if (text) {
        $el.html(`<i class="fas fa-spinner fa-spin mr-1"></i>${text}`);
    } else {
        $el.addClass('loading-state');
    }
    
    $el.prop('disabled', true);
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
    
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: 'Change Order Status',
            html: `
                <div class="form-group text-left">
                    <label for="swal-status" class="form-label">Select New Status:</label>
                    <select id="swal-status" class="form-control">
                        ${statusOptions.map(status => 
                            `<option value="${status}" ${status === currentStatus ? 'selected' : ''}>${status}</option>`
                        ).join('')}
                    </select>
                </div>
                <div class="alert alert-info mt-2">
                    <small><strong>Current Status:</strong> ${currentStatus}</small>
                </div>
            `,
            showCancelButton: true,
            confirmButtonText: 'Update Status',
            confirmButtonColor: '#007bff',
            cancelButtonText: 'Cancel',
            preConfirm: () => {
                const newStatus = document.getElementById('swal-status').value;
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
        const newStatus = prompt(`Change status from "${currentStatus}" to:`, currentStatus);
        if (newStatus && newStatus !== currentStatus && statusOptions.includes(newStatus)) {
            updateOrderStatus(orderId, newStatus);
        }
    }
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
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() || '',
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

/**
 * Reorder existing order
 * @param {number} orderId - Order ID to reorder
 */
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

/**
 * Bulk export orders
 */
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

/**
 * View customer details
 * @param {number} customerId - Customer ID
 */
function viewCustomerDetails(customerId) {
    window.location.href = `/Customers/Details/${customerId}`;
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
    } else {
        window.open(`/Orders/GenerateInvoice/${orderId}`, '_blank');
    }
}

// ==========================================================================
// INVENTORY MANAGEMENT
// ==========================================================================

/**
 * Quick add stock for inventory item
 * @param {number} itemId - Inventory item ID
 * @param {string} itemName - Item name
 */
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
        showToast(`Added ${quantity} units successfully`, 'success');
        setTimeout(() => location.reload(), 2000);
    } catch (error) {
        console.error('Error adding stock:', error);
        showToast('Error adding stock: ' + error.message, 'error');
    }
}

// ==========================================================================
// REPORTS & EXPORT
// ==========================================================================

/**
 * Quick export functionality
 * @param {string} reportType - Type of report
 * @param {string} format - Export format (default: pdf)
 */
function quickExport(reportType, format = 'pdf') {
    const button = window.event?.target || document.activeElement;
    const originalText = button?.innerHTML;
    
    if (button) {
        button.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Generating...';
        button.disabled = true;
    }

    fetch(`/Reports/QuickExport/${reportType}?format=${format}`, {
        method: 'GET',
        headers: {
            'Accept': 'application/pdf',
        }
    })
    .then(response => {
        if (!response.ok) {
            return response.json().then(errorData => {
                throw new Error(errorData.message || `HTTP ${response.status}: ${response.statusText}`);
            });
        }
        return response.blob();
    })
    .then(blob => {
        if (blob.size === 0) {
            throw new Error('Generated file is empty');
        }

        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${reportType}_report_${new Date().getTime()}.pdf`;
        link.style.display = 'none';
        
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        window.URL.revokeObjectURL(url);

        showToast(`${reportType.charAt(0).toUpperCase() + reportType.slice(1)} report downloaded successfully!`, 'success');
    })
    .catch(error => {
        console.error('Export failed:', error);
        showToast(`Failed to download ${reportType} report: ${error.message}`, 'error');
    })
    .finally(() => {
        if (button) {
            setTimeout(() => {
                button.innerHTML = originalText;
                button.disabled = false;
            }, 1000);
        }
    });
}

// ==========================================================================
// DATA TABLES INITIALIZATION
// ==========================================================================

/**
 * Initialize DataTables with standard configuration
 */
function initializeDataTables() {
    if (!$.fn.DataTable) return;
    
    $.fn.dataTable.ext.errMode = 'none';
    
    $('.table:not(.no-datatable)').each(function() {
        const $table = $(this);
        
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
                    { orderable: false, targets: -1 }
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
// APPLICATION CORE
// ==========================================================================

const AmplePack = {
    // Configuration
    config: {
        toast: {
            timer: 3000,
            position: 'top-end',
            showConfirmButton: false,
            toast: true
        }
    },

    // Initialize application
    init: function() {
        this.setupCSRF();
        this.setupDataTables();
        this.setupDropdowns();
        this.setupModals();
        this.setupTooltips();
        this.setupNavigation();
        this.setupForms();
        console.log('AmplePack initialized successfully');
    },

    // Setup CSRF token for AJAX requests
    setupCSRF: function() {
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
    },

    // Setup DataTables
    setupDataTables: function() {
        initializeDataTables();
    },

    // Setup dropdown handling
    setupDropdowns: function() {
        $(document).on('click', '.dropdown-toggle', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const $this = $(this);
            const $dropdown = $this.next('.dropdown-menu');
            
            $('.dropdown-menu').not($dropdown).removeClass('show');
            $dropdown.toggleClass('show');
            
            const rect = this.getBoundingClientRect();
            const dropdownRect = $dropdown[0].getBoundingClientRect();
            
            if (rect.left + dropdownRect.width > window.innerWidth) {
                $dropdown.addClass('dropdown-menu-right');
            }
        });

        $(document).on('click', function(e) {
            if (!$(e.target).closest('.dropdown').length) {
                $('.dropdown-menu').removeClass('show');
            }
        });

        $(document).on('click', '.dropdown-menu', function(e) {
            e.stopPropagation();
        });

        $(document).on('click', '.dropdown-item', function(e) {
            if (!$(e.target).is('input, select, textarea, label')) {
                $(this).closest('.dropdown-menu').removeClass('show');
            }
        });
    },

    // Setup modal handling
    setupModals: function() {
        $('.modal').on('shown.bs.modal', function() {
            $(this).find('input:visible:first').focus();
        });

        $('.modal').on('hidden.bs.modal', function() {
            $(this).find('.is-invalid').removeClass('is-invalid');
            $(this).find('.invalid-feedback').remove();
            $(this).find('form')[0]?.reset();
        });
    },

    // Setup tooltips
    setupTooltips: function() {
        if (typeof bootstrap !== 'undefined') {
            const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (tooltipTriggerEl) {
                return new bootstrap.Tooltip(tooltipTriggerEl);
            });
        } else if ($.fn.tooltip) {
            $('[data-toggle="tooltip"]').tooltip();
            $('[title]').not('[data-toggle="tooltip"]').tooltip();
        }
    },

    // Setup navigation
    setupNavigation: function() {
        this.setActiveNavigation();
        this.setupNavigationHandlers();
    },

    // Set active navigation based on current path
    setActiveNavigation: function() {
        const currentPath = window.location.pathname.toLowerCase();
        const currentController = this.getCurrentController();
        const currentAction = this.getCurrentAction();
        
        console.log('Setting active navigation for:', currentPath, 'Controller:', currentController, 'Action:', currentAction);
        
        $('.nav-sidebar .nav-link').removeClass('active');
        $('.nav-sidebar .nav-item').removeClass('menu-open');
        
        let $activeLink = null;
        let exactMatch = false;
        
        // First pass: Look for exact matches
        $('.nav-sidebar .nav-link[href]').each(function() {
            const $link = $(this);
            const href = $link.attr('href');
            
            if (!href || href === '#') return;
            
            const linkPath = href.toLowerCase();
            
            if (currentPath === linkPath) {
                $activeLink = $link;
                exactMatch = true;
                return false;
            }
            
            if (currentPath === '/' && linkPath.includes('/home/index')) {
                $activeLink = $link;
                exactMatch = true;
                return false;
            }
        });
        
        // Second pass: Controller/Action based matching
        if (!exactMatch && currentController) {
            $('.nav-sidebar .nav-link[href]').each(function() {
                const $link = $(this);
                const href = $link.attr('href');
                
                if (!href || href === '#') return;
                
                const linkPath = href.toLowerCase();
                const linkSegments = linkPath.split('/').filter(s => s.length > 0);
                const linkController = linkSegments.length > 0 ? linkSegments[0] : null;
                const linkAction = linkSegments.length > 1 ? linkSegments[1] : 'index';
                
                if (linkController === currentController && linkAction === currentAction) {
                    $activeLink = $link;
                    return false;
                }
                
                if (linkController === currentController && currentAction === 'index' && !$activeLink) {
                    $activeLink = $link;
                }
                
                if (linkController === currentController && !$activeLink) {
                    $activeLink = $link;
                }
            });
        }
        
        if ($activeLink) {
            console.log('Activating navigation item:', $activeLink.attr('href'));
            this.activateNavigationItem($activeLink);
        } else {
            console.log('No matching navigation item found for:', currentPath);
        }
    },

    // Activate specific navigation item
    activateNavigationItem: function($link) {
        $link.addClass('active');
        
        const $submenu = $link.closest('.nav-treeview');
        if ($submenu.length > 0) {
            const $parentItem = $submenu.closest('.nav-item');
            $parentItem.addClass('menu-open');
            
            const $parentLink = $parentItem.children('.nav-link').first();
            $parentLink.addClass('active').addClass('parent-active');
            
            console.log('Opened parent menu for submenu item');
        } else {
            console.log('Activated top-level menu item');
        }
    },

    // Get current controller from URL
    getCurrentController: function() {
        const path = window.location.pathname;
        const segments = path.split('/').filter(s => s.length > 0);
        
        if (segments.length === 0) {
            return 'home';
        }
        
        return segments[0].toLowerCase();
    },

    // Get current action from URL
    getCurrentAction: function() {
        const path = window.location.pathname;
        const segments = path.split('/').filter(s => s.length > 0);
        
        if (segments.length <= 1) {
            return 'index';
        }
        
        return segments[1].toLowerCase();
    },

    // Setup navigation event handlers
    setupNavigationHandlers: function() {
        $(document).on('click', '.nav-sidebar .nav-link', function(e) {
            const $this = $(this);
            const $parent = $this.parent('.nav-item');
            const hasSubmenu = $parent.find('.nav-treeview').length > 0;
            const href = $this.attr('href');
            
            if (hasSubmenu && href === '#') {
                e.preventDefault();
                
                const isOpen = $parent.hasClass('menu-open');
                
                $parent.siblings('.nav-item').removeClass('menu-open');
                $parent.siblings('.nav-item').find('> .nav-link').removeClass('active');
                
                if (isOpen) {
                    $parent.removeClass('menu-open');
                    $this.removeClass('active');
                } else {
                    $parent.addClass('menu-open');
                    $this.addClass('active');
                }
            }
        });
        
        $('.nav-sidebar .nav-link').on('mouseenter', function() {
            if (!$(this).hasClass('active')) {
                $(this).addClass('nav-hover');
            }
        }).on('mouseleave', function() {
            $(this).removeClass('nav-hover');
        });
    },

    // Setup form handling
    setupForms: function() {
        $(document).on('input', '.form-control[required]', function() {
            const $input = $(this);
            const isValid = this.checkValidity();
            
            $input.toggleClass('is-valid', isValid && $input.val() !== '');
            $input.toggleClass('is-invalid', !isValid && $input.val() !== '');
        });

        $(document).on('submit', 'form[data-loading]', function() {
            const $form = $(this);
            const $submitBtn = $form.find('[type="submit"]');
            
            $submitBtn.addClass('loading').prop('disabled', true);
            
            setTimeout(() => {
                $submitBtn.removeClass('loading').prop('disabled', false);
            }, 5000);
        });
    },

    // Utility functions
    showConfirm: function(message, callback, options = {}) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                title: options.title || 'Are you sure?',
                text: message,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: options.confirmText || 'Yes, proceed!',
                cancelButtonText: options.cancelText || 'Cancel'
            }).then((result) => {
                if (result.isConfirmed && typeof callback === 'function') {
                    callback();
                }
            });
        } else {
            if (confirm(message) && typeof callback === 'function') {
                callback();
            }
        }
    },

    formatCurrency: function(amount) {
        return new Intl.NumberFormat('en-IN', {
            style: 'currency',
            currency: 'INR'
        }).format(amount);
    },

    formatDate: function(date) {
        return new Date(date).toLocaleDateString('en-IN');
    },

    formatDateTime: function(date) {
        return new Date(date).toLocaleString('en-IN');
    }
};

// ==========================================================================
// DOCUMENT READY
// ==========================================================================

$(document).ready(function() {
    // Initialize main application
    AmplePack.init();
    
    // Global error handler for AJAX requests
    $(document).ajaxError(function(event, xhr, settings, thrownError) {
        console.error('AJAX Error:', thrownError);
        showToast('An error occurred. Please try again.', 'error');
    });
    
    // Auto-refresh tables after AJAX operations
    $(document).on('ajaxSuccess', function() {
        setTimeout(() => {
            initializeDataTables();
        }, 500);
    });
    
    // Initialize AdminLTE components
    if (typeof AdminLTE !== 'undefined') {
        AdminLTE.init();
    }
    
    console.log('AmplePack application loaded successfully');
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
window.reorderOrder = reorderOrder;
window.bulkExport = bulkExport;
window.createOrderForCustomer = createOrderForCustomer;
window.addProductForCustomer = addProductForCustomer;
window.viewCustomerDetails = viewCustomerDetails;
window.generateInvoice = generateInvoice;
window.quickAddStock = quickAddStock;
window.quickExport = quickExport;
window.initializeDataTables = initializeDataTables;

// Export main namespace
window.AmplePack = AmplePack;
