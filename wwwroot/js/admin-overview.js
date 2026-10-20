(() => {
    const source = document.getElementById('overview-data');
    if (!source) return;
    const data = JSON.parse(source.textContent);
    const money = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 });
    const compact = new Intl.NumberFormat('vi-VN', { notation: 'compact', maximumFractionDigits: 1 });
    const percent = new Intl.NumberFormat('vi-VN', { style: 'percent', maximumFractionDigits: 1 });
    const color = name => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    const charts = [];
    const grouping = document.getElementById('overview-grouping');
    const note = document.getElementById('overview-grouping-note');

    function fallback(section) {
        section.querySelectorAll('.overview-chart-data').forEach(details => { details.open = true; });
        const message = document.createElement('p');
        message.className = 'overview-chart-fallback';
        message.textContent = 'Biểu đồ chưa tải được. Bạn có thể xem đầy đủ số liệu trong bảng bên dưới.';
        section.prepend(message);
    }
    if (typeof window.Chart !== 'function') {
        fallback(source.parentElement);
        return;
    }
    function options() {
        return {
            responsive: true, maintainAspectRatio: false, animation: false,
            font: { family: getComputedStyle(document.body).fontFamily, size: 12 },
            plugins: {
                legend: { display: false },
                tooltip: { callbacks: { label: item => item.dataset.label + ': ' + money.format(item.parsed.y) } }
            }
        };
    }
    function create(id, config, paint) {
        const canvas = document.getElementById(id);
        if (!canvas) return null;
        const frame = canvas.closest('[data-chart-frame]');
        frame.hidden = false;
        try {
            const chart = new Chart(canvas, config);
            const update = () => {
                paint(chart);
                for (const axis of Object.values(chart.options.scales || {})) {
                    if (axis.ticks) axis.ticks.color = color('--text-muted');
                    if (axis.title) axis.title.color = color('--text-muted');
                    if (axis.grid) axis.grid.color = color('--border-light');
                    if (axis.border) axis.border.color = color('--border-default');
                }
                chart.update('none');
            };
            update();
            charts.push({ chart, update });
            return chart;
        } catch (error) {
            const partial = Chart.getChart(canvas);
            if (partial) partial.destroy();
            frame.hidden = true;
            fallback(canvas.closest('.overview-chart'));
            return null;
        }
    }
    function groupedDays() {
        const step = Number(grouping.value);
        const result = [];
        for (let i = 0; i < data.days.length; i += step) {
            const chunk = data.days.slice(i, i + step);
            const first = chunk[0].label;
            const last = chunk[chunk.length - 1].label;
            result.push({
                label: chunk.length === 1 ? first.slice(0, 5) : first.slice(0, 5) + ' – ' + last.slice(0, 5),
                fullLabel: chunk.length === 1 ? first : first + ' – ' + last,
                pos: chunk.reduce((sum, day) => sum + day.pos, 0),
                online: chunk.reduce((sum, day) => sum + day.online, 0),
                transactions: chunk.reduce((sum, day) => sum + day.transactions, 0),
                units: chunk.reduce((sum, day) => sum + day.units, 0)
            });
        }
        return result;
    }
    let points = groupedDays();
    const revenue = create('overview-revenue-chart', {
        type: 'line',
        data: {
            labels: points.map(x => x.label),
            datasets: [
                { label: 'Tại quầy', data: points.map(x => x.pos), borderWidth: 2, tension: 0, pointRadius: 2, pointHitRadius: 16 },
                { label: 'Online đã giao', data: points.map(x => x.online), borderWidth: 2, borderDash: [6, 4], tension: 0, pointRadius: 2, pointStyle: 'rect', pointHitRadius: 16 }
            ]
        },
        options: {
            ...options(), interaction: { mode: 'index', intersect: false },
            plugins: { ...options().plugins, tooltip: { callbacks: {
                title: items => points[items[0].dataIndex].fullLabel,
                label: item => item.dataset.label + ': ' + money.format(item.parsed.y),
                footer: items => {
                    const point = points[items[0].dataIndex];
                    return 'Tổng: ' + money.format(point.pos + point.online) + '\n' + point.transactions + ' giao dịch · ' + point.units + ' máy';
                }
            } } },
            scales: {
                x: { grid: { display: false }, ticks: { maxTicksLimit: 7, maxRotation: 0 }, border: { display: false } },
                y: { beginAtZero: true, title: { display: true, text: 'Đồng (₫)' }, ticks: { maxTicksLimit: 5, callback: value => compact.format(value) }, border: { display: false } }
            }
        }
    }, chart => {
        chart.data.datasets.forEach((dataset, index) => {
            dataset.borderColor = color(index === 0 ? '--primary-blue' : '--status-success');
            dataset.backgroundColor = dataset.borderColor;
        });
    });
    function updateGrouping() {
        if (!revenue) return;
        points = groupedDays();
        revenue.data.labels = points.map(x => x.label);
        revenue.data.datasets[0].data = points.map(x => x.pos);
        revenue.data.datasets[1].data = points.map(x => x.online);
        revenue.update('none');
        note.textContent = grouping.value === '7'
            ? 'Cộng từng nhóm 7 ngày từ đầu kỳ; nhóm cuối có thể ít hơn 7 ngày. Bảng bên trên luôn giữ số liệu từng ngày. Đơn vị: đồng (₫).'
            : 'Ngày không phát sinh doanh thu được ghi là 0. Đơn vị trục: đồng (₫).';
    }
    if (revenue) {
        grouping.closest('.overview-chart-control').hidden = false;
        grouping.addEventListener('change', updateGrouping);
        updateGrouping();
    }
    create('overview-channel-chart', {
        type: 'doughnut',
        data: { labels: ['Tại quầy', 'Online đã giao'], datasets: [{ data: [data.pos, data.online], borderWidth: 3, hoverOffset: 3 }] },
        options: { ...options(), cutout: '72%', plugins: { ...options().plugins, tooltip: { callbacks: {
            label: item => item.label + ': ' + money.format(item.parsed) + ' (' + percent.format(item.parsed / (data.pos + data.online)) + ')'
        } } } }
    }, chart => {
        chart.data.datasets[0].backgroundColor = [color('--primary-blue'), color('--status-success')];
        chart.data.datasets[0].borderColor = color('--bg-surface');
    });

    const valueLabels = {
        id: 'overviewValues',
        afterDatasetsDraw(chart) {
            const { ctx } = chart;
            ctx.save();
            ctx.fillStyle = color('--text-body');
            ctx.font = '12px ' + getComputedStyle(document.body).fontFamily;
            ctx.textAlign = 'left';
            ctx.textBaseline = 'middle';
            chart.getDatasetMeta(0).data.forEach((bar, index) => {
                ctx.fillText(compact.format(chart.data.datasets[0].data[index]), bar.x + 8, bar.y);
            });
            ctx.restore();
        }
    };
    function bars(id, entries, token, productChart) {
        create(id, {
            type: 'bar',
            data: {
                labels: entries.map(x => x.label),
                datasets: [{ label: 'Doanh thu', data: entries.map(x => x.amount), borderRadius: 4, maxBarThickness: 24 }]
            },
            plugins: [valueLabels],
            options: {
                ...options(), indexAxis: 'y', layout: { padding: { right: 76 } },
                plugins: { ...options().plugins, tooltip: { callbacks: {
                    title: items => {
                        const row = entries[items[0].dataIndex];
                        return row.label + (productChart ? ' · ' + row.id : '');
                    },
                    label: item => money.format(item.parsed.x),
                    afterLabel: item => productChart ? entries[item.dataIndex].quantity + ' máy' : ''
                } } },
                scales: {
                    x: { beginAtZero: true, title: { display: true, text: 'Đồng (₫)' }, ticks: { maxTicksLimit: 4, callback: value => compact.format(value) }, border: { display: false } },
                    y: { grid: { display: false }, border: { display: false }, ticks: {
                        autoSkip: false, callback: value => {
                            const text = entries[value].label;
                            return text.length > 22 ? text.slice(0, 21) + '…' : text;
                        }
                    } }
                }
            }
        }, chart => { chart.data.datasets[0].backgroundColor = color(token); });
    }
    bars('overview-brand-chart', data.brands, '--primary-blue', false);
    bars('overview-product-chart', data.products.map(x => ({ ...x, label: x.name })), '--status-success', true);

    const observer = new MutationObserver(() => charts.forEach(entry => entry.update()));
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
    window.addEventListener('pagehide', event => {
        if (event.persisted) return;
        observer.disconnect();
        grouping.removeEventListener('change', updateGrouping);
        charts.forEach(entry => entry.chart.destroy());
    });
})();
