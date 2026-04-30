// Global AJAX spinner
$(document).ajaxStart(function () {
    $('#loadingSpinner').fadeIn(200);
});

$(document).ajaxStop(function () {
    $('#loadingSpinner').fadeOut(200);
});

// For form submissions
$('form').on('submit', function () {
    $('#loadingSpinner').fadeIn(200);
});

// For export buttons
$('#exportExcelBtn, .export-btn').on('click', function () {
    $('#loadingSpinner').fadeIn(200);
});
// Global confirmation modal
function showConfirm(message, onConfirm) {
    $('#confirmModalMessage').text(message);
    $('#confirmModal').modal('show');

    $('#confirmModalOk').off('click').on('click', function () {
        $('#confirmModal').modal('hide');
        if (onConfirm) onConfirm();
    });
}

// Override default confirm for delete buttons
$(document).on('click', '.btn-delete, .delete-btn, [onclick*="delete"]', function (e) {
    var originalOnClick = $(this).attr('onclick');
    if (originalOnClick) {
        e.preventDefault();
        showConfirm('Are you sure you want to delete this item? This action cannot be undone.', function () {
            eval(originalOnClick);
        });
    }
});
// Sidebar Toggle - Complete Working Version
$(document).ready(function () {
    // Toggle sidebar when button is clicked
    $('.sidebar-toggle, .js-sidebar-toggle, #sidebarCollapse').on('click', function (e) {
        e.preventDefault();
        $('#sidebar').toggleClass('active');
        $('.main').toggleClass('active');

        // Optional: Add animation class
        $('#sidebar').toggleClass('collapsed');
    });

    // Alternative: If you want to store the state in localStorage
    // Remember sidebar state
    var sidebarState = localStorage.getItem('sidebarState');
    if (sidebarState === 'collapsed') {
        $('#sidebar').addClass('active');
        $('.main').addClass('active');
    }

    $('.sidebar-toggle, .js-sidebar-toggle, #sidebarCollapse').on('click', function () {
        var isCollapsed = $('#sidebar').hasClass('active');
        localStorage.setItem('sidebarState', isCollapsed ? 'collapsed' : 'expanded');
    });

    // Dropdown menu handling for sidebar
    $('.dropdown-sidebar .dropdown-toggle').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        var target = $(this).attr('data-bs-target');
        if (target) {
            $(target).toggleClass('show');
            $(this).toggleClass('collapsed');

            // Close other open dropdowns
            $('.dropdown-sidebar .collapse').not(target).removeClass('show');
            $('.dropdown-sidebar .dropdown-toggle').not(this).addClass('collapsed');
        }
    });

    // Active menu highlighting
    const currentUrl = window.location.pathname;
    $('.sidebar-nav .nav-link').each(function () {
        const linkUrl = $(this).attr('href');
        if (linkUrl && currentUrl.indexOf(linkUrl) !== -1) {
            $(this).addClass('active');
            // Expand parent dropdown if inside one
            $(this).closest('.collapse').addClass('show');
            $(this).closest('.dropdown-sidebar').find('.dropdown-toggle').removeClass('collapsed');
        }
    });

    // DataTables initialization
    if ($('.datatable').length) {
        $('.datatable').DataTable({
            language: {
                search: "Search:",
                lengthMenu: "Show _MENU_ entries",
                info: "Showing _START_ to _END_ of _TOTAL_ entries",
                paginate: {
                    first: "First",
                    last: "Last",
                    next: "Next",
                    previous: "Previous"
                }
            },
            pageLength: 10,
            responsive: true
        });
    }

    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        $('.alert:not(.alert-permanent)').fadeOut('slow');
    }, 5000);
});