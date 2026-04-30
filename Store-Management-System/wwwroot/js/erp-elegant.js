(function(){
(function(){
  // New unified sidebar behavior:
  // - Desktop: clicking the toggle will collapse/expand the sidebar (body.erp-sidebar-collapsed)
  // - Mobile: clicking the toggle will open/close the overlay sidebar (body.erp-sidebar-open)
  function isMobile() { return window.innerWidth < 992; }

  document.addEventListener('DOMContentLoaded', function(){
    var toggle = document.getElementById('erp-sidebar-toggle');
    var collapseBtn = document.getElementById('erp-sidebar-collapse');
    var sidebar = document.querySelector('.erp-sidebar');
    var shell = document.querySelector('.erp-shell');
    if (!sidebar || !shell) return;

    // Restore desktop collapsed state
    try {
      if (localStorage.getItem('erpSidebarCollapsed') === '1') {
        document.body.classList.add('erp-sidebar-collapsed');
      }
    } catch (e) {}

    function closeMobile() { document.body.classList.remove('erp-sidebar-open'); sidebar.classList.add('hidden'); }
    function openMobile() { document.body.classList.add('erp-sidebar-open'); sidebar.classList.remove('hidden'); }

    if (toggle) {
      toggle.addEventListener('click', function(e){
        e.preventDefault();
        if (isMobile()) {
          // open overlay
          if (document.body.classList.contains('erp-sidebar-open')) { closeMobile(); }
          else { openMobile(); }
        } else {
          // collapse/expand
          var collapsedNow = document.body.classList.toggle('erp-sidebar-collapsed');
          try { localStorage.setItem('erpSidebarCollapsed', collapsedNow ? '1' : '0'); } catch (e) {}
        }
      });
    }

    // collapse button inside sidebar always collapses on desktop, closes on mobile
    if (collapseBtn) {
      collapseBtn.addEventListener('click', function(e){
        e.preventDefault();
        if (isMobile()) { closeMobile(); }
        else { var c = document.body.classList.toggle('erp-sidebar-collapsed'); try { localStorage.setItem('erpSidebarCollapsed', c ? '1' : '0'); } catch (e) {} }
      });
    }

    // clicking outside on mobile closes it
    document.addEventListener('click', function(e){
      if (!isMobile()) return;
      if (!sidebar.classList.contains('hidden')) {
        if (!sidebar.contains(e.target) && !(toggle && toggle.contains(e.target))) {
          closeMobile();
        }
      }
    });

    // handle resize: cleanup mobile-only state
    window.addEventListener('resize', function(){
      if (isMobile()) {
        // show/hide based on body class
        if (!document.body.classList.contains('erp-sidebar-open')) { sidebar.classList.add('hidden'); }
      } else {
        sidebar.classList.remove('hidden');
        document.body.classList.remove('erp-sidebar-open');
      }
    });
  });
})();

// Global logout helper used by header buttons. Submits hidden logout form if present, otherwise falls back to GET logout.
function logoutSubmit(e){
  try{
    if(e && e.preventDefault) e.preventDefault();
    var f = document.getElementById('logout-form');
    if(f){ f.submit(); return; }
    // fallback
    window.location.href = '/Account/LogoutGet';
  }catch(ex){
    console.error('Logout failed', ex);
    window.location.href = '/Account/LogoutGet';
  }
}

