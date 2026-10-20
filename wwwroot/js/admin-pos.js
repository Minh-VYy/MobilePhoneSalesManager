(() => {
    const root = document.querySelector('[data-pos]');
    if (!root) return;
    const find = name => root.querySelector('[data-pos-' + name + ']');
    const products = new Map(JSON.parse(document.getElementById('pos-products').textContent).map(x => [x.id, x]));
    const cart = new Map(), picks = Array.from(root.querySelectorAll('[data-pos-pick]'));
    const template = document.getElementById('pos-line-template');
    const currency = value => new Intl.NumberFormat('vi-VN').format(value) + ' ₫';
    const normalize = value => value.toLocaleLowerCase('vi-VN').normalize('NFD').replace(/\p{Diacritic}/gu, '').replace(/đ/g, 'd');
    let scanQueue = Promise.resolve(), pending = 0, submitting = false;
    function error(message = '') { find('error').textContent = message; find('error').hidden = !message; }
    function result(message, kind) { const node = find('scan-result'); node.textContent = message; node.dataset.kind = kind; node.hidden = false; }
    function missingCodes() { return Array.from(cart.values()).reduce((sum, line) => sum + Math.max(0, line.quantity - line.codes.length), 0); }
    JSON.parse(document.getElementById('pos-selected').textContent).forEach(item => {
        const product = products.get(item.id);
        if (!product || product.stock < 1) { error('Một sản phẩm đã hết hàng hoặc bị xóa. Kiểm tra lại hóa đơn.'); return; }
        const line = cart.get(item.id) || { quantity: 0, codes: [] };
        const requested = line.quantity + item.quantity;
        line.quantity = Math.max(1, Math.min(999, product.stock, requested));
        line.codes.push(...(item.codes || [])); line.codes = line.codes.slice(0, line.quantity);
        cart.set(item.id, line);
        if (requested > line.quantity) error('Số lượng đã được điều chỉnh theo tồn kho hiện tại. Hãy kiểm tra lại các mã máy.');
    });
    function setQuantity(id, quantity) {
        if (submitting) return;
        const product = products.get(id), max = Math.min(999, product.stock);
        const line = cart.get(id) || { quantity: 0, codes: [] };
        quantity = Math.max(1, Math.min(max, Number.isFinite(quantity) ? Math.trunc(quantity) : 1));
        if (quantity < line.codes.length) { error('Hãy bỏ mã máy không cần bán trước khi giảm số lượng.'); render(); return; }
        line.quantity = quantity; cart.set(id, line); error(); render();
    }
    function render() {
        const lines = document.createDocumentFragment(), inputs = document.createDocumentFragment();
        let total = 0, count = 0, index = 0;
        function hidden(name, value) { const field = document.createElement('input'); field.type = 'hidden'; field.name = name; field.value = value; inputs.append(field); }
        cart.forEach((selection, id) => {
            const product = products.get(id), quantity = selection.quantity, line = template.content.firstElementChild.cloneNode(true);
            line.querySelector('[data-line-name]').textContent = product.name;
            line.querySelector('[data-line-price]').textContent = id + ' · ' + currency(product.price) + ' / máy · Thành tiền ' + currency(product.price * quantity);
            const amount = line.querySelector('[data-line-quantity]');
            amount.value = quantity; amount.max = Math.min(999, product.stock); amount.disabled = submitting;
            amount.setAttribute('aria-label', 'Số lượng ' + product.name);
            line.querySelector('[data-line-minus]').disabled = submitting || quantity <= Math.max(1, selection.codes.length);
            line.querySelector('[data-line-plus]').disabled = submitting || quantity >= Math.min(999, product.stock);
            line.querySelector('[data-line-minus]').addEventListener('click', () => setQuantity(id, quantity - 1));
            line.querySelector('[data-line-plus]').addEventListener('click', () => setQuantity(id, quantity + 1));
            amount.addEventListener('change', () => setQuantity(id, Number(amount.value)));
            amount.addEventListener('keydown', event => { if (event.key === 'Enter') { event.preventDefault(); setQuantity(id, Number(amount.value)); } });
            line.querySelector('[data-line-remove]').disabled = submitting;
            line.querySelector('[data-line-remove]').addEventListener('click', () => { cart.delete(id); error(); render(); });
            const codes = document.createElement('div'); codes.className = 'admin-pos-unit-codes';
            selection.codes.forEach(code => {
                const button = document.createElement('button'); button.type = 'button'; button.textContent = code + ' ×'; button.disabled = submitting;
                button.setAttribute('aria-label', 'Bỏ mã máy ' + code);
                button.addEventListener('click', () => { selection.codes = selection.codes.filter(value => value !== code); error(); render(); });
                codes.append(button);
            });
            const state = document.createElement('small'); state.className = 'admin-pos-code-count';
            state.textContent = selection.codes.length + ' / ' + quantity + ' mã máy đã quét';
            state.dataset.complete = String(selection.codes.length === quantity);
            codes.append(state); line.append(codes); lines.append(line);
            hidden('Items[' + index + '].MaSP', id); hidden('Items[' + index + '].SoLuong', quantity);
            selection.codes.forEach((code, codeIndex) => hidden('Items[' + index + '].UnitCodes[' + codeIndex + ']', code));
            total += product.price * quantity; count += quantity; index++;
        });
        if (!cart.size) { const empty = document.createElement('p'); empty.className = 'admin-empty-note'; empty.textContent = 'Quét IMEI/serial để thêm máy, hoặc chọn phiên bản rồi quét đủ mã.'; lines.append(empty); }
        const scrollTop = find('cart').scrollTop;
        find('cart').replaceChildren(lines); find('cart').scrollTop = scrollTop;
        find('inputs').replaceChildren(inputs); find('total').textContent = currency(total);
        find('count').textContent = cart.size + ' phiên bản · ' + count + ' máy';
        const missing = missingCodes();
        find('unit-summary').textContent = !cart.size ? 'Quét đủ mã máy trước khi xuất hóa đơn.' : missing ? 'Còn thiếu ' + missing + ' mã IMEI/serial.' : 'Đã quét đủ ' + count + ' mã máy. Kiểm tra thanh toán trước khi xác nhận.';
        find('submit').disabled = submitting || !cart.size || missing > 0 || pending > 0;
        picks.forEach(button => {
            const product = products.get(button.dataset.posPick), selected = cart.get(product.id)?.quantity || 0;
            button.disabled = submitting || selected >= Math.min(999, product.stock);
            button.classList.toggle('is-selected', selected > 0);
            button.setAttribute('aria-label', 'Thêm ' + product.name + ', ' + currency(product.price) + '. Còn ' + Math.max(0, product.stock - selected) + ' máy.');
            button.querySelector('[data-pos-stock]').textContent = selected ? 'Đã chọn ' + selected + ' / ' + product.stock : product.stock ? 'Còn ' + product.stock + ' máy' : 'Hết hàng';
            button.querySelector('.admin-pos-product-price strong').textContent = currency(product.price);
        });
    }
    async function scan(code) {
        find('scan-reference').hidden = true;
        result('Đang kiểm tra mã ' + code + '…', 'info');
        const url = new URL(root.dataset.posLookupUrl, location.href); url.searchParams.set('code', code);
        const response = await fetch(url, { headers: { Accept: 'application/json' }, cache: 'no-store' });
        if (response.status === 401 || response.status === 403) throw new Error('Phiên đăng nhập hết hạn hoặc không có quyền POS. Hãy đăng nhập lại.');
        const data = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(data.error || 'Chưa tra cứu được mã máy.');
        if (data.status !== 'Available') {
            if (data.referenceUrl) { find('scan-reference').href = data.referenceUrl; find('scan-reference').hidden = false; }
            const sold = data.soldAt ? ' · ' + new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short', timeZone: 'Asia/Bangkok' }).format(new Date(data.soldAt)) : '';
            throw new Error(data.code + ': ' + data.statusLabel + (data.reference ? ' · ' + data.reference : '') + sold + '. Không thêm vào hóa đơn.');
        }
        if (!data.productExists || !products.has(data.productId)) throw new Error('Sản phẩm của mã máy này không còn trong danh sách. Mở lại màn hình POS để cập nhật.');
        if (Array.from(cart.values()).some(line => line.codes.includes(data.code))) throw new Error('Máy ' + data.code + ' đã được quét trong hóa đơn này.');
        if (!cart.has(data.productId) && cart.size >= 100) throw new Error('Một hóa đơn có tối đa 100 phiên bản.');
        const line = cart.get(data.productId) || { quantity: 0, codes: [] };
        const desired = Math.max(line.quantity, line.codes.length + 1);
        if (desired > Math.min(999, data.stock)) throw new Error('Phiên bản này không còn đủ số lượng khả dụng trong kho.');
        const product = products.get(data.productId); product.stock = data.stock; product.price = data.price; product.name = data.name;
        line.quantity = desired; line.codes.push(data.code); cart.set(data.productId, line); error();
        result('Đã thêm máy ' + data.code + ' · ' + data.name + '. Máy chưa đánh dấu đã bán cho đến khi xuất hóa đơn.', 'success');
    }
    function enqueueScan() {
        if (submitting) return;
        const input = find('scan-code'), code = input.value.trim();
        if (!code) { result('Quét hoặc nhập IMEI/serial của máy.', 'error'); input.focus(); return; }
        input.value = ''; input.focus(); pending++; find('scan-button').disabled = true; render();
        scanQueue = scanQueue.then(async () => {
            try { await scan(code); } catch (failure) { result(failure.message || 'Không kết nối được máy chủ. Hãy quét lại mã.', 'error'); }
            finally { pending--; find('scan-button').disabled = pending > 0; render(); }
        });
    }
    find('scan-code').addEventListener('keydown', event => { if (event.key === 'Enter') { event.preventDefault(); enqueueScan(); } });
    find('scan-button').addEventListener('click', enqueueScan);
    picks.forEach(button => button.addEventListener('click', () => {
        if (!cart.has(button.dataset.posPick) && cart.size >= 100) { error('Một hóa đơn có tối đa 100 phiên bản.'); return; }
        setQuantity(button.dataset.posPick, (cart.get(button.dataset.posPick)?.quantity || 0) + 1);
    }));
    function filter() {
        let visible = 0;
        picks.forEach(button => {
            const product = products.get(button.dataset.posPick);
            button.hidden = !normalize(product.id + ' ' + product.name + ' ' + product.brandName + ' ' + product.storage).includes(normalize(find('search').value.trim())) || !!(find('brand').value && find('brand').value !== product.brand);
            if (!button.hidden) visible++;
        });
        find('no-results').hidden = visible > 0;
    }
    find('search').addEventListener('input', filter); find('brand').addEventListener('change', filter);
    find('search').addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
    root.addEventListener('submit', event => {
        if (!cart.size || missingCodes() || pending > 0 || submitting) { event.preventDefault(); error('Cần quét đủ mã máy và chờ tra cứu hoàn tất trước khi xuất hóa đơn.'); return; }
        submitting = true; find('scan-code').readOnly = true; render(); find('submit').textContent = 'Đang lưu hóa đơn…';
    });
    window.addEventListener('pageshow', () => { submitting = false; find('scan-code').readOnly = false; render(); find('submit').textContent = 'Xác nhận bán & xuất hóa đơn'; });
    render();
})();
