(() => {
    const root = document.querySelector('[data-inventory]');
    if (!root) return;
    const products = JSON.parse(document.getElementById('inventory-data').textContent);
    const map = new Map(products.map(product => [product.id, product]));
    const rows = Array.from(root.querySelectorAll('[data-inventory-row]'));
    const body = root.querySelector('[data-inventory-rows]');
    const find = name => root.querySelector(`[data-inventory-${name}]`);
    const search = find('search'), brand = find('brand'), stock = find('stock'), sort = find('sort');
    const normalize = value => String(value || '').toLocaleLowerCase('vi-VN').normalize('NFD').replace(/\p{Diacritic}/gu, '').replace(/đ/g, 'd');
    const params = new URLSearchParams(location.search);
    search.value = params.get('keyword') || ''; brand.value = params.get('brand') || 'ALL'; stock.value = params.get('stock') || 'ALL';
    if (!brand.value) brand.value = 'ALL'; if (!stock.value) stock.value = 'ALL';
    function filter() {
        const query = normalize(search.value.trim()); let count = 0;
        rows.sort((a, b) => {
            const x = map.get(a.dataset.inventoryRow), y = map.get(b.dataset.inventoryRow);
            return sort.value === 'priceAsc' ? x.price - y.price : sort.value === 'priceDesc' ? y.price - x.price : sort.value === 'stockAsc' ? x.stock - y.stock : x.name.localeCompare(y.name, 'vi');
        });
        rows.forEach(row => {
            const product = map.get(row.dataset.inventoryRow);
            const match = (!query || normalize([product.name, product.id, product.brand, product.ram, product.storage, product.chip].join(' ')).includes(query))
                && (brand.value === 'ALL' || product.brandCode === brand.value)
                && (stock.value === 'ALL' || (stock.value === 'AVAILABLE' && product.stock > 0) || (stock.value === 'LOW' && product.stock < 5) || (stock.value === 'OUT' && product.stock === 0));
            row.hidden = !match; if (match) count++; body.append(row);
        });
        find('count').textContent = `${count} / ${products.length} phiên bản`;
        find('empty').hidden = count > 0;
    }
    [search, brand, stock, sort].forEach(control => control.addEventListener(control === search ? 'input' : 'change', filter));
    find('reset').addEventListener('click', () => { search.value = ''; brand.value = stock.value = 'ALL'; sort.value = 'name'; filter(); search.focus(); });
    function setView(view) {
        root.classList.toggle('inventory--cards', view === 'cards');
        root.querySelectorAll('[data-inventory-view]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.inventoryView === view)));
        try { localStorage.setItem('phonestore.admin.inventoryView', view); } catch { }
    }
    root.querySelectorAll('[data-inventory-view]').forEach(button => button.addEventListener('click', () => setView(button.dataset.inventoryView)));
    try { setView(localStorage.getItem('phonestore.admin.inventoryView') || 'table'); } catch { setView('table'); }
    const dialog = document.getElementById('inventory-preview');
    const image = dialog.querySelector('[data-inventory-image]'), noImage = dialog.querySelector('[data-inventory-no-image]');
    image.addEventListener('error', () => { image.hidden = true; noImage.hidden = false; });
    root.addEventListener('click', event => {
        const trigger = event.target.closest('[data-inventory-preview]'); if (!trigger) return;
        const product = map.get(trigger.dataset.inventoryPreview); if (!product) return;
        const fields = { ...product, price: new Intl.NumberFormat('vi-VN').format(product.price) + ' ₫', stock: product.stock === 0 ? 'Hết hàng' : `${product.stock} máy` };
        dialog.querySelectorAll('[data-inventory-field]').forEach(node => node.textContent = fields[node.dataset.inventoryField] || 'Chưa cập nhật');
        image.hidden = !product.image; noImage.hidden = !image.hidden; image.src = product.image || ''; image.alt = product.name;
        dialog.querySelector('[data-inventory-edit-link]').href = product.editUrl; dialog.querySelector('[data-inventory-customer-link]').href = product.customerUrl;
        dialog.showModal();
    });
    dialog.querySelector('[data-dialog-close]').addEventListener('click', () => dialog.close());
    dialog.addEventListener('click', event => { if (event.target === dialog) { const rect = dialog.getBoundingClientRect(); if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) dialog.close(); } });
    filter(); if (params.has('focus')) search.focus();
})();
