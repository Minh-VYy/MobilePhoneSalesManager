document.addEventListener('DOMContentLoaded', () => {
    // 1. SCROLL REVEAL ANIMATION WITH STAGGER EFFECT (INTERSECTION OBSERVER API)
    const revealElements = document.querySelectorAll('.scroll-reveal');
    
    if ('IntersectionObserver' in window && revealElements.length > 0) {
        const observerOptions = {
            root: null,
            rootMargin: '0px 0px -40px 0px',
            threshold: 0.15
        };

        const revealObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const el = entry.target;
                    // Lấy stagger delay từ data-delay nếu có
                    const delay = el.getAttribute('data-delay') || 0;
                    setTimeout(() => {
                        el.classList.add('revealed');
                    }, delay);
                    // Đã reveal xong thì unobserve
                    observer.unobserve(el);
                }
            });
        }, observerOptions);

        revealElements.forEach((el, index) => {
            // Tự động gán stagger delay nếu chưa gán
            if (!el.hasAttribute('data-delay')) {
                const staggerDelay = (index % 6) * 75; // Thời gian lệch nhau 75ms
                el.setAttribute('data-delay', staggerDelay);
            }
            revealObserver.observe(el);
        });
    } else {
        // Fallback nếu trình duyệt cũ không hỗ trợ IntersectionObserver
        revealElements.forEach(el => el.classList.add('revealed'));
    }

    // 2. POS AUTO CALCULATE SUBTOTALS
    const itemSelects = document.querySelectorAll('.pos-item-select');
    itemSelects.forEach(select => {
        select.addEventListener('change', updatePosSubtotals);
    });

    const qtyInputs = document.querySelectorAll('.pos-qty-input');
    qtyInputs.forEach(input => {
        input.addEventListener('input', updatePosSubtotals);
    });

    function updatePosSubtotals() {
        let grandTotal = 0;
        const rows = document.querySelectorAll('.pos-item-row');
        
        rows.forEach(row => {
            const select = row.querySelector('.pos-item-select');
            const qtyInput = row.querySelector('.pos-qty-input');
            const priceCell = row.querySelector('.pos-price-cell');
            const subtotalCell = row.querySelector('.pos-subtotal-cell');

            if (select && qtyInput && priceCell && subtotalCell) {
                const selectedOption = select.options[select.selectedIndex];
                const price = parseFloat(selectedOption.getAttribute('data-price') || 0);
                const qty = parseInt(qtyInput.value || 0);
                const subtotal = price * qty;

                priceCell.textContent = price > 0 ? price.toLocaleString('vi-VN') + ' VNĐ' : '0 VNĐ';
                subtotalCell.textContent = subtotal > 0 ? subtotal.toLocaleString('vi-VN') + ' VNĐ' : '0 VNĐ';

                grandTotal += subtotal;
            }
        });

        const grandTotalElement = document.getElementById('posGrandTotal');
        if (grandTotalElement) {
            grandTotalElement.textContent = grandTotal.toLocaleString('vi-VN') + ' VNĐ';
        }
    }

    // Run POS calculation on initial load
    updatePosSubtotals();
});

// Print Invoice Function
function printInvoice() {
    window.print();
}
