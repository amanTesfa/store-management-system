// Flatpickr initialization
export function initFlatpickr() {
    if (typeof flatpickr !== 'undefined') {
        document.querySelectorAll('.flatpickr').forEach(function (el) {
            flatpickr(el, { dateFormat: "Y-m-d" });
        });
    }
}