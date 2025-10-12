// Modern Navbar Interactions - Fixed Color Scroll
document.addEventListener('DOMContentLoaded', function () {
    const navbar = document.querySelector('.modern-nav');
    const toggler = document.querySelector('.modern-nav-toggler');
    const collapse = document.querySelector('.modern-nav-collapse');
    const dropdowns = document.querySelectorAll('.modern-nav-item.dropdown');

    // Mobile toggle functionality
    if (toggler && collapse) {
        toggler.addEventListener('click', function () {
            const isExpanded = this.getAttribute('aria-expanded') === 'true';
            this.setAttribute('aria-expanded', !isExpanded);
            collapse.classList.toggle('show');

            // Add backdrop when mobile menu is open
            if (!isExpanded) {
                createBackdrop();
            } else {
                removeBackdrop();
            }
        });
    }

    // Dropdown functionality
    dropdowns.forEach(dropdown => {
        const toggle = dropdown.querySelector('.modern-user-dropdown');
        const menu = dropdown.querySelector('.modern-dropdown-menu');

        if (toggle && menu) {
            toggle.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();

                const isExpanded = this.getAttribute('aria-expanded') === 'true';
                closeAllDropdowns();

                if (!isExpanded) {
                    this.setAttribute('aria-expanded', 'true');
                    menu.classList.add('show');
                }
            });
        }
    });

    // Close dropdowns when clicking outside
    document.addEventListener('click', function (e) {
        if (!e.target.closest('.modern-nav-item.dropdown')) {
            closeAllDropdowns();
        }
    });

    // Close mobile menu when clicking on a link
    const navLinks = document.querySelectorAll('.modern-nav-link');
    navLinks.forEach(link => {
        link.addEventListener('click', function () {
            if (window.innerWidth <= 1024) {
                collapse.classList.remove('show');
                toggler.setAttribute('aria-expanded', 'false');
                removeBackdrop();
            }
        });
    });

    // Navbar scroll effect - REMOVED for consistent dark navbar
    // The navbar will now maintain its dark gradient appearance

    // Helper functions
    function closeAllDropdowns() {
        dropdowns.forEach(dropdown => {
            const toggle = dropdown.querySelector('.modern-user-dropdown');
            const menu = dropdown.querySelector('.modern-dropdown-menu');

            if (toggle && menu) {
                toggle.setAttribute('aria-expanded', 'false');
                menu.classList.remove('show');
            }
        });
    }

    function createBackdrop() {
        const backdrop = document.createElement('div');
        backdrop.className = 'modern-nav-backdrop';
        backdrop.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.5);
            z-index: var(--z-sticky - 1);
            animation: fadeIn 0.3s ease-out;
        `;

        backdrop.addEventListener('click', function () {
            collapse.classList.remove('show');
            toggler.setAttribute('aria-expanded', 'false');
            removeBackdrop();
        });

        document.body.appendChild(backdrop);
        document.body.style.overflow = 'hidden';
    }

    function removeBackdrop() {
        const backdrop = document.querySelector('.modern-nav-backdrop');
        if (backdrop) {
            backdrop.remove();
        }
        document.body.style.overflow = '';
    }

    // Keyboard navigation
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeAllDropdowns();
            if (window.innerWidth <= 1024) {
                collapse.classList.remove('show');
                toggler.setAttribute('aria-expanded', 'false');
                removeBackdrop();
            }
        }
    });

    // Add animation keyframes
    const style = document.createElement('style');
    style.textContent = `
        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }
    `;
    document.head.appendChild(style);
});