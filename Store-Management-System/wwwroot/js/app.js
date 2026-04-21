// Simple app.js - No imports
(function () {
    'use strict';

    document.addEventListener("DOMContentLoaded", function () {
        // Sidebar toggle is already in layout, but ensure it works
        var sidebarToggle = document.querySelector(".js-sidebar-toggle");
        if (sidebarToggle) {
            sidebarToggle.removeEventListener("click", toggleSidebar);
            sidebarToggle.addEventListener("click", toggleSidebar);
        }

        function toggleSidebar() {
            document.querySelector("#sidebar").classList.toggle("collapsed");
        }

        // Auto-hide alerts
        setTimeout(function () {
            document.querySelectorAll('.alert:not(.alert-permanent)').forEach(function (alert) {
                var bsAlert = new bootstrap.Alert(alert);
                bsAlert.close();
            });
        }, 5000);

        // Initialize tooltips
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.forEach(function (tooltipTriggerEl) {
            new bootstrap.Tooltip(tooltipTriggerEl);
        });
    });
})();