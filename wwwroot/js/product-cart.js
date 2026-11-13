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
        const buyer = root.dataset.cartUser === 'true', orderForm = root.querySelector('#customer-order-form');
        const couponInput = root.querySelector('[data-coupon-code]'), appliedCode = root.querySelector('[data-coupon-applied]');
        const couponApply = root.querySelector('[data-coupon-apply]'), couponRemove = root.querySelector('[data-coupon-remove]'), couponMessage = root.querySelector('[data-coupon-message]');
        const money = value => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value);
        const signature = () => JSON.stringify(readCart().map(item => [item.productId, Number(item.quantity)]));
        let quote = null, quoteSignature = '', quoteBusy = false, quoteVersion = 0, quoteTimer;
        let lastSignature = signature();
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
            const quoted = quote && quoteSignature === signature();
            const unapplied = couponInput.value.trim().toUpperCase() !== appliedCode.value.trim().toUpperCase();
            root.querySelector('[data-cart-subtotal]').textContent = money(quoted ? quote.subtotal : total);
            root.querySelector('[data-cart-discount]').textContent = quoted ? '−' + money(quote.discountAmount) : money(0);
            root.querySelector('[data-cart-discount-row]').hidden = !quoted || quote.discountAmount === 0;
            root.querySelector('[data-cart-voucher-label]').textContent = quoted ? quote.voucherCode : '';
            totalLabel.textContent = quoted ? money(quote.total) : buyer ? '—' : money(total);
            couponApply.disabled = !buyer || !validated || quoteBusy || sending;
            couponRemove.hidden = !couponInput.value.trim() && !appliedCode.value;
            couponRemove.disabled = !buyer || quoteBusy || sending;
            couponInput.readOnly = !buyer || sending;
            error.textContent = connectionError || (unavailable ? 'Một sản phẩm đã hết hàng, không còn tồn tại hoặc không đủ số lượng. Hãy giảm số lượng hoặc xóa sản phẩm đó.' : '');
            error.hidden = !error.textContent;
            if (checkout) checkout.disabled = !validated || unavailable || !!connectionError || sending || quoteBusy || !quoted || unapplied;
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

        function cartChanged() {
            const next = signature();
            if (next !== lastSignature) {
                lastSignature = next; quote = null; quoteVersion++; quoteBusy = false;
                clearTimeout(quoteTimer);
                if (validated && buyer && readCart().length) quoteTimer = setTimeout(() => requestQuote(appliedCode.value), 150);
            }
            render();
        }
        async function requestQuote(code) {
            if (!buyer || !validated || !readCart().length) return;
            clearTimeout(quoteTimer);
            const version = ++quoteVersion, cartSignature = signature(), requestedCode = (code || '').trim().toUpperCase();
            quoteBusy = true; quote = null; couponMessage.textContent = 'Đang kiểm tra giá và ưu đãi…'; render();
            const body = new URLSearchParams({ __RequestVerificationToken: orderForm.querySelector('[name="__RequestVerificationToken"]').value, VoucherCode: requestedCode });
            readCart().forEach((item, index) => { body.set('Items[' + index + '].ProductId', item.productId); body.set('Items[' + index + '].Quantity', String(item.quantity)); });
            try {
                const response = await fetch(root.dataset.quoteUrl, { method: 'POST', headers: { Accept: 'application/json' }, body });
                const data = await response.json().catch(() => ({}));
                if (version !== quoteVersion || cartSignature !== signature()) return;
                if (!response.ok) throw new Error(response.status === 401 ? 'Đăng nhập lại để kiểm tra mã giảm giá.' : data.error || 'Chưa kiểm tra được ưu đãi. Hãy thử áp dụng lại.');
                quote = data; quoteSignature = cartSignature; appliedCode.value = data.voucherCode;
                if (couponInput.value.trim().toUpperCase() === requestedCode) couponInput.value = data.voucherCode;
                couponMessage.textContent = data.voucherCode ? 'Đã áp dụng ' + data.voucherCode + ' · Tiết kiệm ' + money(data.discountAmount) + '. Lượt chỉ được giữ khi đặt đơn thành công.' : 'Đã kiểm tra giá. Chưa áp dụng mã giảm giá.';
                const updated = readCart().map(item => {
                    const line = data.lines.find(x => x.productId === item.productId);
                    return line ? { ...item, name: line.name, price: line.unitPrice } : item;
                });
                saveCart(updated);
            } catch (failure) {
                if (version === quoteVersion) { quote = null; couponMessage.textContent = failure.message + ' Bấm Bỏ mã để đặt không dùng mã, hoặc Áp dụng để thử lại.'; }
            } finally { if (version === quoteVersion) { quoteBusy = false; render(); } }
        }
        couponApply.addEventListener('click', () => requestQuote(couponInput.value));
        couponRemove.addEventListener('click', () => { couponInput.value = ''; appliedCode.value = ''; requestQuote(''); });
        couponInput.addEventListener('input', () => { couponMessage.textContent = 'Bấm Áp dụng để kiểm tra mã vừa nhập.'; render(); });
        couponInput.addEventListener('keydown', event => { if (event.key === 'Enter') { event.preventDefault(); requestQuote(couponInput.value); } });
        if (!buyer) couponMessage.textContent = 'Đăng nhập tài khoản khách để áp mã giảm giá và đặt hàng.';
        document.addEventListener('phoneStoreCartUpdated', cartChanged);
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
                if (buyer) requestQuote(appliedCode.value);
            } catch { connectionError = 'Chưa cập nhật được giá và tồn kho. Tải lại trang để thử lại; giỏ hàng của bạn vẫn được giữ.'; render(); }
        }
        root.querySelector('#customer-order-form')?.addEventListener('submit', event => {
            if (!validated || connectionError || sending || (checkout && checkout.disabled)) { event.preventDefault(); return; }
            sending = true;
            if (checkout) { checkout.disabled = true; checkout.textContent = 'Đang đặt hàng…'; }
        });
        window.addEventListener('storage', event => { if (event.key === key) refreshCatalog(); });
        window.addEventListener('pageshow', event => { if (event.persisted) { sending = false; refreshCatalog(); } });
        refreshCatalog();
    }

    document.addEventListener('DOMContentLoaded', () => {
        if (document.querySelector('[data-clear-cart]')) saveCart([]);
        updateBadge();
        renderCartPage();
    });
})();
