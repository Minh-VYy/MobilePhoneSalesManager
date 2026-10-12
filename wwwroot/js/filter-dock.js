/* Filter Dock — Aperture glass proximity spring (vertical) + flyout switching
   + header mega menu toggle. Supports multiple docks per page. */
(function () {
    const smooth = x => { const t = Math.max(0, Math.min(1, x)); return t * t * (3 - 2 * t); };
    const isDark = () => {
        const a = document.documentElement.getAttribute('data-theme');
        if (a) return a === 'dark';
        return !window.matchMedia('(prefers-color-scheme: light)').matches;
    };

    // ThreeUI glass variant params — tuned snappy (same as mobile drawer)
    const PROXIMITY = 60, GROWTH = 20, SPRING = 0.32, DAMPING = 0.72;

    function initDock(dock) {
        const rail  = dock.querySelector('.fd-rail');
        const items = Array.from(rail.querySelectorAll('.fd-item'));
        const panes = Array.from(dock.querySelectorAll('.fd-pane'));
        const initial = (dock.dataset.active || 'ALL').toUpperCase();

        function showPane(key) {
            key = (key || 'ALL').toUpperCase();
            let hit = false;
            panes.forEach(p => {
                const on = p.dataset.brand.toUpperCase() === key;
                p.classList.toggle('show', on);
                if (on) hit = true;
            });
            if (!hit && panes[0]) panes[0].classList.add('show');
        }
        showPane(initial);

        items.forEach(it => {
            const open = () => showPane(it.dataset.brand);
            it.addEventListener('pointerenter', open);
            it.addEventListener('focus', open);
        });
        dock.addEventListener('pointerleave', () => showPane(initial));

        let states = [], pointerY = -9999, hovering = false, raf = null;

        function measure() {
            items.forEach(el => { el.style.height = ''; });
            states = items.map(el => ({ baseH: el.getBoundingClientRect().height, value: 0, velocity: 0 }));
        }

        function tick() {
            const coarse  = window.matchMedia('(pointer: coarse)').matches;
            const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            const dark = isDark();
            let more = false;

            items.forEach((el, i) => {
                const s = states[i]; if (!s || !s.baseH) return;
                let target = 0;
                if (hovering && !coarse && !reduced) {
                    const r = el.getBoundingClientRect();
                    target = smooth(1 - Math.abs(pointerY - (r.top + r.height / 2)) / PROXIMITY);
                }
                s.velocity += (target - s.value) * SPRING;
                s.velocity *= DAMPING;
                s.value += s.velocity;
                const v = Math.max(0, s.value);

                el.style.height = (s.baseH + v * GROWTH).toFixed(2) + 'px';
                if (!el.classList.contains('active')) {
                    el.style.backgroundColor = dark ? `rgba(255,255,255,${(v * 0.18).toFixed(3)})` : `rgba(90,84,232,${(v * 0.14).toFixed(3)})`;
                    el.style.borderColor = v > 0.04 ? (dark ? `rgba(255,255,255,${(v * 0.25).toFixed(3)})` : `rgba(90,84,232,${(v * 0.25).toFixed(3)})`) : '';
                }
                if (Math.abs(s.velocity) > 0.0008) more = true;
            });
            raf = (hovering || more) ? requestAnimationFrame(tick) : null;
        }
        const start = () => { if (!raf) raf = requestAnimationFrame(tick); };

        rail.addEventListener('pointermove', e => { pointerY = e.clientY; hovering = true; start(); }, { passive: true });
        rail.addEventListener('pointerleave', () => { hovering = false; start(); }, { passive: true });
        window.addEventListener('resize', measure);

        dock._fdMeasure = measure;
        measure();
        if (document.fonts && document.fonts.ready) document.fonts.ready.then(measure);
    }

    document.querySelectorAll('.fd').forEach(initDock);

    // ---- Header mega menu ----
    const trigger = document.getElementById('fd-trigger');
    const mega    = document.getElementById('fd-mega');
    if (trigger && mega) {
        const megaDock = mega.querySelector('.fd');
        let closeTimer = null;

        function setOpen(open) {
            clearTimeout(closeTimer);
            mega.classList.toggle('is-open', open);
            mega.setAttribute('aria-hidden', open ? 'false' : 'true');
            trigger.setAttribute('aria-expanded', open ? 'true' : 'false');
            trigger.classList.toggle('is-open', open);
            // Items have zero size while hidden — measure once visible
            if (open && megaDock && megaDock._fdMeasure) requestAnimationFrame(megaDock._fdMeasure);
        }
        const scheduleClose = () => { clearTimeout(closeTimer); closeTimer = setTimeout(() => setOpen(false), 220); };
        const canHover = window.matchMedia('(hover: hover)').matches;

        trigger.addEventListener('click', e => {
            e.stopPropagation();
            setOpen(!mega.classList.contains('is-open'));
        });
        if (canHover) {
            trigger.addEventListener('pointerenter', () => setOpen(true));
            trigger.addEventListener('pointerleave', scheduleClose);
            mega.addEventListener('pointerenter', () => clearTimeout(closeTimer));
            mega.addEventListener('pointerleave', scheduleClose);
        }
        document.addEventListener('click', e => {
            if (!mega.contains(e.target) && !trigger.contains(e.target)) setOpen(false);
        });
        document.addEventListener('keydown', e => { if (e.key === 'Escape') setOpen(false); });
    }
})();
