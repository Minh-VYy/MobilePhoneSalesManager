// Theme Toggle & Dark/Light Mode Preference
(function () {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
        document.documentElement.setAttribute('data-theme', savedTheme);
    }

    document.addEventListener('DOMContentLoaded', function () {
        const themeBtn = document.getElementById('theme-toggle');
        if (themeBtn) {
            themeBtn.addEventListener('click', function () {
                const currentTheme = document.documentElement.getAttribute('data-theme') || 
                    (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
                const nextTheme = currentTheme === 'light' ? 'dark' : 'light';
                
                document.documentElement.setAttribute('data-theme', nextTheme);
                localStorage.setItem('theme', nextTheme);
                showToast(`Đã chuyển sang giao diện ${nextTheme === 'light' ? 'Sáng' : 'Tối'}`);
            });
        }
    });
})();

// Toast notification helper
function showToast(message) {
    let toast = document.getElementById('app-toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'app-toast';
        toast.className = 'toast';
        toast.hidden = true;
        toast.innerHTML = `
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10"/><path d="M9 12l2 2 4-4"/>
            </svg>
            <span id="toast-message"></span>
        `;
        document.body.appendChild(toast);
    }
    
    document.getElementById('toast-message').innerText = message;
    toast.hidden = false;
    toast.style.opacity = '1';
    toast.style.transform = 'translateY(0)';
    
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(8px)';
        setTimeout(() => { toast.hidden = true; }, 250);
    }, 2500);
}

// Scroll Reveal & Dynamic Interactions
document.addEventListener('DOMContentLoaded', function () {
    // 1. Intersection Observer for Scroll Animations
    const observerOptions = {
        root: null,
        threshold: 0.15,
        rootMargin: '0px 0px -40px 0px'
    };

    const scrollObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('is-visible');
            }
        });
    }, observerOptions);

    document.querySelectorAll('.reveal-on-scroll').forEach(el => {
        scrollObserver.observe(el);
    });

    // 2. Color Swatch Interactivity for Flagship Phone
    const swatchBtns = document.querySelectorAll('.swatch-btn');
    const heroPhoneImg = document.getElementById('hero-main-phone');

    if (swatchBtns.length && heroPhoneImg) {
        swatchBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                swatchBtns.forEach(b => b.classList.remove('active'));
                this.classList.add('active');

                const colorName = this.getAttribute('data-color-name');
                const imageSrc = this.getAttribute('data-img');

                if (imageSrc) {
                    heroPhoneImg.style.opacity = '0.3';
                    heroPhoneImg.style.transform = 'scale(0.95)';
                    setTimeout(() => {
                        heroPhoneImg.src = imageSrc;
                        heroPhoneImg.style.opacity = '1';
                        heroPhoneImg.style.transform = 'scale(1)';
                    }, 200);
                }

                showToast(`Đã chọn phiên bản màu: ${colorName}`);
            });
        });
    }
});

