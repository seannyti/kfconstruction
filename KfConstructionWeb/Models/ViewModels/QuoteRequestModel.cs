using System.ComponentModel.DataAnnotations;

namespace KfConstructionWeb.Models.ViewModels
{
    public class QuoteRequestModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [RegularExpression(@"^(\+?1[-.\s]?)?(\([0-9]{3}\)|[0-9]{3})[-.\s]?[0-9]{3}[-.\s]?[0-9]{4}$", 
            ErrorMessage = "Phone number format: (555) 123-4567 or 555-123-4567")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters")]
        [Display(Name = "Company (Optional)")]
        public string? Company { get; set; }

        [Required(ErrorMessage = "Please select a service")]
        [Display(Name = "Services Needed")]
        public string ServicesNeeded { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Budget cannot exceed 50 characters")]
        [Display(Name = "Budget Range (Optional)")]
        public string? Budget { get; set; }

        [Required(ErrorMessage = "Project details are required")]
        [StringLength(3000, ErrorMessage = "Project details cannot exceed 3000 characters")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Project Details")]
        public string ProjectDetails { get; set; } = string.Empty;

        // Simple honeypot field for spam protection
        [Display(Name = "Leave this field empty")]
        public string? HoneyPot { get; set; }
    }
}
