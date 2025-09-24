// Add interactive effects
$(document).ready(function() {
    // Hover effect for cards
    $('.dashboard-card').hover(
        function() {
            $(this).addClass('shadow-lg');
        },
        function() {
            $(this).removeClass('shadow-lg');
        }
    );

    // Improve accessibility with focus states
    $('.dashboard-card').on('focus', function() {
        $(this).addClass('shadow-lg');
    }).on('blur', function() {
        $(this).removeClass('shadow-lg');
    });

    // Stagger card animations on page load
    $('.dashboard-card').each(function(index) {
        $(this).css('animation-delay', (index * 0.1) + 's');
    });
});