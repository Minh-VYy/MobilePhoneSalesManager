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
    if (document.body.classList.contains('admin-body')) return;
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

    // 8. Mobile Navigation Drawer Toggle + Vertical Proximity Spring
    // Translated from ThreeUI topDockController.ts — glass variant, axis=y
    const mobileToggleBtn = document.getElementById('mobile-menu-toggle');
    const mobileDrawer    = document.getElementById('mobile-drawer-menu');

    if (mobileToggleBtn && mobileDrawer) {
        const iconOpen  = mobileToggleBtn.querySelector('.icon-open');
        const iconClose = mobileToggleBtn.querySelector('.icon-close');

        // Spring state per item (mirrors ItemState in topDockController.ts)
        let itemStates = [];

        // Exact smoothstep from topDockController.ts
        function smoothstepDrawer(x) {
            const t = Math.max(0, Math.min(1, x));
            return t * t * (3 - 2 * t);
        }

        function drawerIsDark() {
            const a = document.documentElement.getAttribute('data-theme');
            if (a === 'dark')  return true;
            if (a === 'light') return false;
            return !window.matchMedia('(prefers-color-scheme: light)').matches;
        }

        const drawerContent = mobileDrawer.querySelector('.atd-mobile-drawer__content');
        const drawerItems   = drawerContent
            ? Array.from(drawerContent.querySelectorAll('.atd-mobile-item'))
            : [];

        // Measure natural height of every item (run after drawer becomes visible)
        function measureDrawerItems() {
            drawerItems.forEach(el => { el.style.height = ''; });
            // Force layout read
            drawerItems.forEach(el => void el.getBoundingClientRect());
            itemStates = drawerItems.map(el => ({
                baseH:    el.getBoundingClientRect().height,
                value:    0,
                velocity: 0
            }));
        }

        // ThreeUI glass variant params — tuned for snappy feel
        const D_PROXIMITY     = 60;   // px — wider radius so items react earlier
        const D_HEIGHT_GROWTH = 24;   // px — more dramatic height growth
        const D_SPRING        = 0.32; // higher = faster approach (ThreeUI default 0.19 feels laggy)
        const D_DAMPING       = 0.72; // slight overshoot then settle

        let dPointerY   = -9999;
        let dHovering   = false;
        let dRaf        = null;

        function drawerTick() {
            let needsMore = false;
            const coarse  = window.matchMedia('(pointer: coarse)').matches;
            const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            const dark    = drawerIsDark();

            drawerItems.forEach((el, i) => {
                const s = itemStates[i];
                if (!s) return;

                // Influence: smoothstep of clamp(1 - dist/proximity, 0, 1)
                let target = 0;
                if (dHovering && !coarse && !reduced) {
                    const r    = el.getBoundingClientRect();
                    const cy   = r.top + r.height / 2;
                    const dist = Math.abs(dPointerY - cy);
                    target = smoothstepDrawer(1 - dist / D_PROXIMITY);
                }

                // Exact spring arithmetic from topDockController.ts
                s.velocity += (target - s.value) * D_SPRING;
                s.velocity *= D_DAMPING;
                s.value    += s.velocity;
                const v = Math.max(0, s.value);

                // Primary effect: write element height directly
                el.style.height = (s.baseH + v * D_HEIGHT_GROWTH).toFixed(2) + 'px';

                // Glass background, border, text — theme-adaptive (Aperture dock style)
                if (!el.classList.contains('active')) {
                    if (dark) {
                        el.style.backgroundColor = 'rgba(255,255,255,' + (v * 0.18).toFixed(3) + ')';
                        el.style.borderColor     = v > 0.04 ? 'rgba(255,255,255,' + (v * 0.25).toFixed(3) + ')' : '';
                        el.style.color           = 'rgba(255,255,255,' + (0.80 + v * 0.20).toFixed(3) + ')';
                    } else {
                        el.style.backgroundColor = 'rgba(90,84,232,' + (v * 0.14).toFixed(3) + ')';
                        el.style.borderColor     = v > 0.04 ? 'rgba(90,84,232,' + (v * 0.25).toFixed(3) + ')' : '';
                        el.style.color           = 'rgba(20,26,46,' + (0.80 + v * 0.20).toFixed(3) + ')';
                    }
                }

                const icon = el.querySelector('i');
                if (icon && !el.classList.contains('active')) {
                    icon.style.color = dark
                        ? 'rgba(255,255,255,' + (0.65 + v * 0.35).toFixed(3) + ')'
                        : 'rgba(90,84,232,' + (0.60 + v * 0.40).toFixed(3) + ')';
                }

                if (Math.abs(s.velocity) > 0.0008) needsMore = true;
            });

            dRaf = (dHovering || needsMore) ? requestAnimationFrame(drawerTick) : null;
        }

        function startDrawerSpring() {
            if (!dRaf) dRaf = requestAnimationFrame(drawerTick);
        }

        if (drawerContent) {
            drawerContent.addEventListener('pointermove', e => {
                dPointerY = e.clientY;
                dHovering = true;
                startDrawerSpring();
            }, { passive: true });

            drawerContent.addEventListener('pointerleave', () => {
                dHovering = false;
                startDrawerSpring();
            }, { passive: true });
        }

        function toggleMobileMenu(show) {
            const isOpen = show !== undefined ? show : !mobileDrawer.classList.contains('is-open');
            if (isOpen) {
                mobileDrawer.classList.add('is-open');
                mobileDrawer.setAttribute('aria-hidden', 'false');
                mobileDrawer.inert = false;
                mobileToggleBtn.setAttribute('aria-expanded', 'true');
                if (iconOpen)  iconOpen.style.display  = 'none';
                if (iconClose) iconClose.style.display = 'inline-block';
                // Measure heights after drawer is visible, then start spring
                requestAnimationFrame(() => {
                    measureDrawerItems();
                    startDrawerSpring();
                });
            } else {
                mobileDrawer.classList.remove('is-open');
                mobileDrawer.setAttribute('aria-hidden', 'true');
                mobileDrawer.inert = true;
                mobileToggleBtn.setAttribute('aria-expanded', 'false');
                if (iconOpen)  iconOpen.style.display  = 'inline-block';
                if (iconClose) iconClose.style.display = 'none';
                dHovering = false;
                dPointerY = -9999;
                startDrawerSpring();
            }
        }

        mobileToggleBtn.addEventListener('click', e => { e.stopPropagation(); toggleMobileMenu(); });
        document.addEventListener('click', e => {
            if (!mobileDrawer.contains(e.target) && !mobileToggleBtn.contains(e.target))
                toggleMobileMenu(false);
        });
        document.addEventListener('keydown', e => { if (e.key === 'Escape') toggleMobileMenu(false); });
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

    // Reveal product cards in a short stagger as each product grid enters view.
    const productGrids = document.querySelectorAll('.cat-products-grid, .product-grid');
    if (productGrids.length && 'IntersectionObserver' in window) {
        const productObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('product-card-visible');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -32px 0px' });

        productGrids.forEach(grid => {
            grid.querySelectorAll('.cat-product-card, .product-card').forEach((card, index) => {
                card.style.setProperty('--product-delay', `${Math.min(index, 4) * 75}ms`);
                card.classList.add('product-card-reveal');
                productObserver.observe(card);
            });
        });
    }
    // Database driven hero advertising carousel: copy and image advance together.
    const heroCarousel = document.querySelector('[data-carousel]');
    if (heroCarousel) {
        const slides = Array.from(heroCarousel.querySelectorAll('.ps-phone-img'));
        const dots = Array.from(heroCarousel.querySelectorAll('[data-slide-to]'));
        const copies = Array.from(document.querySelectorAll('[data-ad-copy]'));
        const toggle = heroCarousel.querySelector('[data-carousel-toggle]');
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
        let activeSlide = 0;
        let paused = reducedMotion.matches;
        let timer;
        let touchStartX = null;

        const showSlide = (index) => {
            if (!slides.length) return;
            activeSlide = (index + slides.length) % slides.length;
            slides.forEach((slide, i) => slide.classList.toggle('is-active', i === activeSlide));
            copies.forEach((copy, i) => {
                const active = i === activeSlide;
                copy.classList.toggle('is-active', active);
                copy.setAttribute('aria-hidden', String(!active));
            });
            dots.forEach((dot, i) => {
                const active = i === activeSlide;
                dot.classList.toggle('is-active', active);
                dot.setAttribute('aria-pressed', String(active));
            });
        };
        const stopTimer = () => window.clearTimeout(timer);
        const startTimer = () => {
            stopTimer();
            if (!paused && !document.hidden && !reducedMotion.matches && slides.length > 1) {
                const seconds = Number(slides[activeSlide]?.dataset.duration) || 6;
                timer = window.setTimeout(() => { showSlide(activeSlide + 1); startTimer(); }, seconds * 1000);
            }
        };
        const setPaused = (value) => {
            paused = value;
            if (toggle) {
                toggle.setAttribute('aria-pressed', String(paused));
                toggle.setAttribute('aria-label', paused ? 'Phát trình chiếu' : 'Tạm dừng trình chiếu');
                const icon = toggle.querySelector('span');
                if (icon) icon.textContent = paused ? '▶' : 'Ⅱ';
            }
            startTimer();
        };

        dots.forEach((dot, i) => dot.addEventListener('click', () => { showSlide(i); startTimer(); }));
        toggle?.addEventListener('click', () => setPaused(!paused));
        heroCarousel.addEventListener('mouseenter', stopTimer);
        heroCarousel.addEventListener('mouseleave', startTimer);
        heroCarousel.addEventListener('focusin', stopTimer);
        heroCarousel.addEventListener('focusout', startTimer);
        heroCarousel.addEventListener('touchstart', event => { touchStartX = event.changedTouches[0]?.clientX ?? null; }, { passive: true });
        heroCarousel.addEventListener('touchend', event => {
            if (touchStartX === null) return;
            if (event.target.closest('.ps-carousel-controls')) { touchStartX = null; return; }
            const delta = event.changedTouches[0].clientX - touchStartX;
            if (Math.abs(delta) > 45) { showSlide(activeSlide + (delta < 0 ? 1 : -1)); startTimer(); }
            touchStartX = null;
        }, { passive: true });
        document.addEventListener('visibilitychange', startTimer);
        reducedMotion.addEventListener?.('change', event => setPaused(event.matches));
        showSlide(0);
        startTimer();
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
                    const slides = Array.from(document.querySelectorAll('.ps-phone-carousel .ps-phone-img'));
                    const slideIndex = slides.findIndex(slide => slide.getAttribute('src') === imageSrc);
                    const targetDot = document.querySelector(`.ps-phone-carousel [data-slide-to="${slideIndex}"]`);
                    if (slideIndex >= 0 && targetDot) targetDot.click();
                }

                showToast(`Đã chọn màu: ${colorName}`);
            });
        });
    }

    // 12. Interactive 3D Peek Stack Card Hover Elevate & Preview
    const peekCards = document.querySelectorAll('.ps-peek-card');
    if (peekCards.length) {
        let currentMaxZ = 50;
        peekCards.forEach(card => {
            card.addEventListener('mouseenter', function () {
                currentMaxZ++;
                this.style.zIndex = currentMaxZ;
                this.classList.add('is-top');
            });

            card.addEventListener('mouseleave', function () {
                this.classList.remove('is-top');
            });

            // Smooth click preview to main phone hero stage if available
            card.addEventListener('click', function (e) {
                const imgUrl = this.getAttribute('data-img');
                if (imgUrl && psHeroPhone && !e.target.closest('a[href^="#"]')) {
                    psHeroPhone.style.opacity = '0.2';
                    psHeroPhone.style.transform = 'scale(0.92) translateY(8px)';
                    setTimeout(() => {
                        psHeroPhone.src = imgUrl;
                        psHeroPhone.style.opacity = '1';
                        psHeroPhone.style.transform = '';
                    }, 220);
                }
            });
        });
    }

    // 13. Dynamic Flash Sale Countdown Timer
    const hoursEl = document.getElementById('flashHours');
    const minsEl  = document.getElementById('flashMins');
    const secsEl  = document.getElementById('flashSecs');

    if (hoursEl && minsEl && secsEl) {
        let totalSeconds = 5 * 3600 + 42 * 60 + 19;
        setInterval(() => {
            if (totalSeconds <= 0) {
                totalSeconds = 24 * 3600;
            } else {
                totalSeconds--;
            }
            const h = Math.floor(totalSeconds / 3600);
            const m = Math.floor((totalSeconds % 3600) / 60);
            const s = totalSeconds % 60;

            hoursEl.textContent = String(h).padStart(2, '0');
            minsEl.textContent  = String(m).padStart(2, '0');
            secsEl.textContent  = String(s).padStart(2, '0');
        }, 1000);
    }
});


