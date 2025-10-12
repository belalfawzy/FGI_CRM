// Enhanced interactive effects for modern home page
$(document).ready(function() {
    // Only run on home page to prevent conflicts
    if (!$('.home-page').length) return;

    // Modern card hover effects
    $('.home-page .modern-dashboard-card').hover(
        function() {
            $(this).addClass('modern-card-hover');
            $(this).find('.modern-dashboard-card-icon').addClass('modern-icon-hover');
        },
        function() {
            $(this).removeClass('modern-card-hover');
            $(this).find('.modern-dashboard-card-icon').removeClass('modern-icon-hover');
        }
    );

    // Enhanced accessibility with focus states
    $('.modern-dashboard-card').on('focus', function() {
        $(this).addClass('modern-card-focus');
    }).on('blur', function() {
        $(this).removeClass('modern-card-focus');
    });

    // Smooth scroll for anchor links
    $('.modern-dashboard-card[href^="#"]').on('click', function(e) {
        e.preventDefault();
        const target = $(this).attr('href');
        $('html, body').animate({
            scrollTop: $(target).offset().top - 100
        }, 500);
    });

    // Typing effect for greeting (optional enhancement)
    const greetingText = $('.modern-page-title').text();
    if (greetingText.includes('Good')) {
        $('.modern-page-title').addClass('typing-effect');
    }



    // Card entrance animations with intersection observer
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-in');
            }
        });
    }, observerOptions);

    // Observe all dashboard cards
    $('.modern-dashboard-card').each(function() {
        observer.observe(this);
    });

    // Dynamic greeting icon based on time
    const hour = new Date().getHours();
    const greetingIcon = $('.modern-greeting-icon i');

    if (hour < 12) {
        greetingIcon.removeClass().addClass('fas fa-sun');
    } else if (hour < 18) {
        greetingIcon.removeClass().addClass('fas fa-cloud-sun');
    } else {
        greetingIcon.removeClass().addClass('fas fa-moon');
    }

    // Add ripple effect on card click
    $('.modern-dashboard-card').on('click', function(e) {
        const $card = $(this);
        const $ripple = $('<span class="ripple-effect"></span>');

        const x = e.pageX - $card.offset().left;
        const y = e.pageY - $card.offset().top;

        $ripple.css({
            left: x + 'px',
            top: y + 'px'
        });

        $card.append($ripple);

        setTimeout(() => {
            $ripple.remove();
        }, 600);
    });
});
