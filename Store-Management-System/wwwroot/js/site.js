// Sidebar Toggle
$(document).ready(function () {
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
        $('#content').toggleClass('active');
        $('.wrapper').toggleClass('active');
    });

    // Dropdown menu handling for sidebar
    $('.dropdown-sidebar .dropdown-toggle').on('click', function (e) {
        e.preventDefault();
        const target = $(this).attr('data-bs-target');
        $(target).toggleClass('show');
        $(this).toggleClass('collapsed');

        // Close other open dropdowns
        $('.dropdown-sidebar .collapse').not(target).removeClass('show');
        $('.dropdown-sidebar .dropdown-toggle').not(this).addClass('collapsed');
    });

    // Active menu highlighting
    const currentUrl = window.location.pathname;
    $('.sidebar-nav .nav-link').each(function () {
        if ($(this).attr('href') === currentUrl) {
            $(this).addClass('active');
            // Expand parent dropdown if inside one
            $(this).closest('.collapse').addClass('show');
            $(this).closest('.dropdown-sidebar').find('.dropdown-toggle').removeClass('collapsed');
        }
    });

    // DataTables initialization for tables with class 'datatable'
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
        $('.alert').fadeOut('slow');
    }, 5000);
});