// ============================================
// ShareKaoMao - Loading Overlay & UI Logic
// ============================================

(function () {
    'use strict';

    var overlay = document.getElementById('loading-overlay');
    if (!overlay) return;

    function hideLoading() {
        overlay.classList.remove('active');
        overlay.style.display = 'none';
    }

    function showLoading() {
        overlay.style.display = 'flex';
        overlay.offsetHeight;
        overlay.classList.add('active');

        // Safety: ซ่อน overlay อัตโนมัติหลัง 5 วินาที กันค้าง
        setTimeout(hideLoading, 5000);
    }

    // แสดง loading เมื่อ form ถูก submit
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (form && form.tagName === 'FORM') {
            setTimeout(function () {
                showLoading();
            }, 100);
        }
    });

    // แสดง loading เมื่อคลิกลิงก์ภายใน
    document.addEventListener('click', function (e) {
        var link = e.target.closest('a[href]');
        if (!link) return;

        var href = link.getAttribute('href');
        if (!href || href.startsWith('#') || href.startsWith('javascript')) return;
        if (link.target === '_blank') return;

        setTimeout(function () {
            showLoading();
        }, 100);
    });

    // ซ่อน loading เมื่อหน้าโหลดเสร็จ
    window.addEventListener('pageshow', hideLoading);

    // ซ่อน loading เมื่อ DOM โหลดเสร็จ (fallback)
    document.addEventListener('DOMContentLoaded', hideLoading);

    // Auto-dismiss alerts หลัง 5 วินาที
    var alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            var closeBtn = alert.querySelector('.btn-close');
            if (closeBtn) closeBtn.click();
        }, 5000);
    });
})();
