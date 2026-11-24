(() => {
    const links = document.querySelectorAll('#mainNav a[href]');
    const section = path => path.toLowerCase().split('/').filter(Boolean)[0] || 'home';
    const current = section(location.pathname);
    for (const link of links) {
        if (section(link.pathname) === current) {
            link.classList.add('selected');
            if (link.pathname === location.pathname) link.setAttribute('aria-current', 'page');
            link.closest('.dropdown')?.querySelector('.dropdown-toggle')?.classList.add('selected');
        }
    }

    const form = document.querySelector('[data-verify-form]');
    if (!form) return;
    const button = form.querySelector('[type="submit"]');
    const label = button.querySelector('[data-submit-label]');
    form.addEventListener('submit', () => {
        button.disabled = true;
        button.setAttribute('aria-busy', 'true');
        label.textContent = 'Đang xác minh…';
    });
    window.addEventListener('pageshow', () => {
        button.disabled = false;
        button.removeAttribute('aria-busy');
        label.textContent = 'Xác minh';
    });
})();
