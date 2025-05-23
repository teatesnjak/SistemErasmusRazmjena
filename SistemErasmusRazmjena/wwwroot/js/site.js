// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Debug link clicks
document.addEventListener('DOMContentLoaded', function() {
    // Find all links to ErasmusProgram/Details
    const programDetailLinks = document.querySelectorAll('a[href*="ErasmusProgram/Details"]');
    
    programDetailLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            console.log('Link clicked:', this.getAttribute('href'));
            
            // This prevents issues with empty or malformed URLs
            if (!this.getAttribute('href') || this.getAttribute('href') === '#') {
                e.preventDefault();
                console.error('Invalid href attribute');
            }
        });
    });
});
