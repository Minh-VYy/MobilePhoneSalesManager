(function () {
    const key = 'phonestore.cart.v1';

    function readCart() {
        try {
            const value = JSON.parse(localStorage.getItem(key) || '[]');
            return Array.isArray(value) ? value.filter(item => item && typeof item.productId === 'string').slice(0, 20) : [];
        } catch {
            return [];
        }
    }

    function saveCart(items) {
        localStorage.setItem(key, JSON.stringify(items));
        updateBadge();
        document.dispatchEvent(new CustomEvent('phoneStoreCartUpdated', { detail: { items } }));
    }

    function updateBadge() {
        const count = readCart().reduce((sum, item) => sum + Math.max(0, Number(item.quantity) || 0), 0);
        document.querySelectorAll('[data-cart-badge]').forEach(badge => {
            badge.textContent = count > 99 ? '99+' : String(count);
            badge.setAttribute('aria-label', `${count} sản phẩm trong giỏ hàng`);
            badge.hidden = count === 0;
        });
    }

    function add(item) {
        if (Number(item.stock) < 1) throw new Error('Sản phẩm hiện đã hết hàng.');
        const items = readCart();
        const found = items.find(entry => entry.productId === item.productId);
        if (found) {
            if (Number(found.quantity) >= Math.min(Number(item.stock), 20)) throw new Error('Đã đạt số lượng tối đa có thể đặt cho phiên bản này.');
            Object.assign(found, item, { quantity: (Number(found.quantity) || 0) + 1 });
        } else {
            if (items.length >= 20) throw new Error('Mỗi đơn có tối đa 20 mẫu sản phẩm.');
            items.push({ ...item, quantity: 1 });
        }
        saveCart(items);
        return items;
    }

    window.PhoneStoreCart = { key, read: readCart, save: saveCart, add, updateBadge };

    function renderCartPage() {
        const root = document.querySelector('[data-cart-page]');
        if (!root) return;

const list = root.querySelector('[data-cart-list]');
        const empty = root.querySelector('[data-cart-empty]');
        const summary = root.querySelector('[data-cart-summary]');
        const countLabel = root.querySelector('[data-cart-count]');
        const totalLabel = root.querySelector('[data-cart-total]');
        const hiddenItems = root.querySelector('[data-cart-hidden-items]');
        const checkout = root.querySelector('[data-checkout-submit]');
        const error = root.querySelector('[data-cart-error]');
        let validated = false, connectionError = '', sending = false;
        const catalogUrl = new URL('CartProducts', new URL(root.dataset.catalogBase || './', location.href));

        function render() {
            const items = readCart();
            const count = items.reduce((sum, item) => sum + Number(item.quantity || 0), 0);
            list.replaceChildren();
            hiddenItems.replaceChildren();
            list.hidden = items.length === 0;
            empty.hidden = items.length !== 0;
            summary.hidden = items.length === 0;
            countLabel.textContent = `${count} sản phẩm`;
            let total = 0;
            let unavailable = false;

            items.forEach((item, index) => {
                const price = Number(item.price) || 0;
                const quantity = Math.max(1, Number(item.quantity) || 1);
                total += price * quantity;
                unavailable ||= !Number.isFinite(quantity) || quantity > 20 || quantity > Number(item.stock) || item.unavailable === true;

                const row = document.createElement('article');
                row.className = 'phone-cart-row';
                const imageWrap = document.createElement('div');
                imageWrap.className = 'phone-cart-row__image';
                const image = document.createElement('img');
                image.src = item.image || '';
                image.alt = item.name || 'Điện thoại';
                image.onerror = () => imageWrap.classList.add('is-empty');
                imageWrap.append(image);

                const info = document.createElement('div');
                info.className = 'phone-cart-row__info';
                const name = document.createElement('b');
                name.textContent = item.name || item.productId;
                const choices = document.createElement('small');
                choices.textContent = [item.condition, item.color, item.storage, item.warranty].filter(Boolean).join(' · ');
                const priceText = document.createElement('strong');
                priceText.textContent = Number(item.stock) > 0 ? `${price.toLocaleString('vi-VN')} ₫` : 'Hết hàng';
                info.append(name, choices, priceText);

                const controls = document.createElement('div');
                controls.className = 'phone-cart-row__controls';
                const minus = document.createElement('button');
                minus.type = 'button'; minus.textContent = '−'; minus.setAttribute('aria-label', 'Giảm số lượng');
                const quantityText = document.createElement('span'); quantityText.textContent = String(quantity);
                const plus = document.createElement('button');
                plus.type = 'button'; plus.textContent = '+'; plus.setAttribute('aria-label', 'Tăng số lượng');
                plus.disabled = quantity >= Math.min(Number(item.stock), 20);
                minus.disabled = quantity <= 1;
                const remove = document.createElement('button');
                remove.type = 'button'; remove.className = 'phone-cart-remove'; remove.textContent = 'Xóa';
                minus.addEventListener('click', () => changeQuantity(item.productId, -1));
                plus.addEventListener('click', () => changeQuantity(item.productId, 1));
                remove.addEventListener('click', () => saveCart(readCart().filter(entry => entry.productId !== item.productId)));
                controls.append(minus, quantityText, plus, remove);
                row.append(imageWrap, info, controls);
                list.append(row);

                const productId = document.createElement('input');
                productId.type = 'hidden'; productId.name = `Items[${index}].ProductId`; productId.value = item.productId;
                const quantityInput = document.createElement('input');
                quantityInput.type = 'hidden'; quantityInput.name = `Items[${index}].Quantity`; quantityInput.value = String(quantity);
                hiddenItems.append(productId, quantityInput);
            });
            totalLabel.textContent = `${total.toLocaleString('vi-VN')} ₫`;
            error.textContent = connectionError || (unavailable ? 'Một sản phẩm đã hết hàng, không còn tồn tại hoặc không đủ số lượng. Hãy giảm số lượng hoặc xóa sản phẩm đó.' : '');
            error.hidden = !error.textContent;
            if (checkout) checkout.disabled = !validated || unavailable || !!connectionError || sending;
            updateBadge();
        }

        function changeQuantity(productId, delta) {
            const items = readCart();
            const item = items.find(entry => entry.productId === productId);
            if (!item) return;
            const next = (Number(item.quantity) || 1) + delta;
            if (next < 1) return;
            if (delta > 0 && next > Math.min(Number(item.stock), 20)) return;
            item.quantity = next;
            saveCart(items);
        }

        document.addEventListener('phoneStoreCartUpdated', render);
        render();
        async function refreshCatalog() {
            validated = false; render();
            try {
                const cart = readCart();
                if (!cart.length) { validated = true; render(); return; }
                catalogUrl.searchParams.set('ids', cart.map(item => item.productId).join(','));
                const response = await fetch(catalogUrl, { headers: { Accept: 'application/json' }, cache: 'no-store' });
                if (!response.ok) throw new Error();
                const products = await response.json();
                const updated = readCart().map(item => {
                    const product = products.find(p => p.productId === item.productId);
                    return product ? { ...item, name: product.productName, price: product.price, image: product.image, stock: product.stock,
                        color: product.color, condition: product.conditionText, storage: product.storageLabel, warranty: product.warrantyText, unavailable: false }
                        : { ...item, stock: 0, unavailable: true };
                });
                validated = true; connectionError = ''; saveCart(updated);
            } catch { connectionError = 'Chưa cập nhật được giá và tồn kho. Tải lại trang để thử lại; giỏ hàng của bạn vẫn được giữ.'; render(); }
        }
        root.querySelector('#customer-order-form')?.addEventListener('submit', event => {
            if (!validated || connectionError || sending || (checkout && checkout.disabled)) { event.preventDefault(); return; }
            sending = true;
            if (checkout) { checkout.disabled = true; checkout.textContent = 'Đang đặt hàng…'; }
        });
        window.addEventListener('storage', event => { if (event.key === key) refreshCatalog(); });
        refreshCatalog();
    }

    document.addEventListener('DOMContentLoaded', () => {
        if (document.querySelector('[data-clear-cart]')) saveCart([]);
        updateBadge();
        renderCartPage();
    });
})();
