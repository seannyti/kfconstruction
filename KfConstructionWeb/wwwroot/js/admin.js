// Admin area JavaScript functionality

document.addEventListener('DOMContentLoaded', function() {
    // Force dropdown positioning on show
    const dropdownToggles = document.querySelectorAll('[data-bs-toggle="dropdown"]');
    
    dropdownToggles.forEach(function(toggle) {
        toggle.addEventListener('show.bs.dropdown', function(e) {
            // Add viewport boundary constraint
            this.setAttribute('data-bs-boundary', 'viewport');
            
            // Increase z-index of parent row when dropdown opens
            const parentRow = this.closest('tr');
            if (parentRow) {
                parentRow.style.zIndex = '1056';
                parentRow.style.position = 'relative';
            }
        });
        
        toggle.addEventListener('shown.bs.dropdown', function(e) {
            const dropdownMenu = this.nextElementSibling;
            const rect = dropdownMenu.getBoundingClientRect();
            const viewportWidth = window.innerWidth;
            
            // Force repositioning if extending beyond viewport
            if (rect.right > viewportWidth - 10) {
                // Switch to left-aligned dropdown
                dropdownMenu.classList.remove('dropdown-menu-end');
                dropdownMenu.classList.add('dropdown-menu-start');
                dropdownMenu.style.right = 'auto';
                dropdownMenu.style.left = '0';
                dropdownMenu.style.transform = 'none';
            }
        });
        
        // Reset on hide
        toggle.addEventListener('hidden.bs.dropdown', function(e) {
            const dropdownMenu = this.nextElementSibling;
            dropdownMenu.classList.remove('dropdown-menu-start');
            dropdownMenu.classList.add('dropdown-menu-end');
            dropdownMenu.style.right = '';
            dropdownMenu.style.left = '';
            dropdownMenu.style.transform = '';
            
            // Reset z-index of parent row when dropdown closes
            const parentRow = this.closest('tr');
            if (parentRow) {
                parentRow.style.zIndex = '';
                parentRow.style.position = '';
            }
        });
    });
});