(() => {
    const source = document.getElementById('overview-data');
    if (!source) return;
    const daysSelect = document.getElementById('dashboard-days'), customDates = document.getElementById('overview-custom-dates');
    function dateMode() {
        customDates.hidden = daysSelect.value !== '0';
        customDates.querySelectorAll('input').forEach(input => { input.disabled = customDates.hidden; });
    }
    daysSelect.addEventListener('change', dateMode);
    dateMode();
    const data = JSON.parse(source.textContent);
    const money = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 });
    const compact = new Intl.NumberFormat('vi-VN', { notation: 'compact', maximumFractionDigits: 1 });
    const numbers = new Intl.NumberFormat('vi-VN');
    const percent = new Intl.NumberFormat('vi-VN', { style: 'percent', maximumFractionDigits: 1 });
    const color = name => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    const charts = [], events = new AbortController();
    const grouping = document.getElementById('overview-grouping'), mode = document.getElementById('overview-revenue-mode');
    const volumeMetric = document.getElementById('overview-volume-metric'), productMetric = document.getElementById('overview-product-metric');
    const inspect = document.getElementById('overview-inspect'), note = document.getElementById('overview-grouping-note');
    function listen(node, callback) { node.addEventListener('change', callback, { signal: events.signal }); }
    function fallback(section) {
        section.querySelectorAll('.overview-chart-data').forEach(details => { details.open = true; });
        if (section.querySelector('.overview-chart-fallback')) return;
        const message = document.createElement('p'); message.className = 'overview-chart-fallback';
        message.textContent = 'Biểu đồ chưa tải được. Bảng bên dưới vẫn có đầy đủ số liệu để đối chiếu.';
        section.prepend(message);
    }
    if (typeof window.Chart !== 'function') { fallback(source.parentElement); return; }
    Chart.defaults.font.family = getComputedStyle(document.body).fontFamily;
    Chart.defaults.font.size = 12;
    function options(legend = false) {
        return {
            responsive: true, maintainAspectRatio: false, animation: false,
            font: { family: getComputedStyle(document.body).fontFamily, size: 12 },
            plugins: { legend: { display: legend, position: 'bottom', labels: { boxWidth: 20, boxHeight: 8, padding: 16 } } }
        };
    }
    function create(id, config, paint) {
        const canvas = document.getElementById(id);
        if (!canvas) return null;
        const frame = canvas.closest('[data-chart-frame]'); frame.hidden = false;
        try {
            const chart = new Chart(canvas, config);
            const update = () => {
                paint(chart);
                if (chart.options.plugins.legend) chart.options.plugins.legend.labels.color = color('--text-muted');
                for (const axis of Object.values(chart.options.scales || {})) {
                    if (axis.ticks) axis.ticks.color = color('--text-muted');
                    if (axis.title) axis.title.color = color('--text-muted');
                    if (axis.grid) axis.grid.color = color('--border-light');
                    if (axis.border) axis.border.color = color('--border-default');
                }
                chart.update('none');
            };
            update(); charts.push({ chart, update }); return chart;
        } catch (error) {
            Chart.getChart(canvas)?.destroy(); frame.hidden = true; fallback(canvas.closest('.overview-chart')); return null;
        }
    }
    function groupedDays() {
        const result = [], step = Number(grouping.value);
        for (let i = 0; i < data.days.length; i += step) {
            const chunk = data.days.slice(i, i + step), first = chunk[0], last = chunk[chunk.length - 1];
            const range = key => first[key] === last[key] ? first[key] : first[key] + ' – ' + last[key];
            const sum = key => chunk.reduce((total, day) => total + day[key], 0);
            result.push({
                label: first.label.slice(0, 5) + (chunk.length > 1 ? ' – ' + last.label.slice(0, 5) : ''),
                fullLabel: range('label'), previousLabel: range('previousLabel'),
                pos: sum('pos'), online: sum('online'), transactions: sum('transactions'), units: sum('units'),
                previousRevenue: sum('previousRevenue'), previousTransactions: sum('previousTransactions'), previousUnits: sum('previousUnits')
            });
        }
        return result;
    }
    let points = groupedDays();
    let groupStep = Number(grouping.value);
    function amountAt(point) { return point.pos + point.online; }
    function timeAxes(count = false) {
        return {
            x: { grid: { display: false }, ticks: { maxTicksLimit: 7, maxRotation: 0 }, border: { display: false } },
            y: { beginAtZero: true, title: { display: true, text: count ? 'Giao dịch' : 'Đồng (₫)' }, ticks: { maxTicksLimit: 5, precision: count ? 0 : undefined, callback: value => count ? numbers.format(value) : compact.format(value) }, border: { display: false } }
        };
    }
    function lineSeries() {
        const channel = mode.value === 'channel';
        return [
            { label: channel ? 'Tại quầy' : 'Kỳ hiện tại', data: points.map(x => channel ? x.pos : amountAt(x)), borderWidth: 2, tension: 0, pointRadius: points.length > 40 ? 0 : 2, pointHitRadius: 16 },
            { label: channel ? 'Online đã giao' : 'Kỳ trước (ngày tương ứng)', data: points.map(x => channel ? x.online : x.previousRevenue), borderWidth: 2, tension: 0, borderDash: [6, 4], pointRadius: points.length > 40 ? 0 : 2, pointStyle: 'rect', pointHitRadius: 16 }
        ];
    }
    function paintLines(chart) {
        chart.data.datasets.forEach((dataset, index) => {
            dataset.borderColor = color(index === 0 ? '--primary-blue' : mode.value === 'channel' ? '--status-success' : '--text-muted');
            dataset.backgroundColor = dataset.borderColor;
        });
    }
    const revenue = create('overview-revenue-chart', {
        type: 'line', data: { labels: points.map(x => x.label), datasets: lineSeries() },
        options: {
            ...options(true), interaction: { mode: 'index', intersect: false },
            onClick: (event, items) => { if (items.length) { inspect.value = String(items[0].index); inspectPoint(); } },
            plugins: { ...options(true).plugins, tooltip: { callbacks: {
                title: items => points[items[0].dataIndex].fullLabel,
                label: item => item.dataset.label + ': ' + money.format(item.parsed.y),
                footer: items => {
                    const point = points[items[0].dataIndex];
                    return 'Kỳ trước: ' + point.previousLabel + '\nChênh lệch: ' + money.format(amountAt(point) - point.previousRevenue);
                }
            } } }, scales: timeAxes()
        }
    }, paintLines);
    function inspectPoint() {
        const point = points[Number(inspect.value)];
        if (!point) return;
        const dl = document.getElementById('overview-inspect-values'), fragment = document.createDocumentFragment();
        const cells = [
            ['Doanh thu kỳ này', money.format(amountAt(point))], ['Kỳ trước · ' + point.previousLabel, money.format(point.previousRevenue)],
            ['Chênh lệch doanh thu', money.format(amountAt(point) - point.previousRevenue)],
            ['Giao dịch kỳ này / trước', numbers.format(point.transactions) + ' / ' + numbers.format(point.previousTransactions)],
            ['Máy bán kỳ này / trước', numbers.format(point.units) + ' / ' + numbers.format(point.previousUnits)]
        ];
        cells.forEach(([label, value]) => {
            const cell = document.createElement('div'), dt = document.createElement('dt'), dd = document.createElement('dd');
            dt.textContent = label; dd.textContent = value; cell.append(dt, dd); fragment.append(cell);
        });
        dl.replaceChildren(fragment);
    }
    function refreshInspector(preferredIndex = null) {
        const fragment = document.createDocumentFragment();
        points.forEach((point, index) => { const option = document.createElement('option'); option.value = String(index); option.textContent = point.fullLabel; fragment.append(option); });
        inspect.replaceChildren(fragment);
        let selected = points.length - 1;
        for (let i = points.length - 1; i >= 0; i--) { if (points[i].transactions || points[i].previousTransactions) { selected = i; break; } }
        if (preferredIndex !== null) selected = Math.min(points.length - 1, Math.max(0, preferredIndex));
        inspect.value = String(selected); inspectPoint();
    }
    function volumeSeries() {
        const units = volumeMetric.value === 'units';
        return [
            { label: 'Kỳ hiện tại', data: points.map(x => units ? x.units : x.transactions), maxBarThickness: 24, borderRadius: 3 },
            { label: 'Kỳ trước', data: points.map(x => units ? x.previousUnits : x.previousTransactions), maxBarThickness: 24, borderRadius: 3 }
        ];
    }
    const volume = create('overview-volume-chart', {
        type: 'bar', data: { labels: points.map(x => x.label), datasets: volumeSeries() },
        options: {
            ...options(true), interaction: { mode: 'index', intersect: false }, scales: timeAxes(true),
            plugins: { ...options(true).plugins, tooltip: { callbacks: {
                title: items => points[items[0].dataIndex].fullLabel,
                label: item => item.dataset.label + ': ' + numbers.format(item.parsed.y) + (volumeMetric.value === 'units' ? ' máy' : ' giao dịch'),
                footer: items => 'Kỳ trước: ' + points[items[0].dataIndex].previousLabel
            } } }
        }
    }, chart => {
        chart.data.datasets[0].backgroundColor = color('--primary-blue');
        chart.data.datasets[1].backgroundColor = color('--text-faint');
    });
    function refreshTimeCharts() {
        const selectedOffset = inspect.options.length ? Number(inspect.value) * groupStep : null;
        points = groupedDays();
        groupStep = Number(grouping.value);
        if (revenue) {
            revenue.data.labels = points.map(x => x.label); revenue.data.datasets = lineSeries(); paintLines(revenue); revenue.update('none');
            refreshInspector(selectedOffset === null ? null : Math.floor(selectedOffset / groupStep));
        }
        if (volume) {
            volume.data.labels = points.map(x => x.label); volume.data.datasets = volumeSeries();
            volume.data.datasets[0].backgroundColor = color('--primary-blue'); volume.data.datasets[1].backgroundColor = color('--text-faint');
            volume.options.scales.y.title.text = volumeMetric.value === 'units' ? 'Số máy' : 'Giao dịch'; volume.update('none');
        }
        note.textContent = (grouping.value === '7' ? 'Cộng từng nhóm 7 ngày từ đầu mỗi kỳ; nhóm cuối có thể ngắn hơn. ' : 'Ngày không phát sinh ghi là 0. ')
            + 'Đối chiếu theo vị trí ngày trong hai kỳ; hôm nay có thể chưa đủ ngày. Bảng giữ số liệu từng ngày.';
    }
    if (revenue) {
        grouping.closest('.overview-chart-control').hidden = false; document.getElementById('overview-inspector').hidden = false;
        listen(grouping, refreshTimeCharts); listen(mode, refreshTimeCharts); listen(inspect, inspectPoint); refreshInspector();
    }
    if (volume) { volumeMetric.closest('.overview-chart-control').hidden = false; listen(volumeMetric, refreshTimeCharts); }
    refreshTimeCharts();
    const centerTotal = {
        id: 'overviewCenterTotal',
        afterDraw(chart) {
            const { ctx, chartArea: area } = chart; if (!area) return;
            const x = (area.left + area.right) / 2, y = (area.top + area.bottom) / 2;
            ctx.save(); ctx.textAlign = 'center'; ctx.fillStyle = color('--text-muted');
            ctx.font = '12px ' + getComputedStyle(document.body).fontFamily; ctx.fillText('Tổng doanh thu', x, y - 9);
            ctx.fillStyle = color('--text-title'); ctx.font = '600 18px ' + getComputedStyle(document.body).fontFamily;
            ctx.fillText(compact.format(data.pos + data.online) + ' ₫', x, y + 17); ctx.restore();
        }
    };
    create('overview-channel-chart', {
        type: 'doughnut', plugins: [centerTotal],
        data: { labels: ['Tại quầy', 'Online đã giao'], datasets: [{ data: [data.pos, data.online], borderWidth: 3, hoverOffset: 2 }] },
        options: { ...options(), cutout: '78%', plugins: { ...options().plugins, tooltip: { callbacks: {
            label: item => item.label + ': ' + money.format(item.parsed) + ' (' + percent.format(item.parsed / (data.pos + data.online)) + ')'
        } } } }
    }, chart => { chart.data.datasets[0].backgroundColor = [color('--primary-blue'), color('--status-success')]; chart.data.datasets[0].borderColor = color('--bg-surface'); });

    const valueLabels = {
        id: 'overviewValues',
        afterDatasetsDraw(chart) {
            const { ctx } = chart; ctx.save(); ctx.fillStyle = color('--text-body');
            ctx.font = '12px ' + getComputedStyle(document.body).fontFamily; ctx.textAlign = 'left'; ctx.textBaseline = 'middle';
            chart.data.datasets.forEach((dataset, datasetIndex) => {
                if (!chart.isDatasetVisible(datasetIndex)) return;
                chart.getDatasetMeta(datasetIndex).data.forEach((bar, index) => ctx.fillText(compact.format(dataset.data[index]), bar.x + 6, bar.y, 72));
            }); ctx.restore();
        }
    };
    function horizontalOptions(labels, count = false, legend = false) {
        return {
            ...options(legend), indexAxis: 'y', layout: { padding: { right: 82 } },
            scales: {
                x: { beginAtZero: true, title: { display: true, text: count ? 'Số đơn' : 'Đồng (₫)' }, ticks: { precision: count ? 0 : undefined, maxTicksLimit: 4, callback: value => count ? numbers.format(value) : compact.format(value) }, border: { display: false } },
                y: { grid: { display: false }, border: { display: false }, ticks: { autoSkip: false, callback: value => {
                    const label = labels[value] || ''; return label.length > 23 ? label.slice(0, 22) + '…' : label;
                } } }
            }
        };
    }
    const statusLabels = data.statuses.map(x => x.label);
    create('overview-status-chart', {
        type: 'bar', plugins: [valueLabels],
        data: { labels: statusLabels, datasets: [{ label: 'Đơn tạo trong kỳ', data: data.statuses.map(x => x.count), borderRadius: 4, maxBarThickness: 24 }] },
        options: { ...horizontalOptions(statusLabels, true), plugins: { ...options().plugins, tooltip: { callbacks: {
            label: item => numbers.format(item.parsed.x) + ' đơn',
            afterLabel: item => 'Giá trị sản phẩm: ' + money.format(data.statuses[item.dataIndex].amount) + '\nNhóm đơn theo ngày tạo, không theo ngày giao.'
        } } } }
    }, chart => {
        const tokens = { Pending: '--status-warning', Confirmed: '--primary-blue', Shipping: '--status-purple', Delivered: '--status-success', Cancelled: '--text-muted' };
        chart.data.datasets[0].backgroundColor = data.statuses.map(x => color(tokens[x.status] || '--text-muted'));
    });
    const brandLabels = data.brands.map(x => x.label);
    create('overview-brand-chart', {
        type: 'bar', plugins: [valueLabels],
        data: { labels: brandLabels, datasets: [
            { label: 'Tại quầy', data: data.brands.map(x => x.pos), borderRadius: 3, maxBarThickness: 14 },
            { label: 'Online đã giao', data: data.brands.map(x => x.online), borderRadius: 3, maxBarThickness: 14 }
        ] },
        options: { ...horizontalOptions(brandLabels, false, true), plugins: { ...options(true).plugins, tooltip: { callbacks: {
            label: item => item.dataset.label + ': ' + money.format(item.parsed.x),
            footer: items => 'Tổng hãng: ' + money.format(data.brands[items[0].dataIndex].amount)
        } } } }
    }, chart => { chart.data.datasets[0].backgroundColor = color('--primary-blue'); chart.data.datasets[1].backgroundColor = color('--status-success'); });

    let productRows = [];
    function rankProducts() {
        const key = productMetric.value;
        productRows = data.products.slice().sort((a, b) => b[key] - a[key] || a.id.localeCompare(b.id)).slice(0, 6);
        return productRows;
    }
    rankProducts();
    const product = create('overview-product-chart', {
        type: 'bar', plugins: [valueLabels],
        data: { labels: productRows.map(x => x.name), datasets: [{ label: 'Doanh thu', data: productRows.map(x => x.amount), borderRadius: 4, maxBarThickness: 24 }] },
        options: { ...horizontalOptions(productRows.map(x => x.name)), plugins: { ...options().plugins, tooltip: { callbacks: {
            title: items => productRows[items[0].dataIndex].name + ' · ' + productRows[items[0].dataIndex].id,
            label: item => 'Doanh thu: ' + money.format(productRows[item.dataIndex].amount),
            afterLabel: item => 'Đã bán: ' + numbers.format(productRows[item.dataIndex].quantity) + ' máy'
        } } } }
    }, chart => { chart.data.datasets[0].backgroundColor = color('--status-success'); });
    if (product) {
        productMetric.closest('.overview-chart-control').hidden = false;
        listen(productMetric, () => {
            rankProducts(); const count = productMetric.value === 'quantity';
            product.data.labels = productRows.map(x => x.name); product.data.datasets[0].data = productRows.map(x => count ? x.quantity : x.amount);
            product.data.datasets[0].label = count ? 'Số máy đã bán' : 'Doanh thu';
            product.canvas.setAttribute('aria-label', count ? 'Sáu phiên bản bán nhiều máy nhất; bảng đầy đủ bên dưới.' : 'Sáu phiên bản có doanh thu cao nhất; bảng đầy đủ bên dưới.');
            product.options.scales.x.title.text = count ? 'Số máy' : 'Đồng (₫)';
            product.options.scales.x.ticks.precision = count ? 0 : undefined;
            product.options.scales.y.ticks.callback = value => { const name = productRows[value]?.name || ''; return name.length > 23 ? name.slice(0, 22) + '…' : name; };
            product.update('none');
        });
    }
    const observer = new MutationObserver(() => charts.forEach(entry => entry.update()));
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
    window.addEventListener('pagehide', event => {
        if (event.persisted) return;
        events.abort(); daysSelect.removeEventListener('change', dateMode); observer.disconnect(); charts.forEach(entry => entry.chart.destroy());
    });
    window.addEventListener('pageshow', event => { if (event.persisted) charts.forEach(entry => { entry.chart.resize(); entry.update(); }); });
})();
