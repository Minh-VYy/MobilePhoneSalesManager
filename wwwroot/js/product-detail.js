document.addEventListener('DOMContentLoaded', function () {
    const detail = document.querySelector('[data-product-detail]');
    if (!detail) return;

    let variants = [];
    try {
        variants = JSON.parse(document.getElementById('product-variant-data')?.textContent || '[]');
    } catch {
        variants = [];
    }

    let activeProduct = variants.find(product => product.productId === detail.dataset.productId) || variants[0] || null;
    let selection = activeProduct ? {
        condition: activeProduct.conditionValue,
        storage: activeProduct.storage,
        color: activeProduct.color,
        warranty: String(activeProduct.warrantyMonths)
    } : null;
    const formatPrice = value => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 }).format(Number(value) || 0) + ' ₫';

    function setText(selector, value) {
        const node = detail.querySelector(selector);
        if (node) node.textContent = value == null ? '' : String(value);
    }

    function dimension(product, kind) {
        return ({
            condition: product.conditionValue,
            storage: product.storage,
            color: product.color,
            warranty: String(product.warrantyMonths)
        })[kind] ?? '';
    }

    function markSelectedOptions() {
        detail.querySelectorAll('[data-select-variant]').forEach(button => {
            const selected = button.dataset.variantValue === String(selection?.[button.dataset.variantKind] ?? '');
            button.classList.toggle('is-selected', selected);
            button.setAttribute('aria-pressed', String(selected));
        });
    }

    function updateOptionAvailability() {
        if (!selection) return;
        const keys = ['condition', 'storage', 'color', 'warranty'];
        detail.querySelectorAll('[data-select-variant]').forEach(button => {
            const kind = button.dataset.variantKind;
            const nextSelection = { ...selection, [kind]: button.dataset.variantValue };
            const exact = variants.filter(product => keys.every(key => String(dimension(product, key)) === String(nextSelection[key])));
            const available = exact.length
                ? exact.some(product => Number(product.stock) > 0)
                : variants.some(product => String(dimension(product, kind)) === button.dataset.variantValue && Number(product.stock) > 0);
            button.disabled = !available;
            button.setAttribute('aria-disabled', String(!available));
        });
    }

    function selectedLabel(kind) {
        const button = Array.from(detail.querySelectorAll('[data-select-variant]'))
            .find(option => option.dataset.variantKind === kind && option.dataset.variantValue === selection?.[kind]);
        if (!button) return '';
        if (kind === 'condition') {
            const [type, grade] = selection.condition.split('|');
            return (type === 'New' ? 'Máy mới' : 'Máy đã dùng') + (grade ? ' · ' + grade : '');
        }
        if (kind === 'storage') return button.textContent.trim();
        if (kind === 'color') return button.querySelector('small')?.textContent.trim() || selection.color;
        return button.textContent.trim();
    }

    function syncVariant(product) {
        activeProduct = product;
        selection = {
            condition: product.conditionValue,
            storage: product.storage,
            color: product.color,
            warranty: String(product.warrantyMonths)
        };
        detail.dataset.productId = product.productId;
        detail.dataset.pendingSelection = 'false';

        setText('[data-variant-name]', product.modelSeries);
        setText('[data-variant-description]', product.description);
        setText('[data-variant-full-name]', product.productName);
        setText('[data-variant-long-description]', product.description);
        setText('[data-variant-long-condition]', product.conditionDescription);
        setText('[data-variant-product-code]', product.productId);
        setText('[data-variant-caption-id]', product.productId);
        setText('[data-variant-sku]', product.sku);
        setText('[data-variant-color]', product.color);
        setText('[data-current-color]', product.color);
        setText('[data-current-condition]', product.conditionText);
        setText('[data-current-storage]', product.storage + ' GB');
        setText('[data-variant-warranty-label]', product.warrantyText);
        setText('[data-variant-condition-description]', product.conditionDescription);

        const image = detail.querySelector('[data-variant-image]');
        const fallback = detail.querySelector('.phone-gallery__fallback');
        if (image) {
            const nextImage = new URL(product.image || '', window.location.href).href;
            if (image.src !== nextImage) image.src = product.image || '';
            image.alt = product.productName || product.modelSeries;
            image.style.display = '';
        }
        if (fallback) fallback.hidden = true;

        setText('[data-variant-price]', formatPrice(product.price));
        detail.querySelector('[data-variant-price]')?.classList.remove('is-variant-unavailable');
        const original = detail.querySelector('[data-variant-original]');
        const savings = detail.querySelector('[data-variant-savings]');
        const hasDiscount = Number(product.originalPrice) > Number(product.price);
        if (original) {
            original.textContent = formatPrice(product.originalPrice);
            original.style.display = hasDiscount ? '' : 'none';
        }
        if (savings) {
            savings.textContent = hasDiscount ? 'Tiết kiệm ' + formatPrice(product.originalPrice - product.price) : '';
            savings.style.display = hasDiscount ? '' : 'none';
        }

        const stock = detail.querySelector('[data-variant-stock]');
        if (stock) {
            const available = Number(product.stock) > 0;
            stock.classList.toggle('is-available', available);
            stock.classList.toggle('is-unavailable', !available);
            stock.classList.remove('is-configuration-unavailable');
            const icon = document.createElement('i');
            icon.className = 'fa-solid ' + (available ? 'fa-circle-check' : 'fa-circle-xmark');
            icon.setAttribute('aria-hidden', 'true');
            stock.replaceChildren(icon, document.createTextNode(available
                ? ' Còn ' + product.stock + ' máy trong kho'
                : ' Tạm hết hàng'));
        }

        const specs = {
            screen: [product.screen, product.resolution].filter(Boolean).join(' · '),
            chip: product.chipset,
            ram: product.ram,
            storage: product.storageLabel,
            camera: product.camera,
            battery: [product.battery, product.chargingTechnology].filter(Boolean).join(' · '),
            ports: product.ports,
            weight: product.weight ? product.weight + ' g' : '',
            condition: product.conditionText,
            warranty: product.warrantyText
        };
        Object.entries(specs).forEach(([key, value]) => {
            const node = detail.querySelector('[data-variant-spec="' + key + '"]');
            if (node) node.textContent = value || 'Đang cập nhật';
        });

        detail.querySelectorAll('[data-add-cart], [data-buy-now]').forEach(button => {
            button.dataset.productId = product.productId;
            button.dataset.name = product.productName;
            button.dataset.price = String(product.price);
            button.dataset.image = product.image || '';
            button.dataset.stock = String(product.stock);
            button.dataset.color = product.color || '';
            button.dataset.condition = product.conditionText || '';
            button.dataset.storage = product.storageLabel || '';
            button.dataset.warranty = product.warrantyText || '';
            button.disabled = Number(product.stock) < 1;
        });

        const reviewId = document.getElementById('review-product-id');
        if (reviewId) reviewId.value = product.productId;
        const messageLink = detail.querySelector('[data-variant-message]');
        if (messageLink) messageLink.href = product.smsUrl || '#';

        markSelectedOptions();
        updateOptionAvailability();

        if (product.detailsUrl) history.replaceState(null, '', product.detailsUrl);
    }

    function selectVariant(button) {
        if (!activeProduct || !selection) return;

        const kind = button.dataset.variantKind;
        const value = button.dataset.variantValue;
        const nextSelection = { ...selection, [kind]: value };
        const keys = ['condition', 'storage', 'color', 'warranty'];
        const match = variants.find(product => keys.every(key => String(dimension(product, key)) === String(nextSelection[key])));
        if (match) {
            syncVariant(match);
            return;
        }

        selection = nextSelection;
        detail.dataset.pendingSelection = 'true';
        markSelectedOptions();
        updateOptionAvailability();
        setText('[data-current-condition]', selectedLabel('condition'));
        setText('[data-current-storage]', selectedLabel('storage'));
        setText('[data-current-color]', selectedLabel('color'));
        setText('[data-variant-color]', selectedLabel('color'));
        setText('[data-variant-warranty-label]', selectedLabel('warranty'));
        setText('[data-variant-price]', '—');
        detail.querySelector('[data-variant-price]')?.classList.add('is-variant-unavailable');
        setText('[data-variant-product-code]', '—');
        setText('[data-variant-caption-id]', '—');
        setText('[data-variant-sku]', '—');
        setText('[data-variant-description]', 'Tổ hợp này chưa có trong danh mục sản phẩm.');
        setText('[data-variant-full-name]', [activeProduct.modelSeries, selectedLabel('condition'), selectedLabel('storage'), selectedLabel('color')].filter(Boolean).join(' · '));
        setText('[data-variant-long-description]', 'Tổ hợp này chưa có trong danh mục sản phẩm.');
        setText('[data-variant-condition-description]', 'Hãy chọn tổ hợp có sẵn để xem giá và thông tin chi tiết.');
        setText('[data-variant-long-condition]', 'Tổ hợp này chưa có trong danh mục sản phẩm.');
        setText('[data-variant-spec="storage"]', selectedLabel('storage'));
        setText('[data-variant-spec="condition"]', selectedLabel('condition'));
        setText('[data-variant-spec="warranty"]', selectedLabel('warranty'));

        const original = detail.querySelector('[data-variant-original]');
        const savings = detail.querySelector('[data-variant-savings]');
        if (original) original.style.display = 'none';
        if (savings) savings.style.display = 'none';
        const stock = detail.querySelector('[data-variant-stock]');
        if (stock) {
            stock.classList.remove('is-available');
            stock.classList.add('is-unavailable', 'is-configuration-unavailable');
            const icon = document.createElement('i');
            icon.className = 'fa-solid fa-circle-info';
            icon.setAttribute('aria-hidden', 'true');
            stock.replaceChildren(icon, document.createTextNode(' Chưa có tổ hợp này. Hãy đổi lựa chọn.'));
        }
        const image = detail.querySelector('[data-variant-image]');
        const fallback = detail.querySelector('.phone-gallery__fallback');
        if (image) image.style.display = 'none';
        if (fallback) {
            fallback.hidden = false;
            const message = fallback.querySelector('span');
            if (message) message.textContent = 'Chọn tổ hợp có sẵn để xem hình ảnh.';
        }
        const messageLink = detail.querySelector('[data-variant-message]');
        if (messageLink) {
            const message = `Tôi muốn tư vấn ${activeProduct.modelSeries}: ${selectedLabel('condition')}, ${selectedLabel('storage')}, ${selectedLabel('color')}, bảo hành ${selectedLabel('warranty')}.`;
            messageLink.href = 'sms:0901176222?body=' + encodeURIComponent(message);
        }
        detail.querySelectorAll('[data-add-cart], [data-buy-now]').forEach(action => { action.disabled = true; });
    }

    document.addEventListener('click', event => {
        const option = event.target.closest('[data-select-variant]');
        if (!option) return;
        event.preventDefault();
        selectVariant(option);
    });

    document.querySelectorAll('[data-add-cart], [data-buy-now]').forEach(button => {
        button.addEventListener('click', function () {
            const item = {
                productId: this.dataset.productId,
                name: this.dataset.name,
                price: Number(this.dataset.price),
                image: this.dataset.image,
                stock: Number(this.dataset.stock),
                color: this.dataset.color,
                condition: this.dataset.condition,
                storage: this.dataset.storage,
                warranty: this.dataset.warranty
            };

            if (!item.stock) {
                window.showToast?.('Sản phẩm hiện đã hết hàng.');
                return;
            }

            window.PhoneStoreCart?.add(item);
            if (this.hasAttribute('data-buy-now')) {
                window.location.href = '/DienThoai/Cart';
                return;
            }

            window.showToast?.('Đã thêm ' + item.name + ' vào giỏ hàng.');
        });
    });
});
