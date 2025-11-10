// Authentication Pages Enhanced Functionality - Optimized

document.addEventListener('DOMContentLoaded', function() {
    
    // Password strength indicator
    const passwordInput = document.querySelector('input[type="password"][id*="Password"]:not([id*="Confirm"])');
    
    if (passwordInput && !document.querySelector('[id*="Current"]')) {
        const strengthContainer = document.createElement('div');
        strengthContainer.className = 'password-strength';
        strengthContainer.innerHTML = '<div class="password-strength-bar"></div>';
        passwordInput.parentElement.appendChild(strengthContainer);
        
        const strengthBar = strengthContainer.querySelector('.password-strength-bar');
        
        // Debounce password strength calculation
        let strengthTimeout;
        passwordInput.addEventListener('input', function() {
            clearTimeout(strengthTimeout);
            strengthTimeout = setTimeout(() => {
                const password = this.value;
                const strength = calculatePasswordStrength(password);
                
                requestAnimationFrame(() => {
                    strengthBar.className = 'password-strength-bar';
                    
                    if (password.length === 0) {
                        strengthBar.style.width = '0%';
                    } else if (strength < 3) {
                        strengthBar.classList.add('strength-weak');
                    } else if (strength < 5) {
                        strengthBar.classList.add('strength-medium');
                    } else {
                        strengthBar.classList.add('strength-strong');
                    }
                });
            }, 100);
        });
    }
    
    // Form submission loading state
    const authForms = document.querySelectorAll('form[id*="account"], form[id*="register"]');
    authForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn && !submitBtn.classList.contains('btn-loading')) {
                submitBtn.classList.add('btn-loading');
                submitBtn.disabled = true;
            }
        });
    });
    
    // Add icons to input fields (only once)
    addInputIcons();
});

function calculatePasswordStrength(password) {
    let strength = 0;
    
    if (password.length >= 6) strength++;
    if (password.length >= 10) strength++;
    if (/[a-z]/.test(password) && /[A-Z]/.test(password)) strength++;
    if (/\d/.test(password)) strength++;
    if (/[^a-zA-Z0-9]/.test(password)) strength++;
    
    return strength;
}

function addInputIcons() {
    const inputs = document.querySelectorAll('.auth-form-control');
    
    inputs.forEach(input => {
        const type = input.type;
        const id = input.id.toLowerCase();
        let icon = '';
        
        if (type === 'email' || id.includes('email')) {
            icon = '📧';
        } else if (type === 'password' || id.includes('password')) {
            icon = '🔒';
        } else if (id.includes('name')) {
            icon = '👤';
        }
        
        if (icon && !input.parentElement.querySelector('.auth-input-icon')) {
            const iconSpan = document.createElement('span');
            iconSpan.className = 'auth-input-icon';
            iconSpan.textContent = icon;
            input.parentElement.insertBefore(iconSpan, input);
        }
    });
}

// Confirm password match validation (debounced)
const confirmPasswordInput = document.querySelector('input[id*="ConfirmPassword"]');
if (confirmPasswordInput) {
    const passwordInput = document.querySelector('input[id*="Password"]:not([id*="Confirm"])');
    
    let validationTimeout;
    confirmPasswordInput.addEventListener('input', function() {
        clearTimeout(validationTimeout);
        validationTimeout = setTimeout(() => {
            if (passwordInput && this.value !== '' && passwordInput.value !== this.value) {
                this.setCustomValidity('Passwords do not match');
            } else {
                this.setCustomValidity('');
            }
        }, 200);
    });
}
