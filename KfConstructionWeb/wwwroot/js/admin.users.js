// User Profile Management - Admin Area
(function() {
    // Enhanced delete confirmation
    window.confirmUserDeletion = function(userEmail) {
        const confirmText = `DELETE ${userEmail}`;
        const userInput = prompt(
            `This will permanently delete the user account for "${userEmail}" and all associated data.\n\n` +
            `This action cannot be undone!\n\n` +
            `To confirm, type exactly: ${confirmText}`
        );
        
        if (userInput === confirmText) {
            return confirm(`Are you absolutely sure you want to delete "${userEmail}"? This is your final confirmation.`);
        } else if (userInput !== null) {
            alert('Confirmation text did not match. User deletion cancelled.');
        }
        
        return false;
    };

    // Password reset confirmation
    window.confirmPasswordReset = function() {
        const password = document.getElementById('newPassword').value;
        const confirmPassword = document.getElementById('confirmPassword').value;
        const forceChange = document.getElementById('forcePasswordChange');
        const userEmail = document.getElementById('userEmail') ? document.getElementById('userEmail').value : '';

        // Check if passwords match
        if (password !== confirmPassword) {
            alert('Passwords do not match. Please re-enter matching passwords.');
            return false;
        }

        // Check minimum length
        if (password.length < 6) {
            alert('Password must be at least 6 characters long.');
            return false;
        }

        // Confirm action
        const forceChangeText = forceChange && forceChange.checked 
            ? ' The user will be forced to change their password on next login and their current session will be terminated.' 
            : '';
        return confirm(`Are you sure you want to reset the password for ${userEmail}?${forceChangeText}`);
    };

    // Initialize on page load
    document.addEventListener('DOMContentLoaded', function() {
        // Add tooltips to buttons
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        const tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });

        // Real-time password match validation
        const confirmPasswordInput = document.getElementById('confirmPassword');
        if (confirmPasswordInput) {
            confirmPasswordInput.addEventListener('input', function() {
                const password = document.getElementById('newPassword').value;
                const confirmPassword = this.value;
                
                if (confirmPassword && password !== confirmPassword) {
                    this.setCustomValidity('Passwords do not match');
                    this.classList.add('is-invalid');
                    this.classList.remove('is-valid');
                } else {
                    this.setCustomValidity('');
                    this.classList.remove('is-invalid');
                    if (confirmPassword) {
                        this.classList.add('is-valid');
                    }
                }
            });
        }

        // Log page load
        const userEmailElement = document.getElementById('userEmail');
        if (userEmailElement) {
            console.log('User Profile View Loaded for:', userEmailElement.value);
        }
    });
})();
