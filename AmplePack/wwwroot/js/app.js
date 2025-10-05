/**
 * AmplePack Application JavaScript
 * Compatible with AdminLTE 3.2 and jQuery
 */

(function($) {
    'use strict';

    // ==========================================================================
    // GLOBAL VARIABLES
    // ==========================================================================
    window.AmplePack = window.AmplePack || {};
    
    const App = {
        // Configuration
        config: {
            toast: {
                timer: 3000,
                position: 'top-end',
                showConfirmButton: false,
                toast: true
            },
            dataTable: {
                responsive: true,
                autoWidth: false,
                pageLength: 25,
                lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
                language: {
                    search: "Search:",
                    lengthMenu: "Show _MENU_ entries",
                    info: "Showing _START_ to _END_ of _TOTAL_ entries",
                    paginate: {
                        first: "First",
                        last: "Last",
                        next: "Next",
                        previous: "Previous"
                    },
                    emptyTable: "No data available",
                    zeroRecords: "No matching records found"
                },
                dom: "<'row'<'col-sm-12 col-md-6'l><'col-sm-12 col-md-6'f>>" +
                     "<'row'<'col-sm-12'tr>>" +
                     "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
                columnDefs: [
                    { orderable: false, targets: 'no-sort' },
                    { searchable: false, targets: 'no-search' }
                ]
            }
        },

        // ==========================================================================
        // INITIALIZATION
        // ==========================================================================
        init: function() {
            this.setupCSRF();
            this.setupDataTables();
            this.setupDropdowns();
            this.setupModals();
            this.setupTooltips();
            this.setupNavigation();
            this.setupForms();
            console.log('AmplePack App initialized successfully');
        },

        // ==========================================================================
        // CSRF TOKEN SETUP
        // ==========================================================================
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

        // ==========================================================================
        // DATATABLES SETUP
        // ==========================================================================
        setupDataTables: function() {
            if (!$.fn.DataTable) return;

            // Set error mode to none
            $.fn.dataTable.ext.errMode = 'none';

            // Initialize DataTables
            $('.data-table').each(function() {
                const $table = $(this);
                
                // Skip if already initialized
                if ($.fn.DataTable.isDataTable(this)) {
                    return;
                }

                try {
                    const config = $.extend(true, {}, App.config.dataTable);
                    
                    // Custom configuration from data attributes
                    if ($table.data('page-length')) {
                        config.pageLength = $table.data('page-length');
                    }
                    
                    if ($table.data('order')) {
                        config.order = $table.data('order');
                    }

                    $table.DataTable(config);
                } catch (error) {
                    console.warn('DataTable initialization failed:', error);
                }
            });
        },

        // ==========================================================================
        // DROPDOWN SETUP
        // ==========================================================================
        setupDropdowns: function() {
            // Enhanced dropdown handling
            $(document).on('click', '.dropdown-toggle', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                const $this = $(this);
                const $dropdown = $this.next('.dropdown-menu');
                
                // Close other dropdowns
                $('.dropdown-menu').not($dropdown).removeClass('show');
                
                // Toggle current dropdown
                $dropdown.toggleClass('show');
                
                // Position adjustment for edge cases
                const rect = this.getBoundingClientRect();
                const dropdownRect = $dropdown[0].getBoundingClientRect();
                
                if (rect.left + dropdownRect.width > window.innerWidth) {
                    $dropdown.addClass('dropdown-menu-right');
                }
            });

            // Close dropdowns when clicking outside
            $(document).on('click', function(e) {
                if (!$(e.target).closest('.dropdown').length) {
                    $('.dropdown-menu').removeClass('show');
                }
            });

            // Prevent dropdown from closing when clicking inside
            $(document).on('click', '.dropdown-menu', function(e) {
                e.stopPropagation();
            });

            // Close dropdown when clicking items (except form elements)
            $(document).on('click', '.dropdown-item', function(e) {
                if (!$(e.target).is('input, select, textarea, label')) {
                    $(this).closest('.dropdown-menu').removeClass('show');
                }
            });
        },

        // ==========================================================================
        // MODAL SETUP
        // ==========================================================================
        setupModals: function() {
            // Auto-focus first input in modals
            $('.modal').on('shown.bs.modal', function() {
                $(this).find('input:visible:first').focus();
            });

            // Clear form validation on modal hide
            $('.modal').on('hidden.bs.modal', function() {
                $(this).find('.is-invalid').removeClass('is-invalid');
                $(this).find('.invalid-feedback').remove();
                $(this).find('form')[0]?.reset();
            });
        },

        // ==========================================================================
        // TOOLTIP SETUP
        // ==========================================================================
        setupTooltips: function() {
            $('[data-toggle="tooltip"]').tooltip();
            $('[title]').not('[data-toggle="tooltip"]').tooltip();
        },

        // ==========================================================================
        // NAVIGATION SETUP
        // ==========================================================================
        setupNavigation: function() {
            this.setActiveNavigation();
            this.setupNavigationHandlers();
        },

        setActiveNavigation: function() {
            const currentPath = window.location.pathname.toLowerCase();
            const currentController = this.getCurrentController();
            const currentAction = this.getCurrentAction();
            
            console.log('Setting active navigation for:', currentPath, 'Controller:', currentController, 'Action:', currentAction);
            
            // Remove all active classes first
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
                
                // Exact path match (highest priority)
                if (currentPath === linkPath) {
                    $activeLink = $link;
                    exactMatch = true;
                    return false; // Break the loop
                }
                
                // Check for root path match
                if (currentPath === '/' && linkPath.includes('/home/index')) {
                    $activeLink = $link;
                    exactMatch = true;
                    return false;
                }
            });
            
            // Second pass: Controller/Action based matching if no exact match
            if (!exactMatch && currentController) {
                $('.nav-sidebar .nav-link[href]').each(function() {
                    const $link = $(this);
                    const href = $link.attr('href');
                    
                    if (!href || href === '#') return;
                    
                    const linkPath = href.toLowerCase();
                    
                    // Parse the link path
                    const linkSegments = linkPath.split('/').filter(s => s.length > 0);
                    const linkController = linkSegments.length > 0 ? linkSegments[0] : null;
                    const linkAction = linkSegments.length > 1 ? linkSegments[1] : 'index';
                    
                    // Controller and action match
                    if (linkController === currentController && linkAction === currentAction) {
                        $activeLink = $link;
                        return false;
                    }
                    
                    // Controller match with index action (fallback)
                    if (linkController === currentController && currentAction === 'index' && !$activeLink) {
                        $activeLink = $link;
                    }
                    
                    // Just controller match (lowest priority)
                    if (linkController === currentController && !$activeLink) {
                        $activeLink = $link;
                    }
                });
            }
            
            // Apply active state
            if ($activeLink) {
                console.log('Activating navigation item:', $activeLink.attr('href'));
                this.activateNavigationItem($activeLink);
            } else {
                console.log('No matching navigation item found for:', currentPath);
            }
        },

        activateNavigationItem: function($link) {
            // Add active class to the link
            $link.addClass('active');
            
            // Check if this is a submenu item
            const $submenu = $link.closest('.nav-treeview');
            if ($submenu.length > 0) {
                // This is a submenu item, open the parent menu
                const $parentItem = $submenu.closest('.nav-item');
                $parentItem.addClass('menu-open');
                
                // Also make the parent link active (but with different styling)
                const $parentLink = $parentItem.children('.nav-link').first();
                $parentLink.addClass('active').addClass('parent-active');
                
                console.log('Opened parent menu for submenu item');
            } else {
                console.log('Activated top-level menu item');
            }
        },

        getCurrentController: function() {
            const path = window.location.pathname;
            const segments = path.split('/').filter(s => s.length > 0);
            
            // Handle root path
            if (segments.length === 0) {
                return 'home';
            }
            
            return segments[0].toLowerCase();
        },

        getCurrentAction: function() {
            const path = window.location.pathname;
            const segments = path.split('/').filter(s => s.length > 0);
            
            // Handle root path or controller only
            if (segments.length <= 1) {
                return 'index';
            }
            
            return segments[1].toLowerCase();
        },

        setupNavigationHandlers: function() {
            // Handle expandable menu clicks
            $(document).on('click', '.nav-sidebar .nav-link', function(e) {
                const $this = $(this);
                const $parent = $this.parent('.nav-item');
                const hasSubmenu = $parent.find('.nav-treeview').length > 0;
                const href = $this.attr('href');
                const dataSection = $this.attr('data-section');
                
                // For menu items with submenus (like Reports, Orders)
                if (hasSubmenu && (href === '#' || dataSection)) {
                    e.preventDefault();
                    
                    // Toggle current menu
                    const isOpen = $parent.hasClass('menu-open');
                    
                    // Close all other open menus at the same level
                    $parent.siblings('.nav-item').removeClass('menu-open');
                    $parent.siblings('.nav-item').find('> .nav-link').removeClass('active');
                    
                    // Toggle current menu
                    if (isOpen) {
                        $parent.removeClass('menu-open');
                        $this.removeClass('active');
                    } else {
                        $parent.addClass('menu-open');
                        $this.addClass('active');
                    }
                    
                    console.log('Toggled submenu:', dataSection || 'unknown', 'Open:', !isOpen);
                }
                // For actual navigation links, let them proceed normally
                // The active state will be set on page load
            });
            
            // Add visual feedback for navigation
            $('.nav-sidebar .nav-link').on('mouseenter', function() {
                if (!$(this).hasClass('active')) {
                    $(this).addClass('nav-hover');
                }
            }).on('mouseleave', function() {
                $(this).removeClass('nav-hover');
            });
        },

        // ==========================================================================
        // FORM SETUP
        // ==========================================================================
        setupForms: function() {
            // Real-time validation
            $(document).on('input', '.form-control[required]', function() {
                const $input = $(this);
                const isValid = this.checkValidity();
                
                $input.toggleClass('is-valid', isValid && $input.val() !== '');
                $input.toggleClass('is-invalid', !isValid && $input.val() !== '');
            });

            // Form submission with loading state
            $(document).on('submit', 'form[data-loading]', function() {
                const $form = $(this);
                const $submitBtn = $form.find('[type="submit"]');
                
                $submitBtn.addClass('loading').prop('disabled', true);
                
                // Re-enable after 5 seconds as failsafe
                setTimeout(() => {
                    $submitBtn.removeClass('loading').prop('disabled', false);
                }, 5000);
            });
        },

        // ==========================================================================
        // UTILITY FUNCTIONS
        // ==========================================================================
        showToast: function(message, type = 'info', options = {}) {
            // Use SweetAlert2 if available, fallback to simple alert
            if (typeof Swal !== 'undefined') {
                const config = $.extend({}, this.config.toast, {
                    icon: type,
                    title: message
                }, options);
                
                Swal.fire(config);
            } else {
                // Fallback to browser alert
                alert(message);
            }
        },

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
                // Fallback to browser confirm
                if (confirm(message) && typeof callback === 'function') {
                    callback();
                }
            }
        },

        showLoading: function(selector) {
            const $element = $(selector);
            if ($element.length) {
                $element.addClass('loading').prop('disabled', true);
            }
        },

        hideLoading: function(selector) {
            const $element = $(selector);
            if ($element.length) {
                $element.removeClass('loading').prop('disabled', false);
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
    // INVENTORY MANAGEMENT
    // ==========================================================================
    const InventoryManager = {
        init: function() {
            this.setupQuickAdjust();
            this.loadInventoryData();
        },

        setupQuickAdjust: function() {
            $(document).on('click', '.quick-add', this.handleQuickAdd.bind(this));
            $(document).on('click', '.quick-remove', this.handleQuickRemove.bind(this));
        },

        loadInventoryData: function() {
            // Load real inventory data for the current page
            if ($('.inventory-table').length > 0) {
                this.refreshInventoryTable();
            }
        },

        refreshInventoryTable: function() {
            $.ajax({
                url: '/api/inventory/current-stock',
                method: 'GET',
                success: function(response) {
                    if (response.success) {
                        // Update inventory display with real data
                        $('.inventory-table tbody tr').each(function() {
                            const $row = $(this);
                            const itemId = $row.data('item-id');
                            const item = response.data.find(i => i.id === itemId);
                            
                            if (item) {
                                $row.find('.available-quantity').text(item.availableQuantity + ' ' + item.unit);
                                $row.find('.stock-status').removeClass('badge-success badge-warning badge-danger')
                                    .addClass(item.stockStatus === 'In Stock' ? 'badge-success' : 
                                             item.stockStatus === 'Low Stock' ? 'badge-warning' : 'badge-danger')
                                    .text(item.stockStatus);
                            }
                        });
                    }
                },
                error: function() {
                    console.warn('Failed to load inventory data');
                }
            });
        },

        handleQuickAdd: function(e) {
            e.preventDefault();
            const $btn = $(e.currentTarget);
            const itemId = $btn.data('item-id');
            const itemName = $btn.data('item-name');
            
            this.showQuantityModal('Add Stock', itemId, itemName, 'add');
        },

        handleQuickRemove: function(e) {
            e.preventDefault();
            const $btn = $(e.currentTarget);
            const itemId = $btn.data('item-id');
            const itemName = $btn.data('item-name');
            
            this.showQuantityModal('Remove Stock', itemId, itemName, 'remove');
        },

        showQuantityModal: function(title, itemId, itemName, action) {
            const modalHtml = `
                <div class="modal fade" id="quantityModal" tabindex="-1" role="dialog">
                    <div class="modal-dialog" role="document">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">${title}</h5>
                                <button type="button" class="close" data-dismiss="modal">
                                    <span>&times;</span>
                                </button>
                            </div>
                            <div class="modal-body">
                                <form id="quantityForm">
                                    <div class="form-group">
                                        <label>Item: <strong>${itemName}</strong></label>
                                    </div>
                                    <div class="form-group">
                                        <label for="quantity">Quantity</label>
                                        <input type="number" class="form-control" id="quantity" 
                                               min="0.01" step="0.01" required>
                                    </div>
                                    <div class="form-group">
                                        <label for="reason">Reason</label>
                                        <textarea class="form-control" id="reason" rows="3" 
                                                  placeholder="Enter reason for stock ${action}" required></textarea>
                                    </div>
                                </form>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                                <button type="button" class="btn btn-primary" onclick="InventoryManager.processAdjustment('${action}', ${itemId})">
                                    ${title}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            $('#quantityModal').remove();
            $('body').append(modalHtml);
            $('#quantityModal').modal('show');
        },

        processAdjustment: function(action, itemId) {
            const quantity = $('#quantity').val();
            const reason = $('#reason').val();

            if (!quantity || quantity <= 0) {
                App.showToast('Please enter a valid quantity', 'error');
                return;
            }

            if (!reason.trim()) {
                App.showToast('Please enter a reason for the adjustment', 'error');
                return;
            }

            const data = {
                itemId: itemId,
                quantity: parseFloat(quantity),
                reason: reason,
                action: action
            };

            App.showLoading('#quantityModal .btn-primary');

            $.ajax({
                url: '/Inventory/AdjustStock',
                method: 'POST',
                data: JSON.stringify(data),
                contentType: 'application/json',
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                success: function(response) {
                    if (response.success) {
                        App.showToast(`Stock ${action}ed successfully`, 'success');
                        $('#quantityModal').modal('hide');
                        
                        // Update the specific row instead of full page reload
                        InventoryManager.updateInventoryRow(itemId, response.newQuantity, response.stockStatus);
                    } else {
                        App.showToast(response.message || 'Operation failed', 'error');
                    }
                },
                error: function(xhr, status, error) {
                    App.showToast('An error occurred. Please try again.', 'error');
                    console.error('Inventory adjustment error:', error);
                },
                complete: function() {
                    App.hideLoading('#quantityModal .btn-primary');
                }
            });
        },

        updateInventoryRow: function(itemId, newQuantity, stockStatus) {
            const $row = $(`.inventory-table tbody tr[data-item-id="${itemId}"]`);
            if ($row.length) {
                $row.find('.available-quantity').text(newQuantity);
                $row.find('.stock-status')
                    .removeClass('badge-success badge-warning badge-danger')
                    .addClass(stockStatus === 'In Stock' ? 'badge-success' : 
                             stockStatus === 'Low Stock' ? 'badge-warning' : 'badge-danger')
                    .text(stockStatus);
            }
        }
    };

    // ==========================================================================
    // ORDER MANAGEMENT
    // ==========================================================================
    const OrderManager = {
        init: function() {
            this.setupStatusChange();
            this.setupInvoiceGeneration();
        },

        setupStatusChange: function() {
            window.changeOrderStatus = this.changeStatus.bind(this);
        },

        setupInvoiceGeneration: function() {
            window.generateInvoice = this.generateInvoice.bind(this);
        },

        changeStatus: function(orderId, currentStatus) {
            const statuses = ['Pending', 'Processing', 'Completed', 'Delivered', 'Cancelled'];
            const options = statuses.map(status => 
                `<option value="${status}" ${status === currentStatus ? 'selected' : ''}>${status}</option>`
            ).join('');

            const modalHtml = `
                <div class="modal fade" id="statusModal" tabindex="-1" role="dialog">
                    <div class="modal-dialog" role="document">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">Change Order Status</h5>
                                <button type="button" class="close" data-dismiss="modal">
                                    <span>&times;</span>
                                </button>
                            </div>
                            <div class="modal-body">
                                <div class="form-group">
                                    <label for="newStatus">New Status</label>
                                    <select class="form-control" id="newStatus">
                                        ${options}
                                    </select>
                                </div>
                                <div class="alert alert-info">
                                    <small>Current status: <strong>${currentStatus}</strong></small>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                                <button type="button" class="btn btn-primary" onclick="OrderManager.updateStatus(${orderId})">
                                    Update Status
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            $('#statusModal').remove();
            $('body').append(modalHtml);
            $('#statusModal').modal('show');
        },

        updateStatus: function(orderId) {
            const newStatus = $('#newStatus').val();
            
            App.showLoading('#statusModal .btn-primary');

            $.ajax({
                url: `/Orders/ChangeStatus/${orderId}`,
                method: 'POST',
                data: JSON.stringify({ status: newStatus }),
                contentType: 'application/json',
                success: function(response) {
                    if (response.success) {
                        App.showToast('Status updated successfully', 'success');
                        $('#statusModal').modal('hide');
                        setTimeout(() => location.reload(), 1000);
                    } else {
                        App.showToast(response.message || 'Update failed', 'error');
                    }
                },
                error: function() {
                    App.showToast('An error occurred. Please try again.', 'error');
                },
                complete: function() {
                    App.hideLoading('#statusModal .btn-primary');
                }
            });
        },

        generateInvoice: function(orderId) {
            App.showToast('Generating invoice...', 'info');
            window.open(`/Orders/DownloadInvoice/${orderId}`, '_blank');
        },

        // Quick export functionality for reports
        quickExport: function(reportType, format = 'pdf') {
            const button = window.event?.target || document.activeElement;
            const originalText = button?.innerHTML;
            
            if (button) {
                button.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Generating...';
                button.disabled = true;
            }

            // Use fetch API for better error handling
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

                // Create download link
                const url = window.URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.download = `${reportType}_report_${new Date().getTime()}.pdf`;
                link.style.display = 'none';
                
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                
                // Clean up the URL object
                window.URL.revokeObjectURL(url);

                // Show success message
                App.showToast(`${reportType.charAt(0).toUpperCase() + reportType.slice(1)} report downloaded successfully!`, 'success');
            })
            .catch(error => {
                console.error('Export failed:', error);
                App.showToast(`Failed to download ${reportType} report: ${error.message}`, 'error');
            })
            .finally(() => {
                // Restore button
                if (button) {
                    setTimeout(() => {
                        button.innerHTML = originalText;
                        button.disabled = false;
                    }, 1000);
                }
            });
        }
    };

    // ==========================================================================
    // CUSTOMER MANAGEMENT
    // ==========================================================================
    const CustomerManager = {
        init: function() {
            this.setupQuickActions();
        },

        setupQuickActions: function() {
            window.createOrderForCustomer = function(customerId) {
                window.location.href = `/Orders/CreateWorkflow?customerId=${customerId}`;
            };

            window.addProductForCustomer = function(customerId) {
                window.location.href = `/CustomerProducts/Create?customerId=${customerId}`;
            };
        }
    };

    // ==========================================================================
    // DOCUMENT READY
    // ==========================================================================
    $(document).ready(function() {
        // Initialize main app
        App.init();

        // Add debugging for navigation issues
        console.log('App.js: Document ready, current path:', window.location.pathname);
        
        // Debug navigation clicks
        $(document).on('click', '.nav-sidebar .nav-link', function(e) {
            const $this = $(this);
            const href = $this.attr('href');
            const dataSection = $this.attr('data-section');
            const text = $this.text().trim();
            
            console.log('Navigation click:', {
                text: text,
                href: href,
                dataSection: dataSection,
                hasSubmenu: $this.parent().find('.nav-treeview').length > 0
            });
            
            // If this is a real navigation link (not #), let it work
            if (href && href !== '#' && !dataSection) {
                console.log('Following navigation link:', href);
            }
        });

        // Initialize specific managers based on current page
        const path = window.location.pathname.toLowerCase();
        
        if (path.includes('/inventory')) {
            InventoryManager.init();
        }
        
        if (path.includes('/orders')) {
            OrderManager.init();
        }
        
        if (path.includes('/customers')) {
            CustomerManager.init();
        }

        // Make quick export globally available
        window.quickExport = function(reportType, format) {
            OrderManager.quickExport(reportType, format);
        };

        // Initialize AdminLTE components
        if (typeof AdminLTE !== 'undefined') {
            AdminLTE.init();
            console.log('AdminLTE initialized');
        } else {
            console.warn('AdminLTE not found');
        }
    });

    // Export to global scope for debugging
    window.AmplePack.App = App;
    window.AmplePack.InventoryManager = InventoryManager;
    window.AmplePack.OrderManager = OrderManager;
    window.AmplePack.CustomerManager = CustomerManager;

})(jQuery);