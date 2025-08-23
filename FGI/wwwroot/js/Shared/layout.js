$(document).ready(function () {
    // Add active class to current page nav item
    const current = location.pathname;
    $('#navbarNav .nav-link').each(function () {
        const $this = $(this);
        if ($this.attr('href') === current || $this.attr('href').startsWith(current)) {
            $this.addClass('active');
        }
    });

    // Smooth scrolling for anchor links
    $('a[href*="#"]').on('click', function (e) {
        if (this.pathname === window.location.pathname && this.hash) {
            e.preventDefault();
            const target = $(this.hash);
            if (target.length) {
                $('html, body').animate({
                    scrollTop: target.offset().top - 80
                }, 600, 'easeInOutQuad');
            }
        }
    });

    // Close mobile menu on link click
    $('.navbar-nav .nav-link').on('click', function () {
        $('.navbar-collapse').collapse('hide');
    });

    // Improved dropdown interaction
    $('.user-dropdown').on('click', function (e) {
        e.stopPropagation();
    });

    // Handle responsive behavior
    function handleResize() {
        if ($(window).width() < 992) {
            $('.navbar-nav').addClass('mobile-menu');
        } else {
            $('.navbar-nav').removeClass('mobile-menu');
        }
    }

    // Initial call and on resize
    handleResize();
    $(window).resize(handleResize);
});