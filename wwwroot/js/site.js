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

// Scroll Reveal & Dynamic Interactions (Kage Architecture Engine)
document.addEventListener('DOMContentLoaded', function () {
    // 1. Custom Animated Cursor Dot
    const cursor = document.createElement('div');
    cursor.className = 'cur-dot';
    document.body.appendChild(cursor);

    let mouseX = 0, mouseY = 0, curX = 0, curY = 0;
    document.addEventListener('mousemove', e => {
        mouseX = e.clientX;
        mouseY = e.clientY;
    });

    function animateCursor() {
        curX += (mouseX - curX) * 0.15;
        curY += (mouseY - curY) * 0.15;
        cursor.style.transform = `translate3d(${curX}px, ${curY}px, 0)`;
        requestAnimationFrame(animateCursor);
    }
    animateCursor();

    // Hover expansion for interactive elements
    const hoverTargets = 'a, button, input, select, .product-card-3d, .spec-box, .swatch-btn, .chip';
    document.addEventListener('mouseover', e => {
        if (e.target.closest(hoverTargets)) {
            cursor.classList.add('act');
        }
    });
    document.addEventListener('mouseout', e => {
        if (e.target.closest(hoverTargets)) {
            cursor.classList.remove('act');
        }
    });

    // 2. Interactive Canvas Background Particle Engine (#gl-tech)
    const canvas = document.getElementById('gl-tech');
    if (canvas) {
        const ctx = canvas.getContext('2d');
        let width = canvas.width = window.innerWidth;
        let height = canvas.height = window.innerHeight;

        window.addEventListener('resize', () => {
            width = canvas.width = window.innerWidth;
            height = canvas.height = window.innerHeight;
        });

        const particles = Array.from({ length: 45 }, () => ({
            x: Math.random() * width,
            y: Math.random() * height,
            radius: Math.random() * 2 + 0.8,
            vx: (Math.random() - 0.5) * 0.4,
            vy: (Math.random() - 0.5) * 0.4,
            alpha: Math.random() * 0.5 + 0.2
        }));

        function renderCanvas() {
            ctx.clearRect(0, 0, width, height);
            
            // Draw floating tech particles & connecting lines
            for (let i = 0; i < particles.length; i++) {
                const p = particles[i];
                p.x += p.vx;
                p.y += p.vy;

                if (p.x < 0) p.x = width;
                if (p.x > width) p.x = 0;
                if (p.y < 0) p.y = height;
                if (p.y > height) p.y = 0;

                ctx.beginPath();
                ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
                ctx.fillStyle = `rgba(108, 107, 245, ${p.alpha})`;
                ctx.fill();

                for (let j = i + 1; j < particles.length; j++) {
                    const p2 = particles[j];
                    const dist = Math.hypot(p.x - p2.x, p.y - p2.y);
                    if (dist < 130) {
                        ctx.beginPath();
                        ctx.moveTo(p.x, p.y);
                        ctx.lineTo(p2.x, p2.y);
                        ctx.strokeStyle = `rgba(0, 194, 168, ${(1 - dist / 130) * 0.15})`;
                        ctx.lineWidth = 0.6;
                        ctx.stroke();
                    }
                }
            }
            requestAnimationFrame(renderCanvas);
        }
        renderCanvas();
    }

    // 3. Word-by-Word Clip-Mask Reveal Builder
    document.querySelectorAll('.word-reveal').forEach(heading => {
        const text = heading.textContent.trim();
        heading.textContent = '';
        const words = text.split(/\s+/);
        
        words.forEach((wordText, i) => {
            const wordMask = document.createElement('span');
            wordMask.className = 'word-mask';
            
            const wordSpan = document.createElement('span');
            wordSpan.className = 'word';
            wordSpan.style.setProperty('--word-delay', `${i * 65}ms`);
            wordSpan.textContent = wordText + (i < words.length - 1 ? '\u00A0' : '');
            
            wordMask.appendChild(wordSpan);
            heading.appendChild(wordMask);
        });
    });

    // 4. Scroll Reveal Observer ([data-rv])
    // Use threshold:0 + rootMargin so elements already in viewport fire immediately on load
    const revealObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                // Apply stagger delay from parent card if set
                const card = entry.target.closest('.ps-card');
                const delay = card ? getComputedStyle(card).getPropertyValue('--card-delay').trim() : null;
                if (delay) entry.target.style.transitionDelay = delay;

                entry.target.classList.add('rv-in');
                entry.target.classList.add('is-visible');
                revealObserver.unobserve(entry.target); // once is enough
            }
        });
    }, {
        root: null,
        threshold: 0,
        rootMargin: '0px 0px -60px 0px'
    });

    // Small rAF delay so browser has laid out before we observe
    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            document.querySelectorAll('[data-rv], section.sec-chapter, .reveal-on-scroll').forEach(el => {
                revealObserver.observe(el);
            });
        });
    });

    // 5. Color Swatch Interactivity for Flagship Phone
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

    // 6. Animated Top Dock Proximity Spring Physics Engine (ThreeUI Spec)
    const dockContainer = document.querySelector('.atd-modern__dock');
    if (dockContainer) {
        const items = Array.from(dockContainer.querySelectorAll('.atd-modern__item'));
        const PROXIMITY = 120;
        const MAX_SCALE = 1.15;
        let animFrame = null;
        let isHovering = false;
        let pointerX = 0;

        const itemStates = items.map(() => ({ targetScale: 1, currentScale: 1, translateY: 0, targetY: 0 }));

        function updateProximity() {
            let activeAnim = false;
            items.forEach((item, index) => {
                const rect = item.getBoundingClientRect();
                const itemCenterX = rect.left + rect.width / 2;
                const state = itemStates[index];

                if (isHovering) {
                    const dist = Math.abs(pointerX - itemCenterX);
                    if (dist < PROXIMITY) {
                        const factor = Math.cos((dist / PROXIMITY) * (Math.PI / 2));
                        state.targetScale = 1 + (MAX_SCALE - 1) * factor;
                        state.targetY = -3.5 * factor;
                    } else {
                        state.targetScale = 1;
                        state.targetY = 0;
                    }
                } else {
                    state.targetScale = 1;
                    state.targetY = 0;
                }

                state.currentScale += (state.targetScale - state.currentScale) * 0.22;
                state.translateY += (state.targetY - state.translateY) * 0.22;

                if (Math.abs(state.targetScale - state.currentScale) > 0.001 || Math.abs(state.targetY - state.translateY) > 0.01) {
                    activeAnim = true;
                }

                item.style.transform = `scale(${state.currentScale.toFixed(3)}) translateY(${state.translateY.toFixed(2)}px)`;
            });

            if (isHovering || activeAnim) {
                animFrame = requestAnimationFrame(updateProximity);
            } else {
                animFrame = null;
            }
        }

        dockContainer.addEventListener('mousemove', (e) => {
            pointerX = e.clientX;
            isHovering = true;
            if (!animFrame) animFrame = requestAnimationFrame(updateProximity);
        });

        dockContainer.addEventListener('mouseleave', () => {
            isHovering = false;
            if (!animFrame) animFrame = requestAnimationFrame(updateProximity);
        });
    }

    // 7. Scroll-Triggered Dynamic Glassmorphism Header
    const headerWrapper = document.querySelector('.atd-modern-wrapper');
    if (headerWrapper) {
        function updateHeaderGlassOnScroll() {
            if (window.scrollY > 15) {
                headerWrapper.classList.add('is-scrolled');
            } else {
                headerWrapper.classList.remove('is-scrolled');
            }
        }
        window.addEventListener('scroll', updateHeaderGlassOnScroll, { passive: true });
        updateHeaderGlassOnScroll();
    }

    // 8. Mobile Navigation Drawer Toggle Handler
    const mobileToggleBtn = document.getElementById('mobile-menu-toggle');
    const mobileDrawer = document.getElementById('mobile-drawer-menu');

    if (mobileToggleBtn && mobileDrawer) {
        const iconOpen = mobileToggleBtn.querySelector('.icon-open');
        const iconClose = mobileToggleBtn.querySelector('.icon-close');

        function toggleMobileMenu(show) {
            const isOpen = show !== undefined ? show : !mobileDrawer.classList.contains('is-open');
            if (isOpen) {
                mobileDrawer.classList.add('is-open');
                mobileToggleBtn.setAttribute('aria-expanded', 'true');
                if (iconOpen) iconOpen.style.display = 'none';
                if (iconClose) iconClose.style.display = 'inline-block';
            } else {
                mobileDrawer.classList.remove('is-open');
                mobileToggleBtn.setAttribute('aria-expanded', 'false');
                if (iconOpen) iconOpen.style.display = 'inline-block';
                if (iconClose) iconClose.style.display = 'none';
            }
        }

        mobileToggleBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            toggleMobileMenu();
        });

        document.addEventListener('click', (e) => {
            if (!mobileDrawer.contains(e.target) && !mobileToggleBtn.contains(e.target)) {
                toggleMobileMenu(false);
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') toggleMobileMenu(false);
        });
    }

    // 9. Animated Number Counters for Stat Cards
    function animateCounter(el) {
        const target = parseInt(el.getAttribute('data-count') || '0', 10);
        const suffix = el.getAttribute('data-suffix') || '';
        const duration = 1400;
        const start = performance.now();

        function step(now) {
            const elapsed = now - start;
            const progress = Math.min(elapsed / duration, 1);
            // ease out cubic
            const eased = 1 - Math.pow(1 - progress, 3);
            const current = Math.round(eased * target);
            el.textContent = current.toLocaleString('vi-VN') + suffix;
            if (progress < 1) requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    }

    const counterObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const valEl = entry.target.querySelector('[data-count]');
                if (valEl && !valEl.dataset.counted) {
                    valEl.dataset.counted = '1';
                    animateCounter(valEl);
                }
            }
        });
    }, { threshold: 0.35 });

    document.querySelectorAll('.ps-stat-card').forEach(card => counterObserver.observe(card));

    // 10. Progress Rail — track active section
    const railBtns = document.querySelectorAll('.rail button[data-target]');
    const railSections = Array.from(railBtns)
        .map(btn => document.getElementById(btn.dataset.target))
        .filter(Boolean);

    if (railSections.length) {
        const railObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const id = entry.target.id;
                    railBtns.forEach(btn => {
                        btn.classList.toggle('on', btn.dataset.target === id);
                    });
                }
            });
        }, { rootMargin: '-40% 0px -40% 0px', threshold: 0 });

        railSections.forEach(sec => railObserver.observe(sec));

        railBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                const target = document.getElementById(btn.dataset.target);
                if (target) target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            });
        });
    }

    // 11. New Color Swatch (ps-swatch) interactivity
    const psSwatches = document.querySelectorAll('.ps-swatch');
    const psHeroPhone = document.getElementById('hero-main-phone');

    if (psSwatches.length && psHeroPhone) {
        psSwatches.forEach(btn => {
            btn.addEventListener('click', function () {
                psSwatches.forEach(b => b.classList.remove('active'));
                this.classList.add('active');

                const colorName = this.getAttribute('data-color-name');
                const imageSrc = this.getAttribute('data-img');

                if (imageSrc) {
                    psHeroPhone.style.opacity = '0.2';
                    psHeroPhone.style.transform = 'scale(0.94) translateY(6px)';
                    setTimeout(() => {
                        psHeroPhone.src = imageSrc;
                        psHeroPhone.style.opacity = '1';
                        psHeroPhone.style.transform = '';
                    }, 220);
                }

                showToast(`Đã chọn màu: ${colorName}`);
            });
        });
    }
});


