(function () {
    const key = 'phonestore.cart.v1';

    function readCart() {
        try {
            const value = JSON.parse(localStorage.getItem(key) || '[]');
            return Array.isArray(value) ? value.filter(item => item && item.productId) : [];
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
        const items = readCart();
        const found = items.find(entry => entry.productId === item.productId);
        if (found) found.quantity = Math.min(Number(found.stock) || 99, (Number(found.quantity) || 0) + 1);
        else items.push({ ...item, quantity: 1 });
        saveCart(items);
        return items;
    }

    window.PhoneStoreCart = { key, read: readCart, save: saveCart, add, updateBadge };

    function renderCartPage() {
        const root = document.querySelector('[data-cart-page]');
        if (!root) return;

        if (document.getElementById('cart-order-success')) {
            saveCart([]);
        }

        const list = root.querySelector('[data-cart-list]');
        const empty = root.querySelector('[data-cart-empty]');
        const summary = root.querySelector('[data-cart-summary]');
        const countLabel = root.querySelector('[data-cart-count]');
        const totalLabel = root.querySelector('[data-cart-total]');
        const hiddenItems = root.querySelector('[data-cart-hidden-items]');

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

            items.forEach((item, index) => {
                const price = Number(item.price) || 0;
                const quantity = Math.max(1, Number(item.quantity) || 1);
                total += price * quantity;

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
                priceText.textContent = `${price.toLocaleString('vi-VN')} ₫`;
                info.append(name, choices, priceText);

                const controls = document.createElement('div');
                controls.className = 'phone-cart-row__controls';
                const minus = document.createElement('button');
                minus.type = 'button'; minus.textContent = '−'; minus.setAttribute('aria-label', 'Giảm số lượng');
                const quantityText = document.createElement('span'); quantityText.textContent = String(quantity);
                const plus = document.createElement('button');
                plus.type = 'button'; plus.textContent = '+'; plus.setAttribute('aria-label', 'Tăng số lượng');
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
            updateBadge();
        }

        function changeQuantity(productId, delta) {
            const items = readCart();
            const item = items.find(entry => entry.productId === productId);
            if (!item) return;
            const next = (Number(item.quantity) || 1) + delta;
            if (next < 1) return;
            item.quantity = Math.min(Number(item.stock) || 99, next);
            saveCart(items);
            render();
        }

        document.addEventListener('phoneStoreCartUpdated', render);
        render();
    }

    document.addEventListener('DOMContentLoaded', () => {
        updateBadge();
        renderCartPage();
    });
})();
