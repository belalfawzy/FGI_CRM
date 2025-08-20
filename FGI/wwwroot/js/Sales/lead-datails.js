
$(document).ready(function () {
    // Handle alert dismissal (already managed by Bootstrap, but included for completeness)
    $('.alert .btn-close').on('click', function () {
        $(this).closest('.alert').alert('close');
    });
});