import './modules/bootstrap';
import { initSidebar } from './modules/sidebar';
import { initTheme } from './modules/theme';
import { initFeather } from './modules/feather';
import { initCharts } from './modules/chartjs';
import { initFlatpickr } from './modules/flatpickr';
import { initVectorMaps } from './modules/vector-maps';

document.addEventListener("DOMContentLoaded", function () {
    initSidebar();
    initTheme();
    initFeather();
    initCharts();
    initFlatpickr();
    initVectorMaps();
});