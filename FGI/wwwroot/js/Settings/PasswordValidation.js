// Password validation function
function validatePassword(password) {
    const minLength = 8;
    const hasNumber = /\d/.test(password);
    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasSpecialChar = /[!@$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password);
    
    return password.length >= minLength && hasNumber && hasUpperCase && hasLowerCase && hasSpecialChar;
}

// Password validation on input
document.getElementById('newPassword').addEventListener('input', function() {
    const password = this.value;
    const isValid = validatePassword(password);
    
    if (password.length > 0) {
        if (isValid) {
            this.classList.remove('is-invalid');
            this.classList.add('is-valid');
            // Remove existing feedback
            const existingFeedback = this.nextElementSibling;
            if (existingFeedback && existingFeedback.classList.contains('invalid-feedback')) {
                existingFeedback.remove();
            }
        } else {
            this.classList.remove('is-valid');
            this.classList.add('is-invalid');
            // Add feedback if not exists
            if (!this.nextElementSibling || !this.nextElementSibling.classList.contains('invalid-feedback')) {
                const feedback = document.createElement('div');
                feedback.className = 'invalid-feedback';
                feedback.textContent = 'Password must be at least 8 characters with 1 number, 1 uppercase, 1 lowercase, and 1 special character.';
                this.parentNode.appendChild(feedback);
            }
        }
    } else {
        this.classList.remove('is-valid', 'is-invalid');
        const existingFeedback = this.nextElementSibling;
        if (existingFeedback && existingFeedback.classList.contains('invalid-feedback')) {
            existingFeedback.remove();
        }
    }
});
