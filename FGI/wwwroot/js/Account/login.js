// Toggle password visibility
document.getElementById('togglePassword').addEventListener('click', function() {
    const passwordInput = document.getElementById('password');
    const icon = this.querySelector('i');

    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        icon.classList.replace('fa-eye', 'fa-eye-slash');
    } else {
        passwordInput.type = 'password';
        icon.classList.replace('fa-eye-slash', 'fa-eye');
    }
});

// Form validation
document.getElementById('loginForm').addEventListener('submit', function(e) {
    // Client-side validation only
    let isValid = true;
    const email = document.getElementById('email');
    const password = document.getElementById('password');
    const errorAlert = document.getElementById('errorAlert');
    const errorList = document.getElementById('errorList');
    
    // Hide previous error messages
    errorAlert.style.display = 'none';
    errorList.innerHTML = '';
    
    // Email validation
    if (!email.value || !/\S+@\S+\.\S+/.test(email.value)) {
        isValid = false;
        const li = document.createElement('li');
        li.textContent = 'Please enter a valid email address';
        errorList.appendChild(li);
    }
    
    // Password validation
    if (!password.value || password.value.length < 6) {
        isValid = false;
        const li = document.createElement('li');
        li.textContent = 'Password must be at least 6 characters';
        errorList.appendChild(li);
    }
    
    // Show errors if any
    if (!isValid) {
        e.preventDefault();
        errorAlert.style.display = 'block';
    }
    
    // If form is valid, it will continue to submit to server
    // where real login processing happens
});