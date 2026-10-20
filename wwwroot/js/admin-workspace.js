(() => {
    const sidebar = document.getElementById('adminSidebar'), toggle = document.getElementById('mobileSidebarToggle');
    const mobile = matchMedia('(max-width:900px)');
    const backdrop = document.querySelector('.admin-sidebar-backdrop');
    function menu(open) {
        if (!sidebar || !toggle) return;
        sidebar.classList.toggle('open', open); sidebar.inert = mobile.matches && !open;
        const main = document.querySelector('.admin-main'); if (main) main.inert = mobile.matches && open;
        toggle.setAttribute('aria-expanded', String(open)); if (backdrop) backdrop.hidden = !open;
        document.body.classList.toggle('admin-menu-open', open);
        if (open) sidebar.querySelector('a')?.focus();
    }
    toggle?.addEventListener('click', () => menu(!sidebar.classList.contains('open')));
    document.querySelectorAll('[data-admin-close]').forEach(button => button.addEventListener('click', () => { menu(false); toggle?.focus(); }));
    mobile.addEventListener('change', () => menu(false)); menu(false);
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') { const wasOpen = sidebar?.classList.contains('open'); menu(false); if (wasOpen) toggle?.focus(); document.querySelectorAll('.admin-notifications[open]').forEach(item => item.open = false); }
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
            event.preventDefault(); const search = document.getElementById('globalAdminSearch');
            if (search?.offsetParent) search.focus(); else if (search) location.href = search.form.action + '?focus=search';
        }
    });
    document.querySelectorAll('[data-admin-list]').forEach(root => {
        const rows = Array.from(root.querySelectorAll('[data-admin-row]'));
        const keyword = root.querySelector('[data-admin-keyword]'), status = root.querySelector('[data-admin-status]');
        const params = new URLSearchParams(location.search);
        const pagination = root.querySelector('[data-admin-pagination]');
        const size = root.querySelector('[data-admin-page-size]');
        const prev = root.querySelector('[data-admin-page-prev]'), next = root.querySelector('[data-admin-page-next]');
        let page = 1;
        if (pagination) pagination.hidden = false;
        if (keyword) keyword.value = params.get('keyword') || '';
        if (status && params.has('status')) status.value = params.get('status');
        function filter() {
            const normalize = value => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLocaleLowerCase('vi-VN');
            const query = normalize((keyword?.value || '').trim());
            const matching = rows.filter(row => (!query || normalize(row.dataset.search).includes(query)) && (!status?.value || status.value === row.dataset.status));
            const pageSize = Number(size?.value || Math.max(1, matching.length));
            const pages = Math.max(1, Math.ceil(matching.length / pageSize)); page = Math.min(page, pages);
            rows.forEach(row => row.hidden = true);
            matching.slice((page - 1) * pageSize, page * pageSize).forEach(row => row.hidden = false);
            root.querySelector('[data-admin-list-count]').textContent = `${matching.length} / ${rows.length} ${root.dataset.adminListNoun || 'đơn'}`;
            root.querySelector('[data-admin-empty]').hidden = matching.length > 0;
            if (pagination) { root.querySelector('[data-admin-page-label]').textContent = `Trang ${page} / ${pages}`; prev.disabled = page <= 1; next.disabled = page >= pages; }
        }
        const reset = () => { page = 1; filter(); };
        keyword?.addEventListener('input', reset); status?.addEventListener('change', reset); size?.addEventListener('change', reset);
        prev?.addEventListener('click', () => { page = Math.max(1, page - 1); filter(); }); next?.addEventListener('click', () => { page++; filter(); }); filter();
    });
})();
