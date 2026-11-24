(() => {
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
