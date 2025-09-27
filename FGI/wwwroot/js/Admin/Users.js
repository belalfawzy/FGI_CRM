$(document).ready(function() {
    // Password validation
    function validatePassword(password) {
        const minLength = 8;
        const hasNumber = /\d/.test(password);
        const hasUpperCase = /[A-Z]/.test(password);
        const hasLowerCase = /[a-z]/.test(password);
        const hasSpecialChar = /[!@$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password);
        
        return password.length >= minLength && hasNumber && hasUpperCase && hasLowerCase && hasSpecialChar;
    }

    // Email formatting
    function formatEmail(name, role) {
        if (name && role) {
            return name.toLowerCase().replace(/\s+/g, '') + '@' + role.toLowerCase() + '.fgi';
        }
        return '';
    }

    // Auto-format email when name or role changes
    $('#userForm input[name*="FullName"]').on('input', function() {
        const name = $(this).val();
        const role = $('#userForm select[name*="Role"]').val();
        if (name && role) {
            $('#userForm input[name*="Email"]').val(formatEmail(name, role));
        }
    });

    $('#userForm select[name*="Role"]').on('change', function() {
        const role = $(this).val();
        const name = $('#userForm input[name*="FullName"]').val();
        if (name && role) {
            $('#userForm input[name*="Email"]').val(formatEmail(name, role));
        }
    });

    // Password validation on input
    $('#password').on('input', function() {
        const password = $(this).val();
        const isValid = validatePassword(password);
        
        if (password.length > 0) {
            if (isValid) {
                $(this).removeClass('is-invalid').addClass('is-valid');
                $(this).next('.invalid-feedback').remove();
            } else {
                $(this).removeClass('is-valid').addClass('is-invalid');
                if (!$(this).next('.invalid-feedback').length) {
                    $(this).after('<div class="invalid-feedback">Password must be at least 8 characters with 1 number, 1 uppercase, 1 lowercase, and 1 special character.</div>');
                }
            }
        } else {
            $(this).removeClass('is-valid is-invalid');
            $(this).next('.invalid-feedback').remove();
        }
    });

    // Form validation
    $('#userForm').on('submit', function(e) {
        const password = $('#password').val();
        if (password && !validatePassword(password)) {
            e.preventDefault();
            if (window.toastNotification) {
                window.toastNotification.error('Password must be at least 8 characters with 1 number, 1 uppercase, 1 lowercase, and 1 special character.');
            }
            return false;
        }
    });

    // Toggle password visibility
    $('#togglePassword').on('click', function() {
        const passwordField = $('#password');
        const type = passwordField.attr('type') === 'password' ? 'text' : 'password';
        passwordField.attr('type', type);
        $(this).find('i').toggleClass('fa-eye fa-eye-slash');
    });
});
