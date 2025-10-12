/* Modern Navbar JavaScript - Namespaced and Conflict-Free */
/* This file handles all navbar interactions with modern event handling */

(function() {
    'use strict';
    
    // Configuration
    const CONFIG = {
        breakpoints: {
            mobile: 1024
        },
        selectors: {
            navbar: '.modern-nav',
            toggler: '.modern-nav-toggler',
            collapse: '.modern-nav-collapse',
            nav: '.modern-nav-nav',
            userDropdown: '.modern-user-dropdown',
            dropdownMenu: '.modern-dropdown-menu',
            dropdownItem: '.modern-dropdown-item'
        },
        classes: {
            show: 'show',
            active: 'active',
            expanded: 'aria-expanded'
        }
    };
    
    // State management
    let isInitialized = false;
    let isMobile = window.innerWidth <= CONFIG.breakpoints.mobile;
    
    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        if (!isInitialized) {
            initializeModernNavbar();
            isInitialized = true;
        }
    });
    
    function initializeModernNavbar() {
        const navbar = document.querySelector(CONFIG.selectors.navbar);
        if (!navbar) {
            console.warn('Modern navbar not found');
            return;
        }
        
        // Initialize components
        initializeToggler();
        initializeUserDropdown();
        initializeActiveLinks();
        initializeResponsiveBehavior();
        initializeAccessibility();
        
        console.log('Modern navbar initialized successfully');
    }
    
    function initializeToggler() {
        const toggler = document.querySelector(CONFIG.selectors.toggler);
        const collapse = document.querySelector(CONFIG.selectors.collapse);
        
        if (!toggler || !collapse) return;
        
        // Toggle functionality
        toggler.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const isExpanded = collapse.classList.contains(CONFIG.classes.show);
            
            if (isExpanded) {
                closeMobileMenu();
            } else {
                openMobileMenu();
            }
        });
        
        // Close on outside click (mobile only)
        document.addEventListener('click', function(e) {
            if (isMobile && !toggler.contains(e.target) && !collapse.contains(e.target)) {
                closeMobileMenu();
            }
        });
        
        // Close on nav link click (mobile only)
        const navLinks = collapse.querySelectorAll('a[href]');
        navLinks.forEach(link => {
            link.addEventListener('click', function() {
                if (isMobile) {
                    closeMobileMenu();
                }
            });
        });
    }
    
    function initializeUserDropdown() {
        const userDropdown = document.querySelector(CONFIG.selectors.userDropdown);
        const dropdownMenu = document.querySelector(CONFIG.selectors.dropdownMenu);
        
        if (!userDropdown || !dropdownMenu) return;
        
        // Toggle dropdown
        userDropdown.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const isVisible = dropdownMenu.classList.contains(CONFIG.classes.show);
            
            // Close all dropdowns first
            closeAllDropdowns();
            
            // Toggle current dropdown
            if (!isVisible) {
                openDropdown(userDropdown, dropdownMenu);
            }
        });
        
        // Close on outside click
        document.addEventListener('click', function(e) {
            if (!userDropdown.contains(e.target) && !dropdownMenu.contains(e.target)) {
                closeAllDropdowns();
            }
        });
        
        // Close on dropdown item click
        const dropdownItems = dropdownMenu.querySelectorAll(CONFIG.selectors.dropdownItem);
        dropdownItems.forEach(item => {
            item.addEventListener('click', function() {
                // Small delay to allow form submission or navigation
                setTimeout(() => {
                    closeAllDropdowns();
                }, 100);
            });
        });
    }
    
    function initializeActiveLinks() {
        const currentPath = window.location.pathname;
        const navLinks = document.querySelectorAll(`${CONFIG.selectors.nav} a[href]`);
        
        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href && (href === currentPath || href.startsWith(currentPath))) {
                link.classList.add(CONFIG.classes.active);
            }
        });
    }
    
    function initializeResponsiveBehavior() {
        // Handle window resize
        let resizeTimer;
        window.addEventListener('resize', function() {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(handleResize, 250);
        });
        
        // Initial call
        handleResize();
    }
    
    function initializeAccessibility() {
        // Keyboard navigation
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                closeAllDropdowns();
                closeMobileMenu();
            }
        });
        
        // Focus management
        const focusableElements = document.querySelectorAll(
            `${CONFIG.selectors.toggler}, ${CONFIG.selectors.userDropdown}, ${CONFIG.selectors.dropdownItem}`
        );
        
        focusableElements.forEach(element => {
            element.addEventListener('focus', function() {
                this.classList.add('focus-ring');
            });
            
            element.addEventListener('blur', function() {
                this.classList.remove('focus-ring');
            });
        });
    }
    
    function openMobileMenu() {
        const toggler = document.querySelector(CONFIG.selectors.toggler);
        const collapse = document.querySelector(CONFIG.selectors.collapse);
        
        if (!toggler || !collapse) return;
        
        collapse.classList.add(CONFIG.classes.show);
        toggler.setAttribute(CONFIG.classes.expanded, 'true');
        
        // Add body class for mobile menu state
        document.body.classList.add('mobile-menu-open');
        
        // Animate in
        collapse.style.animation = 'slideIn 0.3s ease-out';
    }
    
    function closeMobileMenu() {
        const toggler = document.querySelector(CONFIG.selectors.toggler);
        const collapse = document.querySelector(CONFIG.selectors.collapse);
        
        if (!toggler || !collapse) return;
        
        collapse.classList.remove(CONFIG.classes.show);
        toggler.setAttribute(CONFIG.classes.expanded, 'false');
        
        // Remove body class
        document.body.classList.remove('mobile-menu-open');
    }
    
    function openDropdown(trigger, menu) {
        menu.classList.add(CONFIG.classes.show);
        trigger.setAttribute(CONFIG.classes.expanded, 'true');
        
        // Position dropdown for mobile
        if (isMobile) {
            menu.style.position = 'static';
            menu.style.width = '100%';
            menu.style.marginTop = '0.5rem';
        } else {
            menu.style.position = 'absolute';
            menu.style.width = 'auto';
            menu.style.marginTop = '0.5rem';
        }
    }
    
    function closeAllDropdowns() {
        const dropdowns = document.querySelectorAll(CONFIG.selectors.dropdownMenu);
        const triggers = document.querySelectorAll(CONFIG.selectors.userDropdown);
        
        dropdowns.forEach(dropdown => {
            dropdown.classList.remove(CONFIG.classes.show);
        });
        
        triggers.forEach(trigger => {
            trigger.setAttribute(CONFIG.classes.expanded, 'false');
        });
    }
    
    function handleResize() {
        const wasMobile = isMobile;
        isMobile = window.innerWidth <= CONFIG.breakpoints.mobile;
        
        // Reset mobile menu if switching from mobile to desktop
        if (wasMobile && !isMobile) {
            closeMobileMenu();
            closeAllDropdowns();
        }
        
        // Reset dropdown positioning
        const dropdownMenu = document.querySelector(CONFIG.selectors.dropdownMenu);
        if (dropdownMenu) {
            if (isMobile) {
                dropdownMenu.style.position = 'static';
                dropdownMenu.style.width = '100%';
                dropdownMenu.style.marginTop = '0.5rem';
            } else {
                dropdownMenu.style.position = 'absolute';
                dropdownMenu.style.width = 'auto';
                dropdownMenu.style.marginTop = '0.5rem';
            }
        }
    }
    
    // Utility functions
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
    
    // Public API
    window.ModernNavbar = {
        // Public methods
        closeAllDropdowns: closeAllDropdowns,
        closeMobileMenu: closeMobileMenu,
        openMobileMenu: openMobileMenu,
        
        // State getters
        isMobile: () => isMobile,
        isInitialized: () => isInitialized,
        
        // Reinitialize if needed
        reinitialize: function() {
            if (isInitialized) {
                initializeModernNavbar();
            }
        }
    };
    
    // Auto-reinitialize if new content is loaded
    if (window.MutationObserver) {
        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                    // Check if navbar was added
                    const hasNavbar = Array.from(mutation.addedNodes).some(node => 
                        node.nodeType === 1 && node.querySelector && node.querySelector(CONFIG.selectors.navbar)
                    );
                    
                    if (hasNavbar && !isInitialized) {
                        initializeModernNavbar();
                        isInitialized = true;
                    }
                }
            });
        });
        
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }
    
})();

