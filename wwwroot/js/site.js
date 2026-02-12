// ============================================
// ShareKaoMao - Loading Overlay & UI Logic
// ============================================

(function () {
    'use strict';

    var overlay = document.getElementById('loading-overlay');
    if (!overlay) return;

    // แสดง loading overlay
    function showLoading() {
        overlay.style.display = 'flex';
        // Force reflow แล้วค่อยเพิ่ม class เพื่อให้ transition ทำงาน
        overlay.offsetHeight;
        overlay.classList.add('active');
    }

    // แสดง loading เมื่อ form ถูก submit
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (form && form.tagName === 'FORM') {
            // รอสักครู่ก่อนแสดง loading (ป้องกัน flash สำหรับ action ที่เร็ว)
            setTimeout(function () {
                showLoading();
            }, 100);
        }
    });

    // แสดง loading เมื่อคลิกลิงก์ภายใน (ไม่ใช่ # หรือ javascript:)
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

    // ซ่อน loading เมื่อหน้าโหลดเสร็จ (กรณี bfcache / back button)
    window.addEventListener('pageshow', function () {
        overlay.classList.remove('active');
        overlay.style.display = 'none';
    });

    // Auto-dismiss alerts หลัง 5 วินาที
    var alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            var closeBtn = alert.querySelector('.btn-close');
            if (closeBtn) closeBtn.click();
        }, 5000);
    });
})();
