// AmplePack - Site JavaScript
// Enhanced order and inventory management functionality

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

// Show toast notifications
function showToast(message, type = 'info', duration = 5000) {
    // Remove existing toasts
    $('.toast-container .toast').toast('hide');
    
    const toastId = 'toast_' + Date.now();
    const iconClass = {
        'success': 'fas fa-check-circle text-success',
        'error': 'fas fa-exclamation-circle text-danger',
        'warning': 'fas fa-exclamation-triangle text-warning',
        'info': 'fas fa-info-circle text-info'
    }[type] || 'fas fa-info-circle text-info';
    
    const toastHtml = `
        <div class="toast" id="${toastId}" role="alert" aria-live="assertive" aria-atomic="true" data-delay="${duration}">
            <div class="toast-header">
                <i class="${iconClass} mr-2"></i>
                <strong class="mr-auto">AmplePack</strong>
                <button type="button" class="ml-2 mb-1 close" data-dismiss="toast" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;
    
    // Create toast container if it doesn't exist
    if (!$('.toast-container').length) {
        $('body').append('<div class="toast-container position-fixed" style="top: 20px; right: 20px; z-index: 9999;"></div>');
    }
    
    $('.toast-container').append(toastHtml);
    $(`#${toastId}`).toast('show');
}

// Show loading spinner
function showLoading(element) {
    const $el = $(element);
    $el.data('original-html', $el.html());
    $el.html('<i class="fas fa-spinner fa-spin"></i> Loading...');
    $el.prop('disabled', true);
}

// Hide loading spinner
function hideLoading(element) {
    const $el = $(element);
    const originalHtml = $el.data('original-html');
    if (originalHtml) {
        $el.html(originalHtml);
    }
    $el.prop('disabled', false);
}

// ============================================================================
// INVENTORY QUICK-ADJUST
// ============================================================================

class InventoryQuickAdjust {
    constructor() {
        this.init();
    }
    
    init() {
        this.setupQuickAdjustControls();
    }
    
    setupQuickAdjustControls() {
        // Quick add event
        $(document).on('click', '.quick-add', (e) => {
            const itemId = $(e.currentTarget).data('item-id');
            const itemName = $(e.currentTarget).data('item-name');
            this.showQuickAddModal(itemId, itemName);
        });
        
        // Quick remove event
        $(document).on('click', '.quick-remove', (e) => {
            const itemId = $(e.currentTarget).data('item-id');
            const itemName = $(e.currentTarget).data('item-name');
            this.showQuickRemoveModal(itemId, itemName);
        });
    }
    
    showQuickAddModal(itemId, itemName) {
        $('#addItemId').val(itemId);
        $('#addItemName').val(itemName);
        $('#addQuantity').val('');
        $('#addReason').val('');
        $('#quickAddModal').modal('show');
    }
    
    showQuickRemoveModal(itemId, itemName) {
        $('#removeItemId').val(itemId);
        $('#removeItemName').val(itemName);
        $('#removeQuantity').val('');
        $('#removeCustomer').val('');
        $('#removeOrder').val('').prop('disabled', true);
        $('#removeReason').val('');
        $('#quickRemoveModal').modal('show');
    }
    
    async processQuickAdd() {
        const formData = {
            itemId: parseInt($('#addItemId').val()),
            quantity: parseFloat($('#addQuantity').val()),
            reason: $('#addReason').val()
        };
        
        if (!formData.quantity || formData.quantity <= 0) {
            showToast('Please enter a valid quantity', 'error');
            return;
        }
        
        try {
            showLoading('#confirmQuickAdd');
            
            const response = await fetch('/api/inventory/quick-add', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                body: JSON.stringify(formData)
            });
            
            const result = await response.json();
            
            if (result.success) {
                $('#quickAddModal').modal('hide');
                showToast(result.message || `Successfully added ${formData.quantity} units`, 'success');
                this.updateInventoryDisplay(formData.itemId, result.newQuantity);
            } else {
                showToast(result.message || 'Failed to add inventory', 'error');
            }
        } catch (error) {
            console.error('Error adding inventory:', error);
            showToast('Error adding inventory', 'error');
        } finally {
            hideLoading('#confirmQuickAdd');
        }
    }
    
    async processQuickRemove() {
        const formData = {
            itemId: parseInt($('#removeItemId').val()),
            quantity: parseFloat($('#removeQuantity').val()),
            customerId: $('#removeCustomer').val() ? parseInt($('#removeCustomer').val()) : null,
            orderId: $('#removeOrder').val() ? parseInt($('#removeOrder').val()) : null,
            reason: $('#removeReason').val()
        };
        
        if (!formData.quantity || formData.quantity <= 0) {
            showToast('Please enter a valid quantity', 'error');
            return;
        }
        
        if (!formData.reason.trim()) {
            showToast('Please provide a reason for the removal', 'error');
            return;
        }
        
        try {
            showLoading('#confirmQuickRemove');
            
            const response = await fetch('/api/inventory/quick-remove', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                body: JSON.stringify(formData)
            });
            
            const result = await response.json();
            
            if (result.success) {
                $('#quickRemoveModal').modal('hide');
                showToast(result.message || `Successfully removed ${formData.quantity} units`, 'success');
                this.updateInventoryDisplay(formData.itemId, result.newQuantity);
            } else {
                showToast(result.message || 'Failed to remove inventory', 'error');
            }
        } catch (error) {
            console.error('Error removing inventory:', error);
            showToast('Error removing inventory', 'error');
        } finally {
            hideLoading('#confirmQuickRemove');
        }
    }
    
    updateInventoryDisplay(itemId, newQuantity) {
        const $row = $(`.inventory-item-row[data-item-id="${itemId}"]`);
        const $quantityCell = $row.find('.quantity-cell');
        
        // Determine badge class based on low stock status
        const isLowStock = newQuantity < 10;
        const badgeClass = isLowStock ? 'badge-warning' : 'badge-success';
        
        $quantityCell.html(`<span class="badge ${badgeClass}">${newQuantity.toFixed(2)}</span>`);
        
        // Add flash effect
        $quantityCell.addClass('bg-success text-white');
        setTimeout(() => {
            $quantityCell.removeClass('bg-success text-white');
        }, 1000);

        // Update row styling if low stock status changed
        if (isLowStock) {
            $row.addClass('table-warning');
        } else {
            $row.removeClass('table-warning');
        }
    }
}

// ============================================================================
// ACCESSIBILITY ENHANCEMENTS
// ============================================================================

class AccessibilityManager {
    constructor() {
        this.init();
    }
    
    init() {
        this.setupKeyboardNavigation();
        this.setupAriaLabels();
        this.setupFocusManagement();
    }
    
    setupKeyboardNavigation() {
        // ESC key to close modals
        $(document).on('keydown', (e) => {
            if (e.key === 'Escape') {
                $('.modal.show').modal('hide');
            }
        });
    }
    
    setupAriaLabels() {
        // Add ARIA labels to dynamic content
        $('.quick-add').attr('aria-label', 'Quick add inventory');
        $('.quick-remove').attr('aria-label', 'Quick remove inventory');
    }
    
    setupFocusManagement() {
        // Manage focus for modals
        $('.modal').on('shown.bs.modal', function() {
            $(this).find('input:first').focus();
        });
    }
}

// ============================================================================
// INITIALIZATION
// ============================================================================

$(document).ready(function() {
    // Initialize inventory quick adjust if on inventory page
    if (window.location.pathname.includes('/Inventory')) {
        window.inventoryQuickAdjust = new InventoryQuickAdjust();
    }
    
    // Initialize accessibility enhancements globally
    window.accessibilityManager = new AccessibilityManager();
    
    // Setup global CSRF token
    $.ajaxSetup({
        beforeSend: function(xhr, settings) {
            if (!/^(GET|HEAD|OPTIONS|TRACE)$/i.test(settings.type) && !this.crossDomain) {
                xhr.setRequestHeader("RequestVerificationToken", $('input[name=__RequestVerificationToken]').val());
            }
        }
    });
});

// ============================================================================
// GLOBAL HELPER FUNCTIONS
// ============================================================================

// Function to create order for customer
window.createOrderForCustomer = function(customerId) {
    window.location.href = `/Orders/CreateWorkflow?customerId=${customerId}`;
};

// Function to show audit log
window.showAuditLog = function(type, entityId) {
    showToast(`Audit log for ${type} #${entityId} - Feature coming soon!`, 'info');
};

// Export for external use
window.AmplePack = {
    InventoryQuickAdjust,
    AccessibilityManager,
    showToast,
    showLoading,
    hideLoading
};
