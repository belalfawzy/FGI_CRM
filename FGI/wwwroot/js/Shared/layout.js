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
        if ($(window).width() < 992) {
            $('.navbar-collapse').collapse('hide');
        }
    });

    // Enhanced dropdown interaction for mobile and desktop
    $('.user-dropdown').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        
        const dropdown = $(this).next('.dropdown-menu');
        const isVisible = dropdown.hasClass('show');
        
        // Close all other dropdowns first
        $('.dropdown-menu').removeClass('show');
        
        // Toggle current dropdown
        if (!isVisible) {
            dropdown.addClass('show');
            
            // For mobile, ensure dropdown is visible
            if ($(window).width() <= 991.98) {
                dropdown.css({
                    'display': 'block',
                    'position': 'static',
                    'width': '100%',
                    'margin-top': '0.5rem'
                });
            }
        }
    });
    
    // Close dropdown when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.user-dropdown, .dropdown-menu').length) {
            $('.dropdown-menu').removeClass('show');
        }
    });
    
    // Close dropdown when clicking on dropdown items
    $('.dropdown-item').on('click', function () {
        $('.dropdown-menu').removeClass('show');
    });
    
    // Handle window resize for dropdown positioning
    $(window).on('resize', function() {
        if ($(window).width() > 991.98) {
            // Desktop - reset dropdown positioning
            $('.dropdown-menu').css({
                'position': 'absolute',
                'width': 'auto',
                'margin-top': '0.125rem'
            });
        } else {
            // Mobile - ensure proper positioning
            $('.dropdown-menu').css({
                'position': 'static',
                'width': '100%',
                'margin-top': '0.5rem'
            });
        }
    });

    // Handle responsive behavior
    function handleResize() {
        if ($(window).width() < 992) {
            $('.navbar-nav').addClass('mobile-menu');
        } else {
            $('.navbar-nav').removeClass('mobile-menu');
        }
    }

    // Initial call and debounced resize
    handleResize();
    let resizeTimer;
    $(window).resize(function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(handleResize, 250);
    });
});