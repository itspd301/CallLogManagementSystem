(function () {
    'use strict';

    var body = document.body;
    var isDesktop = function () { return window.innerWidth >= 992; };

    var toggleBtn = document.getElementById('sidebarToggleBtn');
    var backdrop = document.getElementById('sidebarBackdrop');

    function setDesktopCollapsed(collapsed) {
        body.classList.toggle('sidebar-collapsed', collapsed);
        try { localStorage.setItem('clms.sidebarCollapsed', collapsed ? '1' : '0'); } catch (e) { /* storage unavailable */ }
    }

    function setMobileOpen(open) {
        body.classList.toggle('sidebar-mobile-open', open);
    }

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            if (isDesktop()) {
                setDesktopCollapsed(!body.classList.contains('sidebar-collapsed'));
            } else {
                setMobileOpen(!body.classList.contains('sidebar-mobile-open'));
            }
        });
    }

    if (backdrop) {
        backdrop.addEventListener('click', function () {
            setMobileOpen(false);
        });
    }

    // Collapse the mobile drawer automatically after navigating.
    document.querySelectorAll('.app-sidebar .nav-link').forEach(function (link) {
        link.addEventListener('click', function () {
            if (!isDesktop()) {
                setMobileOpen(false);
            }
        });
    });
})();
