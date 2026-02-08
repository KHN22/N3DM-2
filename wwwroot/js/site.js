/* ============================================================
   3D Model Marketplace - Client-side JavaScript
   ============================================================ */

document.addEventListener('DOMContentLoaded', function () {

    // --- Profile Dropdown Toggle ---
    const dropdown = document.querySelector('.profile-dropdown');
    const trigger = dropdown ? dropdown.querySelector('.profile-trigger') : null;

    if (trigger && dropdown) {
        trigger.addEventListener('click', function (e) {
            e.stopPropagation();
            dropdown.classList.toggle('open');
        });

        document.addEventListener('click', function (e) {
            if (!dropdown.contains(e.target)) {
                dropdown.classList.remove('open');
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                dropdown.classList.remove('open');
                trigger.focus();
            }
        });
    }

    // --- Role Selection Toggle (Register Page) ---
    const roleInputs = document.querySelectorAll('input[name="Role"]');
    const sellerNotice = document.getElementById('sellerNotice');

    roleInputs.forEach(function (input) {
        input.addEventListener('change', function () {
            if (sellerNotice) {
                sellerNotice.style.display = this.value === 'Seller' ? 'flex' : 'none';
            }
        });
    });

    // --- Admin Sidebar Active State ---
    const sidebarLinks = document.querySelectorAll('.admin-sidebar a, .settings-nav a');
    sidebarLinks.forEach(function (link) {
        if (link.href === window.location.href) {
            link.classList.add('active');
        }
    });

    // --- Mock Chart Animation (Admin Sales Overview) ---
    const bars = document.querySelectorAll('.bar-chart-mock .bar');
    if (bars.length > 0) {
        bars.forEach(function (bar) {
            const targetHeight = bar.style.height;
            bar.style.height = '0';
            setTimeout(function () {
                bar.style.height = targetHeight;
            }, 100);
        });
    }
});
