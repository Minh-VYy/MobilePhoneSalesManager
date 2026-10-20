(() => {
    const root = document.querySelector('[data-banner-editor]');
    if (!root) return;
    const find = name => root.querySelector(`[data-banner-${name}]`);
    const image = find('preview-image'), empty = find('preview-empty');
    let lastSource;
    function update() {
        find('preview-title').textContent = find('title').value.trim() || 'Tiêu đề banner của bạn';
        find('preview-description').textContent = find('description').value.trim() || 'Nội dung giới thiệu sản phẩm hoặc chương trình của cửa hàng.';
        const source = find('image').value.trim().replace(/^~\//, root.dataset.base);
        if (source === lastSource) return;
        lastSource = source;
        image.hidden = true; empty.hidden = false;
        if (!source) { image.removeAttribute('src'); empty.textContent = 'Nhập đường dẫn để xem trước hình ảnh.'; return; }
        try {
            const url = new URL(source, location.origin);
            if (!['http:', 'https:'].includes(url.protocol)) throw new Error();
            empty.textContent = 'Đang mở hình ảnh…'; image.src = url.href;
        } catch { image.removeAttribute('src'); empty.textContent = 'Đường dẫn hình ảnh chưa hợp lệ.'; }
    }
    image.addEventListener('load', () => { image.hidden = false; empty.hidden = true; });
    image.addEventListener('error', () => { image.hidden = true; empty.hidden = false; empty.textContent = 'Không tải được ảnh. Hãy kiểm tra lại đường dẫn.'; });
    ['title', 'description'].forEach(name => find(name).addEventListener('input', update));
    find('image').addEventListener('change', update);
    update();
})();
