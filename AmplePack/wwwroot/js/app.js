/**
 * AmplePack Application JavaScript - Clean Version
 * Compatible with AdminLTE 3.2 and jQuery
 */

(function($) {
    'use strict';

    // ==========================================================================
    // MAIN APPLICATION
    // ==========================================================================
    const App = {
        // Configuration
        config: {
            toast: {
                timer: 3000,
                position: 'top-end',
                showConfirmButton: false,
                toast: true
            }
        },

        // ==========================================================================
        // INITIALIZATION
        // ==========================================================================
        init: function() {
            this.setupCSRF();
            this.setupNavigation();
            this.setupModals();
            this.setupTooltips();
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
        // NAVIGATION SETUP - SIMPLIFIED AND WORKING
        // ==========================================================================
        setupNavigation: function() {
            // Let AdminLTE handle the treeview functionality
            // We only need to set the active states correctly
            
            // Wait for AdminLTE to initialize, then set active navigation
            setTimeout(() => {
                this.setActiveNavigation();
            }, 100);
        },

        setActiveNavigation: function() {
            const currentPath = window.location.pathname.toLowerCase();
            const segments = currentPath.split('/').filter(s => s.length > 0);
            const currentController = segments.length > 0 ? segments[0] : 'home';
            const currentAction = segments.length > 1 ? segments[1] : 'index';
            
            console.log('Setting active navigation for:', currentController, currentAction);
            
            // Remove all active classes first
            $('.nav-sidebar .nav-link').removeClass('active');
            $('.nav-sidebar .nav-item').removeClass('menu-open');
            
            let $activeLink = null;
            
            // Try to match using data attributes first
            $activeLink = $(`.nav-sidebar .nav-link[data-controller="${currentController}"][data-action="${currentAction}"]`);
            
            // If not found, try href matching
            if ($activeLink.length === 0) {
                $('.nav-sidebar .nav-link[href]').each(function() {
                    const $link = $(this);
                    const href = $link.attr('href');
                    
                    if (!href || href === '#') return;
                    
                    const linkPath = href.toLowerCase();
                    
                    // Handle root path
                    if (currentPath === '/' && (linkPath.includes('/home/index') || linkPath === '/')) {
                        $activeLink = $link;
                        return false;
                    }
                    
                    // Exact path match
                    if (currentPath === linkPath) {
                        $activeLink = $link;
                        return false;
                    }
                    
                    // Controller/Action match
                    if (linkPath.includes(`/${currentController}/${currentAction}`)) {
                        $activeLink = $link;
                        return false;
                    }
                    
                    // Controller match (fallback)
                    if (linkPath.includes(`/${currentController}`) && !$activeLink.length) {
                        $activeLink = $link;
                    }
                });
            }
            
            // Activate the found link
            if ($activeLink && $activeLink.length > 0) {
                console.log('Activating navigation:', $activeLink.attr('href') || $activeLink.data('controller'));
                $activeLink.addClass('active');
                
                // Check if this is a submenu item
                const $submenu = $activeLink.closest('.nav-treeview');
                if ($submenu.length > 0) {
                    // This is a submenu item, open the parent menu
                    const $parentItem = $submenu.closest('.nav-item');
                    $parentItem.addClass('menu-open');
                    
                    // Also make the parent link active
                    const $parentLink = $parentItem.children('.nav-link').first();
                    $parentLink.addClass('active');
                    
                    console.log('Opened parent menu for submenu item');
                }
            } else {
                console.log('No matching navigation item found');
            }
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
        // UTILITY FUNCTIONS
        // ==========================================================================
        showToast: function(message, type = 'info', options = {}) {
            if (typeof Swal !== 'undefined') {
                const config = $.extend({}, this.config.toast, {
                    icon: type,
                    title: message
                }, options);
                
                Swal.fire(config);
            } else {
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
        }
    };

    // ==========================================================================
    // EXPORT FUNCTIONALITY
    // ==========================================================================
    const ExportManager = {
        quickExport: function(reportType, format = 'csv') { // Default to CSV for reliability
            const button = window.event?.target || document.activeElement;
            const originalText = button?.innerHTML;
            
            if (button) {
                button.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Generating...';
                button.disabled = true;
            }

            const fileExtension = format === 'csv' ? '.csv' : '.pdf';
            const contentType = format === 'csv' ? 'text/csv' : 'application/pdf';

            fetch(`/Reports/QuickExport/${reportType}?format=${format}`, {
                method: 'GET',
                headers: {
                    'Accept': contentType,
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
                link.download = `${reportType}_report_${new Date().getTime()}${fileExtension}`;
                link.style.display = 'none';
                
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                window.URL.revokeObjectURL(url);

                App.showToast(`${reportType.charAt(0).toUpperCase() + reportType.slice(1)} report downloaded successfully!`, 'success');
            })
            .catch(error => {
                console.error('Export failed:', error);
                App.showToast(`Failed to download ${reportType} report: ${error.message}`, 'error');
            })
            .finally(() => {
                if (button) {
                    setTimeout(() => {
                        button.innerHTML = originalText;
                        button.disabled = false;
                    }, 1000);
                }
            });
        },

        // Orders-specific export function
        exportOrders: function(format = 'csv') {
            const button = window.event?.target || document.activeElement;
            const originalText = button?.innerHTML;
            
            if (button) {
                button.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Exporting...';
                button.disabled = true;
            }

            // Get current filter values from the form
            const customerFilter = document.getElementById('customerFilter')?.value || '';
            const statusFilter = document.getElementById('statusFilter')?.value || '';
            const startDate = document.getElementById('startDate')?.value || '';
            const endDate = document.getElementById('endDate')?.value || '';
            const searchTerm = document.getElementById('searchTerm')?.value || '';

            // Build query parameters
            const params = new URLSearchParams();
            if (customerFilter) params.append('customerFilter', customerFilter);
            if (statusFilter) params.append('statusFilter', statusFilter);
            if (startDate) params.append('startDate', startDate);
            if (endDate) params.append('endDate', endDate);
            if (searchTerm) params.append('searchTerm', searchTerm);
            params.append('format', format);

            const fileExtension = format === 'csv' ? '.csv' : '.pdf';
            const contentType = format === 'csv' ? 'text/csv' : 'application/pdf';

            fetch(`/Orders/Export?${params}`, {
                method: 'GET',
                headers: {
                    'Accept': contentType,
                }
            })
            .then(response => {
                // Check if this was a fallback response
                const wasFallback = response.headers.get('X-Export-Fallback');
                
                if (!response.ok) {
                    throw new Error(`Export failed with status ${response.status}`);
                }
                return response.blob().then(blob => ({ blob, wasFallback }));
            })
            .then(({ blob, wasFallback }) => {
                if (blob.size === 0) {
                    throw new Error('Generated file is empty');
                }

                const url = window.URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                
                // Adjust filename if fallback occurred
                const actualExtension = wasFallback ? '.csv' : fileExtension;
                link.download = `Orders_Export_${new Date().getTime()}${actualExtension}`;
                link.style.display = 'none';
                
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                window.URL.revokeObjectURL(url);

                // Show appropriate message
                if (wasFallback) {
                    App.showToast('PDF export failed, but CSV file was generated successfully. Please try CSV format for better reliability.', 'warning');
                } else {
                    App.showToast(`Orders exported to ${format.toUpperCase()} successfully!`, 'success');
                }
            })
            .catch(error => {
                console.error('Export failed:', error);
                App.showToast(`Failed to export orders: ${error.message}`, 'error');
                
                // If PDF failed, suggest CSV
                if (format === 'pdf') {
                    App.showConfirm('PDF export failed. Would you like to try CSV format instead?', () => {
                        ExportManager.exportOrders('csv');
                    }, {
                        title: 'Export Failed',
                        confirmText: 'Try CSV',
                        cancelText: 'Cancel'
                    });
                }
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
    };

    // ==========================================================================
    // DOCUMENT READY
    // ==========================================================================
    $(document).ready(function() {
        // Initialize AdminLTE first
        if (typeof AdminLTE !== 'undefined') {
            AdminLTE.init();
            console.log('AdminLTE initialized');
        }

        // Then initialize our app
        App.init();

        // Make export functions globally available
        window.quickExport = ExportManager.quickExport;
        window.exportOrders = ExportManager.exportOrders;

        // Global debug function
        window.debugNavigation = function() {
            console.log('Current path:', window.location.pathname);
            console.log('Active links:', $('.nav-sidebar .nav-link.active').length);
            console.log('Open menus:', $('.nav-sidebar .nav-item.menu-open').length);
        };
    });

    // Export to global scope
    window.AmplePack = {
        App: App,
        ExportManager: ExportManager
    };

})(jQuery);