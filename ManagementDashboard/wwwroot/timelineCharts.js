window.timelineCharts = (function () {
    window.charts = window.charts || {};

    function renderLine(id, labels, data, yCallback) {
        try {
            if (typeof Chart === 'undefined') {
                console.warn('Chart.js not available; skipping renderLine for', id);
                return;
            }
            var el = document.getElementById(id);
            if (!el) {
                console.warn('Canvas element not found for id', id);
                return;
            }
            var existing = Chart.getChart(el);
            if (existing) {
                existing.destroy();
            }
            el.style.display = 'block';
            const ctx = el.getContext && el.getContext('2d');
            if (!ctx) {
                console.warn('Unable to get 2d context for', id);
                return;
            }
            window.charts[id] = new Chart(ctx, {
                type: 'line',
                data: {
                    // labels optional when using time scale; data is array of {x: ISODate, y: number}
                    datasets: [{
                        label: 'State',
                        data: data,
                        borderColor: 'royalblue',
                        backgroundColor: 'rgba(65,105,225,0.2)',
                        fill: false,
                        pointRadius: 3,
                        tension: 0.0,
                        stepped: 'before',
                        parsing: false
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        x: {
                            type: 'time',
                            time: { tooltipFormat: 'HH:mm:ss.SSS', displayFormats: { second: 'HH:mm:ss' } }
                        },
                        y: {
                            ticks: {
                                callback: function (val) { return yCallback ? yCallback(val) : val; }
                            },
                            min: 0,
                            max: 1
                        }
                    },
                    plugins: {
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    var y = context.parsed && context.parsed.y !== undefined ? context.parsed.y : (context.raw && context.raw.y !== undefined ? context.raw.y : context.raw);
                                    return y === 1 ? 'Open' : (y === 0.5 ? 'HalfOpen' : 'Closed');
                                }
                            }
                        }
                    }
                }
            });
        } catch (e) {
            console.error(e);
        }
    }

    function renderStatus(id, points, datasetLabel) {
        try {
            if (typeof Chart === 'undefined') {
                console.warn('Chart.js not available; skipping renderStatus for', id);
                return;
            }
            var el = document.getElementById(id);
            if (!el) {
                console.warn('Canvas element not found for id', id);
                return;
            }
            var existing = Chart.getChart(el);
            if (existing) {
                existing.destroy();
            }
            el.style.display = 'block';
            const ctx = el.getContext && el.getContext('2d');
            if (!ctx) {
                console.warn('Unable to get 2d context for', id);
                return;
            }
            // points: array of { x: ISO8601 timestamp, y: numeric status }
            const values = points.map(p => p && p.y !== undefined ? p.y : 0);
            const pointBg = values.map(s => {
                if (s >= 500) return 'red';
                if (s >= 400) return 'orange';
                if (s >= 200) return 'seagreen';
                if (s === 0) return 'gray';
                return 'steelblue';
            });

            try {
                console.log('[timelineCharts] renderStatus points sample:', points.slice(0, 8));
            } catch (e) { }

            window.charts[id] = new Chart(ctx, {
                type: 'line',
                data: {
                    datasets: [{
                        label: datasetLabel || 'Status',
                        data: points,
                        pointBackgroundColor: pointBg,
                        backgroundColor: 'rgba(70,130,180,0.08)',
                        borderColor: 'steelblue',
                        borderWidth: 1,
                        pointRadius: 4,
                        showLine: true,
                        fill: false,
                        tension: 0.0,
                        parsing: false
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        x: {
                            type: 'time',
                            time: { tooltipFormat: 'HH:mm:ss.SSS', displayFormats: { second: 'HH:mm:ss' } }
                        },
                        y: {
                            title: { display: true, text: 'HTTP Status' }
                        }
                    },
                    plugins: {
                        tooltip: {
                            callbacks: {
                                label: function (ctx) { var v = ctx.parsed && ctx.parsed.y !== undefined ? ctx.parsed.y : (ctx.raw && ctx.raw.y !== undefined ? ctx.raw.y : ctx.raw); return 'Status: ' + v; }
                            }
                        }
                    }
                }
            });
        } catch (e) { console.error(e); }
    }

    function renderAttempts(id, initialPoints, retryPoints) {
        try {
            if (typeof Chart === 'undefined') {
                console.warn('Chart.js not available; skipping renderAttempts for', id);
                return;
            }
            var el = document.getElementById(id);
            if (!el) {
                console.warn('Canvas element not found for id', id);
                return;
            }
            var existing = Chart.getChart(el);
            if (existing) {
                existing.destroy();
            }
            el.style.display = 'block';
            const ctx = el.getContext && el.getContext('2d');
            if (!ctx) { console.warn('Unable to get 2d context for', id); return; }
            window.charts[id] = new Chart(ctx, {
                type: 'line',
                data: {
                    datasets: [
                        {
                            label: 'Initial Attempts',
                            data: initialPoints,
                            borderColor: 'seagreen',
                            backgroundColor: 'rgba(46,139,87,0.08)',
                            pointRadius: 3,
                            fill: false,
                            parsing: false
                        },
                        {
                            label: 'Retries',
                            data: retryPoints,
                            borderColor: 'orange',
                            backgroundColor: 'rgba(255,165,0,0.08)',
                            pointRadius: 3,
                            fill: false,
                            parsing: false
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: { x: { type: 'time', time: { tooltipFormat: 'HH:mm:ss.SSS', displayFormats: { second: 'HH:mm:ss' } } }, y: { title: { display: true, text: 'Attempts' } } },
                    plugins: { tooltip: { callbacks: { label: function(ctx){ var v = ctx.parsed && ctx.parsed.y !== undefined ? ctx.parsed.y : (ctx.raw && ctx.raw.y !== undefined ? ctx.raw.y : ctx.raw); return ctx.dataset.label + ': ' + v; } } } }
                }
            });
        } catch(e){ console.error(e); }
    }

    return {
        renderCircuit: renderLine,
        renderStatus: renderStatus,
        renderAttempts: renderAttempts
    };
})();
