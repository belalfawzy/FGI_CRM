/* Main Navbar - Namespaced JavaScript to prevent conflicts */
/* This file ensures consistent navbar behavior across all views */

(function() {
    'use strict';
    
    // Wait for DOM to be ready
    document.addEventListener('DOMContentLoaded', function() {
        initializeMainNavbar();
    });
    
    function initializeMainNavbar() {
        const navbar = document.querySelector('.main-navbar');
        if (!navbar) return;
        
        // Initialize navbar toggler
        initializeNavbarToggler();
        
        // Initialize user dropdown
        initializeUserDropdown();
        
        // Initialize active link highlighting
        initializeActiveLinks();
        
        // Initialize responsive behavior
        initializeResponsiveBehavior();
    }
    
    function initializeNavbarToggler() {
        const toggler = document.querySelector('.main-navbar .main-navbar-toggler');
        const collapse = document.querySelector('.main-navbar .main-navbar-collapse');
        
        if (!toggler || !collapse) return;
        
        toggler.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const isExpanded = collapse.classList.contains('show');
            
            if (isExpanded) {
                collapse.classList.remove('show');
                toggler.setAttribute('aria-expanded', 'false');
            } else {
                collapse.classList.add('show');
                toggler.setAttribute('aria-expanded', 'true');
            }
        });
        
        // Close mobile menu when clicking outside
        document.addEventListener('click', function(e) {
            if (window.innerWidth <= 991.98) {
                if (!toggler.contains(e.target) && !collapse.contains(e.target)) {
                    collapse.classList.remove('show');
                    toggler.setAttribute('aria-expanded', 'false');
                }
            }
        });
        
        // Close mobile menu when clicking on nav links
        const navLinks = collapse.querySelectorAll('.main-navbar-link');
        navLinks.forEach(link => {
            link.addEventListener('click', function() {
                if (window.innerWidth <= 991.98) {
                    collapse.classList.remove('show');
                    toggler.setAttribute('aria-expanded', 'false');
                }
            });
        });
    }
    
    function initializeUserDropdown() {
        const userDropdown = document.querySelector('.main-navbar .main-user-dropdown');
        const dropdownMenu = document.querySelector('.main-navbar .main-dropdown-menu');
        
        if (!userDropdown || !dropdownMenu) return;
        
        // Handle dropdown toggle
        userDropdown.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const isVisible = dropdownMenu.classList.contains('show');
            
            // Close all other dropdowns first
            closeAllDropdowns();
            
            // Toggle current dropdown
            if (!isVisible) {
                dropdownMenu.classList.add('show');
                userDropdown.setAttribute('aria-expanded', 'true');
                
                // For mobile, ensure proper positioning
                if (window.innerWidth <= 991.98) {
                    dropdownMenu.style.display = 'block';
                    dropdownMenu.style.position = 'static';
                    dropdownMenu.style.width = '100%';
                    dropdownMenu.style.marginTop = '0.5rem';
                }
            } else {
                dropdownMenu.classList.remove('show');
                userDropdown.setAttribute('aria-expanded', 'false');
            }
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!userDropdown.contains(e.target) && !dropdownMenu.contains(e.target)) {
                closeAllDropdowns();
            }
        });
        
        // Close dropdown when clicking on dropdown items
        const dropdownItems = dropdownMenu.querySelectorAll('.main-dropdown-item');
        dropdownItems.forEach(item => {
            item.addEventListener('click', function() {
                // Small delay to allow form submission or navigation
                setTimeout(() => {
                    closeAllDropdowns();
                }, 100);
            });
        });
        
        // Handle window resize for dropdown positioning
        window.addEventListener('resize', function() {
            if (window.innerWidth > 991.98) {
                // Desktop - reset dropdown positioning
                dropdownMenu.style.position = 'absolute';
                dropdownMenu.style.width = 'auto';
                dropdownMenu.style.marginTop = '0.125rem';
            } else {
                // Mobile - ensure proper positioning
                dropdownMenu.style.position = 'static';
                dropdownMenu.style.width = '100%';
                dropdownMenu.style.marginTop = '0.5rem';
            }
        });
    }
    
    function closeAllDropdowns() {
        const dropdowns = document.querySelectorAll('.main-navbar .main-dropdown-menu');
        const dropdownTriggers = document.querySelectorAll('.main-navbar .main-user-dropdown');
        
        dropdowns.forEach(dropdown => {
            dropdown.classList.remove('show');
        });
        
        dropdownTriggers.forEach(trigger => {
            trigger.setAttribute('aria-expanded', 'false');
        });
    }
    
    function initializeActiveLinks() {
        const currentPath = window.location.pathname;
        const navLinks = document.querySelectorAll('.main-navbar .main-navbar-link');
        
        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href && (href === currentPath || href.startsWith(currentPath))) {
                link.classList.add('active');
            }
        });
    }
    
    function initializeResponsiveBehavior() {
        const navbar = document.querySelector('.main-navbar');
        const collapse = document.querySelector('.main-navbar .main-navbar-collapse');
        
        if (!navbar || !collapse) return;
        
        function handleResize() {
            if (window.innerWidth <= 991.98) {
                collapse.classList.add('mobile-menu');
            } else {
                collapse.classList.remove('mobile-menu');
                collapse.classList.remove('show');
                
                // Reset dropdown positioning on desktop
                const dropdownMenu = document.querySelector('.main-navbar .main-dropdown-menu');
                if (dropdownMenu) {
                    dropdownMenu.style.position = 'absolute';
                    dropdownMenu.style.width = 'auto';
                    dropdownMenu.style.marginTop = '0.125rem';
                }
            }
        }
        
        // Initial call
        handleResize();
        
        // Debounced resize handler
        let resizeTimer;
        window.addEventListener('resize', function() {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(handleResize, 250);
        });
    }
    
    // Expose functions globally if needed
    window.MainNavbar = {
        closeAllDropdowns: closeAllDropdowns,
        initializeMainNavbar: initializeMainNavbar
    };
    
})();

