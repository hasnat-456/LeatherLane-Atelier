// Global Application Scripts for LeatherLane Atelier

function togglePassword(inputId) {
    const input = document.getElementById(inputId);
    const button = input.nextElementSibling;
    
    if (input.type === 'password') {
        input.type = 'text';
        button.textContent = 'Hide';
    } else {
        input.type = 'password';
        button.textContent = 'Show';
    }
}

// Add smooth fade in for elements when they enter viewport if needed
document.addEventListener('DOMContentLoaded', () => {
    // Basic setup for any global interactions
});
