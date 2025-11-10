// Quote Request Form JavaScript

document.addEventListener('DOMContentLoaded', function() {
    const quoteForm = document.getElementById('quoteForm');
    
    if (quoteForm) {
        // Form submission handling
        quoteForm.addEventListener('submit', function(e) {
            const submitButton = quoteForm.querySelector('button[type="submit"]');
            
            // Disable submit button to prevent double submission
            if (submitButton) {
                submitButton.disabled = true;
                submitButton.innerHTML = '<i class="bi bi-hourglass-split me-2"></i>Submitting...';
                
                // Re-enable after 3 seconds in case of validation errors
                setTimeout(function() {
                    if (submitButton.disabled) {
                        submitButton.disabled = false;
                        submitButton.innerHTML = '<i class="bi bi-send me-2"></i>Submit Quote Request';
                    }
                }, 3000);
            }
        });
        
        // Phone number formatting
        const phoneInput = document.querySelector('input[type="tel"]');
        if (phoneInput) {
            phoneInput.addEventListener('input', function(e) {
                let value = e.target.value.replace(/\D/g, '');
                
                if (value.length > 0) {
                    if (value.length <= 3) {
                        e.target.value = '(' + value;
                    } else if (value.length <= 6) {
                        e.target.value = '(' + value.slice(0, 3) + ') ' + value.slice(3);
                    } else {
                        e.target.value = '(' + value.slice(0, 3) + ') ' + value.slice(3, 6) + '-' + value.slice(6, 10);
                    }
                }
            });
        }
        
        // Character counter for project details
        const projectDetails = document.querySelector('textarea[name="ProjectDetails"]');
        if (projectDetails) {
            const maxLength = projectDetails.getAttribute('maxlength') || 3000;
            const formText = projectDetails.parentElement.querySelector('.form-text');
            
            if (formText) {
                const originalText = formText.innerHTML;
                
                projectDetails.addEventListener('input', function() {
                    const remaining = maxLength - this.value.length;
                    formText.innerHTML = originalText + ' <span class="text-muted">(' + remaining + ' characters remaining)</span>';
                    
                    if (remaining < 100) {
                        formText.classList.add('text-warning');
                    } else {
                        formText.classList.remove('text-warning');
                    }
                });
            }
        }
        
        // Service selection change handler
        const serviceSelect = document.querySelector('select[name="ServicesNeeded"]');
        if (serviceSelect) {
            serviceSelect.addEventListener('change', function() {
                // Could add dynamic field visibility based on service type
                // For now, just visual feedback
                if (this.value) {
                    this.classList.add('border-success');
                } else {
                    this.classList.remove('border-success');
                }
            });
        }
        
        // Budget range selection
        const budgetSelect = document.querySelector('select[name="Budget"]');
        if (budgetSelect) {
            budgetSelect.addEventListener('change', function() {
                if (this.value) {
                    this.classList.add('border-success');
                } else {
                    this.classList.remove('border-success');
                }
            });
        }
        
        // Auto-scroll to first validation error
        const handleValidationErrors = function() {
            const firstError = document.querySelector('.text-danger:not(:empty)');
            if (firstError) {
                const errorElement = firstError.closest('.col-12, .col-md-6');
                if (errorElement) {
                    errorElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            }
        };
        
        // Check for validation errors on page load
        if (document.querySelector('.text-danger:not(:empty)')) {
            handleValidationErrors();
        }
    }
    
    // FAQ accordion tracking (optional analytics)
    const faqButtons = document.querySelectorAll('.quote-faq .accordion-button');
    faqButtons.forEach(function(button) {
        button.addEventListener('click', function() {
            const questionText = this.textContent.trim();
            console.log('FAQ clicked:', questionText);
            // Could send to analytics here
        });
    });
    
    // Smooth scroll for call-to-action links
    const ctaLinks = document.querySelectorAll('.quote-cta-card a[href^="tel:"], .quote-cta-card a[href^="mailto:"]');
    ctaLinks.forEach(function(link) {
        link.addEventListener('click', function() {
            console.log('CTA clicked:', this.href);
            // Could track conversion events here
        });
    });
    
    // Add visual feedback for required fields
    const requiredInputs = document.querySelectorAll('input[required], select[required], textarea[required]');
    requiredInputs.forEach(function(input) {
        input.addEventListener('blur', function() {
            if (!this.value) {
                this.classList.add('border-warning');
            } else {
                this.classList.remove('border-warning');
                this.classList.add('border-success');
            }
        });
        
        input.addEventListener('focus', function() {
            this.classList.remove('border-warning');
        });
    });
});
