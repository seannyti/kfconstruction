// Testimonials Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Initialize testimonial animations
    initTestimonialAnimations();
    
    // Initialize rating interactions
    initRatingInteractions();
    
    // Initialize submit form features
    initSubmitForm();
});

// Animate testimonial cards on scroll
function initTestimonialAnimations() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
            }
        });
    }, observerOptions);

    // Add animate class and observe testimonial cards
    document.querySelectorAll('.testimonial-card').forEach(card => {
        card.classList.add('animate-in');
        observer.observe(card);
    });
}

// Initialize rating star interactions
function initRatingInteractions() {
    document.querySelectorAll('.rating-stars.interactive').forEach(ratingContainer => {
        const stars = ratingContainer.querySelectorAll('.star');
        
        stars.forEach((star, index) => {
            star.addEventListener('click', function() {
                const rating = index + 1;
                updateRating(ratingContainer, rating);
            });
            
            star.addEventListener('mouseenter', function() {
                highlightStars(stars, index + 1);
            });
        });
        
        ratingContainer.addEventListener('mouseleave', function() {
            const currentRating = parseInt(ratingContainer.dataset.rating) || 0;
            highlightStars(stars, currentRating);
        });
    });
}

// Initialize submit form functionality
function initSubmitForm() {
    // Set timestamp when form loads (for bot detection)
    const timestampInput = document.getElementById('timestamp');
    if (timestampInput) {
        timestampInput.value = Date.now();
    }
    
    // Character counter for content textarea
    initCharacterCounter();
    
    // Enhanced rating interaction
    initSubmitRatingStars();
    
    // Form validation
    initFormValidation();
    
    // Real-time content validation
    initContentValidation();
}

// Initialize character counter
function initCharacterCounter() {
    const contentTextarea = document.querySelector('textarea[name="Content"]');
    const charCountSpan = document.getElementById('char-count');

    if (contentTextarea && charCountSpan) {
        function updateCharCount() {
            const count = contentTextarea.value.length;
            charCountSpan.textContent = count;
            
            charCountSpan.className = 'char-count';
            if (count > 2000) {
                charCountSpan.classList.add('danger');
            } else if (count > 1800) {
                charCountSpan.classList.add('warning');
            }
        }

        contentTextarea.addEventListener('input', updateCharCount);
        updateCharCount(); // Initial count
    }
}

// Initialize submit form rating stars
function initSubmitRatingStars() {
    const ratingStars = document.querySelectorAll('.rating-star');
    const ratingRadios = document.querySelectorAll('.rating-radio');

    ratingStars.forEach((star, index) => {
        star.addEventListener('mouseenter', function() {
            // Highlight stars on hover
            for (let i = ratingStars.length - 1; i >= index; i--) {
                ratingStars[i].style.color = '#ffc107';
            }
        });

        star.addEventListener('mouseleave', function() {
            // Reset to selected state
            const checkedRadio = document.querySelector('.rating-radio:checked');
            const checkedIndex = checkedRadio ? Array.from(ratingRadios).indexOf(checkedRadio) : -1;
            
            ratingStars.forEach((s, i) => {
                if (checkedIndex >= 0 && i >= ratingStars.length - 1 - checkedIndex) {
                    s.style.color = '#ffc107';
                } else {
                    s.style.color = '#ddd';
                }
            });
        });
    });
}

// Initialize form validation
function initFormValidation() {
    const form = document.getElementById('testimonialForm');
    if (form) {
        form.addEventListener('submit', function(e) {
            // Rating validation
            const rating = document.querySelector('.rating-radio:checked');
            if (!rating) {
                e.preventDefault();
                alert('Please select a rating before submitting your testimonial.');
                return false;
            }

            // Content quality check (client-side hint)
            const contentTextarea = document.querySelector('textarea[name="Content"]');
            const content = contentTextarea.value.trim();
            if (content.length < 50) {
                if (!confirm('Your testimonial seems quite short. Are you sure you want to submit it as is?')) {
                    e.preventDefault();
                    return false;
                }
            }

            // Prevent double submission
            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Submitting...';
                
                // Re-enable after 10 seconds in case of error
                setTimeout(() => {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = '<i class="fas fa-paper-plane me-2"></i>Submit Testimonial';
                }, 10000);
            }
        });
    }
}

// Initialize content validation hints
function initContentValidation() {
    const contentTextarea = document.querySelector('textarea[name="Content"]');
    if (contentTextarea) {
        contentTextarea.addEventListener('blur', function() {
            const content = this.value.trim();
            const words = content.split(/\s+/).filter(w => w.length > 2);
            
            // Remove any existing hints
            const existingHint = document.getElementById('content-hint');
            if (existingHint) {
                existingHint.remove();
            }

            if (words.length < 8 && content.length > 0) {
                const hint = document.createElement('div');
                hint.id = 'content-hint';
                hint.className = 'form-text text-warning content-hint';
                hint.innerHTML = '<i class="fas fa-lightbulb me-1"></i>Consider adding more specific details about your experience for a more helpful testimonial.';
                this.parentNode.appendChild(hint);
            }
        });
    }
}

// Update rating value
function updateRating(container, rating) {
    container.dataset.rating = rating;
    const hiddenInput = container.parentElement.querySelector('input[type="hidden"]');
    if (hiddenInput) {
        hiddenInput.value = rating;
    }
    
    const stars = container.querySelectorAll('.star');
    highlightStars(stars, rating);
}

// Highlight stars up to the specified count
function highlightStars(stars, count) {
    stars.forEach((star, index) => {
        if (index < count) {
            star.classList.add('active');
            star.innerHTML = '★';
        } else {
            star.classList.remove('active');
            star.innerHTML = '☆';
        }
    });
}

// Smooth scroll to testimonials section
function scrollToTestimonials() {
    const testimonialsSection = document.querySelector('#testimonials-section');
    if (testimonialsSection) {
        testimonialsSection.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });
    }
}

// Filter testimonials by service type
function filterTestimonials(serviceType) {
    const url = new URL(window.location);
    if (serviceType && serviceType !== 'all') {
        url.searchParams.set('serviceType', serviceType);
    } else {
        url.searchParams.delete('serviceType');
    }
    window.location = url;
}

// Management page functionality
function initManagementPage() {
    // Handle display order updates
    const displayOrderInputs = document.querySelectorAll('.display-order-input');
    
    displayOrderInputs.forEach(input => {
        let originalValue = input.value;
        
        input.addEventListener('blur', function() {
            const newValue = this.value;
            const testimonialId = this.dataset.testimonialId;
            
            if (newValue !== originalValue) {
                updateDisplayOrder(testimonialId, newValue)
                    .then(success => {
                        if (success) {
                            originalValue = newValue;
                            this.style.borderColor = '#28a745';
                            setTimeout(() => {
                                this.style.borderColor = '';
                            }, 2000);
                        } else {
                            this.value = originalValue;
                            this.style.borderColor = '#dc3545';
                            setTimeout(() => {
                                this.style.borderColor = '';
                            }, 2000);
                        }
                    });
            }
        });
        
        input.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                this.blur();
            }
        });
    });
}

// Update display order via AJAX
async function updateDisplayOrder(id, displayOrder) {
    try {
        const response = await fetch('/Testimonials/UpdateDisplayOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: `id=${id}&displayOrder=${displayOrder}`
        });
        
        const result = await response.json();
        return result.success;
    } catch (error) {
        console.error('Error updating display order:', error);
        return false;
    }
}

// Initialize management page if we're on it
if (window.location.pathname.includes('/Manage')) {
    document.addEventListener('DOMContentLoaded', initManagementPage);
}