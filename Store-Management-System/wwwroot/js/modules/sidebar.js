// Sidebar functionality
export function initSidebar() {
    var sidebarToggle = document.querySelector(".js-sidebar-toggle");
    if (sidebarToggle) {
        sidebarToggle.addEventListener("click", function () {
            document.querySelector("#sidebar").classList.toggle("collapsed");
        });
    }
}