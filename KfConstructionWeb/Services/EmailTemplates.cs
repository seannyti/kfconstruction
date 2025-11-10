using KfConstructionWeb.Models;

namespace KfConstructionWeb.Services;

/// <summary>
/// Email template generation methods for EmailSender
/// </summary>
public partial class EmailSender
{
    #region Email Template Generation

    private string GenerateTestimonialNotificationHtml(Testimonial testimonial)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>New Testimonial Notification</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .testimonial {{ background-color: white; padding: 15px; border-left: 4px solid #3498db; margin: 10px 0; }}
        .rating {{ color: #f39c12; font-size: 1.2em; }}
        .button {{ display: inline-block; padding: 10px 20px; background-color: #3498db; color: white; text-decoration: none; border-radius: 5px; margin: 10px 5px; }}
        .footer {{ text-align: center; color: #666; font-size: 0.9em; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏗️ KF Construction</h1>
            <h2>New Testimonial Submitted</h2>
        </div>
        
        <div class='content'>
            <p><strong>A new testimonial has been submitted and requires your review.</strong></p>
            
            <div class='testimonial'>
                <h3>📝 Testimonial Details</h3>
                <p><strong>Name:</strong> {testimonial.DisplayName}</p>
                <p><strong>Email:</strong> {testimonial.Email}</p>
                <p><strong>Rating:</strong> <span class='rating'>{new string('★', testimonial.Rating)}{new string('☆', 5 - testimonial.Rating)}</span> ({testimonial.Rating}/5)</p>
                {(testimonial.ServiceType.HasValue ? $"<p><strong>Service Type:</strong> {testimonial.ServiceType.Value.ToString().Replace("_", " ")}</p>" : "")}
                {(!string.IsNullOrEmpty(testimonial.Location) ? $"<p><strong>Location:</strong> {testimonial.Location}</p>" : "")}
                
                <h4>💬 Testimonial Content:</h4>
                <blockquote style='margin: 10px 0; padding: 10px; background-color: #ecf0f1; border-left: 3px solid #3498db;'>
                    {testimonial.Content}
                </blockquote>
                
                <p><strong>Submitted:</strong> {testimonial.SubmittedAt:MMMM dd, yyyy 'at' h:mm tt}</p>
                <p><strong>IP Address:</strong> {testimonial.IPAddress}</p>
            </div>
            
            <div style='text-align: center; margin: 20px 0;'>
                <a href='https://your-domain.com/testimonials/manage' class='button'>📋 Review in Admin Panel</a>
                <a href='https://your-domain.com/testimonials/details/{testimonial.Id}' class='button'>👁️ View Details</a>
            </div>
            
            <p><em>Please review this testimonial and take appropriate action (approve, reject, or flag for further review).</em></p>
        </div>
        
        <div class='footer'>
            <p>This is an automated notification from KF Construction Admin System</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateTestimonialConfirmationHtml(string name)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Thank You for Your Testimonial</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #27ae60; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .highlight {{ background-color: #e8f5e8; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .button {{ display: inline-block; padding: 12px 25px; background-color: #27ae60; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .footer {{ text-align: center; color: #666; font-size: 0.9em; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏗️ KF Construction</h1>
            <h2>Thank You!</h2>
        </div>
        
        <div class='content'>
            <p>Dear {name},</p>
            
            <div class='highlight'>
                <h3>🙏 We appreciate your feedback!</h3>
                <p>Thank you for taking the time to share your experience with KF Construction. Your testimonial helps us improve our services and assists future clients in making informed decisions.</p>
            </div>
            
            <h3>📋 What happens next?</h3>
            <ul>
                <li><strong>Review Process:</strong> Our team will review your testimonial to ensure it meets our community guidelines.</li>
                <li><strong>Publication:</strong> Once approved, your testimonial will be featured on our website.</li>
                <li><strong>Timeline:</strong> This process typically takes 1-2 business days.</li>
            </ul>
            
            <div style='text-align: center; margin: 20px 0;'>
                <a href='https://your-domain.com/testimonials' class='button'>🌟 View All Testimonials</a>
            </div>
            
            <h3>🤝 Stay Connected</h3>
            <p>Follow us on social media for updates on our latest projects and construction tips:</p>
            <ul>
                <li>📘 Facebook: KF Construction</li>
                <li>📸 Instagram: @kfconstruction</li>
                <li>💼 LinkedIn: KF Construction LLC</li>
            </ul>
            
            <div class='highlight'>
                <p><strong>Need more work done?</strong> We'd love to help with your next project. Contact us anytime for a free consultation!</p>
                <p>📞 Phone: (555) 123-4567<br>
                   📧 Email: info@kfconstruction.com</p>
            </div>
        </div>
        
        <div class='footer'>
            <p>Best regards,<br>The KF Construction Team</p>
            <p><em>Building excellence, one project at a time.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateContactFormHtml(string name, string email, string subject, string message)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Contact Form Submission</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #e74c3c; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .contact-info {{ background-color: white; padding: 15px; border-left: 4px solid #e74c3c; margin: 10px 0; }}
        .message-box {{ background-color: #fff; padding: 15px; border: 1px solid #ddd; border-radius: 5px; margin: 15px 0; }}
        .button {{ display: inline-block; padding: 10px 20px; background-color: #e74c3c; color: white; text-decoration: none; border-radius: 5px; }}
        .footer {{ text-align: center; color: #666; font-size: 0.9em; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏗️ KF Construction</h1>
            <h2>New Contact Form Submission</h2>
        </div>
        
        <div class='content'>
            <p><strong>A new contact form has been submitted through your website.</strong></p>
            
            <div class='contact-info'>
                <h3>👤 Contact Information</h3>
                <p><strong>Name:</strong> {name}</p>
                <p><strong>Email:</strong> <a href='mailto:{email}'>{email}</a></p>
                <p><strong>Subject:</strong> {subject}</p>
                <p><strong>Submitted:</strong> {DateTime.Now:MMMM dd, yyyy 'at' h:mm tt}</p>
            </div>
            
            <div class='message-box'>
                <h3>💬 Message Content</h3>
                <p>{message.Replace("\n", "<br>")}</p>
            </div>
            
            <div style='text-align: center; margin: 20px 0;'>
                <a href='mailto:{email}?subject=Re: {subject}' class='button'>📧 Reply to Customer</a>
            </div>
            
            <p><em>Please respond to this inquiry within 24 hours to maintain excellent customer service.</em></p>
        </div>
        
        <div class='footer'>
            <p>This is an automated notification from KF Construction Website</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateWelcomeEmailHtml(string name)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome to KF Construction</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .welcome-box {{ background-color: #e8f5e8; padding: 20px; border-radius: 5px; margin: 15px 0; text-align: center; }}
        .feature {{ background-color: white; padding: 15px; margin: 10px 0; border-radius: 5px; }}
        .button {{ display: inline-block; padding: 12px 25px; background-color: #2c3e50; color: white; text-decoration: none; border-radius: 5px; margin: 10px 5px; }}
        .footer {{ text-align: center; color: #666; font-size: 0.9em; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏗️ KF Construction</h1>
            <h2>Welcome to Our Client Portal!</h2>
        </div>
        
        <div class='content'>
            <div class='welcome-box'>
                <h2>🎉 Welcome, {name}!</h2>
                <p>Thank you for choosing KF Construction. We're excited to work with you and bring your construction vision to life.</p>
            </div>
            
            <h3>🔑 Your Client Portal Access</h3>
            <p>You now have access to our exclusive client portal where you can:</p>
            
            <div class='feature'>
                <h4>📊 Track Project Progress</h4>
                <p>View real-time updates, timeline milestones, and project status.</p>
            </div>
            
            <div class='feature'>
                <h4>📸 Browse Project Gallery</h4>
                <p>See photos and videos of your project as it progresses.</p>
            </div>
            
            <div class='feature'>
                <h4>💬 Direct Communication</h4>
                <p>Message our team directly through the portal for quick responses.</p>
            </div>
            
            <div class='feature'>
                <h4>📋 Access Documents</h4>
                <p>Download contracts, permits, and project documentation.</p>
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://your-domain.com/clientportal' class='button'>🚀 Access Your Portal</a>
                <a href='https://your-domain.com/portfolio' class='button'>👁️ View Our Work</a>
            </div>
            
            <h3>📞 Contact Information</h3>
            <p>Our team is here to help you every step of the way:</p>
            <ul>
                <li><strong>Phone:</strong> (555) 123-4567</li>
                <li><strong>Email:</strong> info@kfconstruction.com</li>
                <li><strong>Office Hours:</strong> Monday - Friday, 8:00 AM - 6:00 PM</li>
                <li><strong>Emergency:</strong> (555) 123-4568</li>
            </ul>
        </div>
        
        <div class='footer'>
            <p>Welcome to the KF Construction family!<br>
               <em>Building excellence, one project at a time.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetHtml(string resetLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset - KF Construction</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #e67e22; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .security-notice {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .reset-box {{ background-color: white; padding: 20px; border-radius: 5px; text-align: center; margin: 20px 0; }}
        .button {{ display: inline-block; padding: 15px 30px; background-color: #e67e22; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ text-align: center; color: #666; font-size: 0.9em; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏗️ KF Construction</h1>
            <h2>Password Reset Request</h2>
        </div>
        
        <div class='content'>
            <p>Hello,</p>
            
            <p>We received a request to reset your password for your KF Construction account.</p>
            
            <div class='reset-box'>
                <h3>🔐 Reset Your Password</h3>
                <p>Click the button below to create a new password:</p>
                <a href='{resetLink}' class='button'>Reset My Password</a>
                <p><small>This link will expire in 24 hours for security purposes.</small></p>
            </div>
            
            <div class='security-notice'>
                <h4>🛡️ Security Notice</h4>
                <ul>
                    <li>If you didn't request this password reset, please ignore this email.</li>
                    <li>Never share your password with anyone.</li>
                    <li>This link can only be used once.</li>
                    <li>If you continue to have issues, contact our support team.</li>
                </ul>
            </div>
            
            <p><strong>Need Help?</strong><br>
               If you're having trouble clicking the button, copy and paste the following link into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 3px;'>
               {resetLink}
            </p>
            
            <p><strong>Contact Support:</strong><br>
               📞 Phone: (555) 123-4567<br>
               📧 Email: support@kfconstruction.com</p>
        </div>
        
        <div class='footer'>
            <p>KF Construction - Secure Account Management<br>
               <em>This is an automated security email.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    #endregion
}