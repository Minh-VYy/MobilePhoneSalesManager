document.addEventListener('click', event => {
    const button = event.target.closest('[data-toggle-password]');
    if (!button) return;
    const input = button.parentElement.querySelector('input');
    const show = input.type === 'password';
    input.type = show ? 'text' : 'password';
    button.setAttribute('aria-pressed', String(show));
    button.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
});
document.addEventListener('submit', event => {
    const form = event.target.closest('[data-confirm]');
    if (form && !window.confirm(form.dataset.confirm)) event.preventDefault();
});
