// Theme handling
export function initTheme() {
    var savedTheme = localStorage.getItem('adminkit-theme');
    if (savedTheme) {
        document.body.classList.add(savedTheme);
    }

    window.setTheme = function (theme) {
        document.body.classList.remove('theme-dark', 'theme-light');
        document.body.classList.add(theme);
        localStorage.setItem('adminkit-theme', theme);
    };
}