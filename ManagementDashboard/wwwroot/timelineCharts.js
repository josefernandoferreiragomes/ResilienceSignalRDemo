window.timelineCharts = (function () {
    window.charts = window.charts || {};

    function renderLine(id, data, yCallback) {
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
            var existing = window.charts[id] || Chart.getChart(el);
            if (existing) {
                existing.destroy();
                delete window.charts[id];
            }
            el.style.display = 'block';
            window.charts[id] = new Chart(el, {
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

    function renderStatus(id, datasets) {
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
            var existing = window.charts[id] || Chart.getChart(el);
            if (existing) {
                existing.destroy();
                delete window.charts[id];
            }
            el.style.display = 'block';

            // Color mapping for HTTP status codes
            var statusColorMap = {
                200: 'seagreen',
                404: 'darkorange',
                500: 'crimson',
                503: 'orangered'
            };

            // Ensure datasets is an array of dataset objects with label, data, and optional color
            // Each dataset: { label: "200 OK", data: [...], color: "seagreen" }
            var chartDatasets = [];
            if (Array.isArray(datasets)) {
                chartDatasets = datasets.map(function (ds) {
                    var color = ds.color || statusColorMap[ds.statusCode] || 'steelblue';
                    return {
                        label: ds.label,
                        data: ds.data,
                        borderColor: color,
                        backgroundColor: color.replace(/[^,]+(?=\))/, '0.08'),
                        pointRadius: 4,
                        showLine: true,
                        fill: false,
                        tension: 0.0,
                        parsing: false,
                        borderWidth: 2
                    };
                });
            }

            try {
                console.log('[timelineCharts] renderStatus datasets count:', chartDatasets.length);
                chartDatasets.forEach(function (ds, idx) {
                    console.log('[timelineCharts] Dataset ' + idx + ':', ds.label, '- points:', ds.data.length);
                });
            } catch (e) { }

            window.charts[id] = new Chart(el, {
                type: 'line',
                data: {
                    datasets: chartDatasets
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
                            title: { display: true, text: 'Request Count' }
                        }
                    },
                    plugins: {
                        legend: {
                            display: true,
                            position: 'top'
                        },
                        tooltip: {
                            callbacks: {
                                label: function (ctx) {
                                    var v = ctx.parsed && ctx.parsed.y !== undefined ? ctx.parsed.y : (ctx.raw && ctx.raw.y !== undefined ? ctx.raw.y : ctx.raw);
                                    return ctx.dataset.label + ': ' + v;
                                }
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
            var existing = window.charts[id] || Chart.getChart(el);
            if (existing) {
                existing.destroy();
                delete window.charts[id];
            }
            el.style.display = 'block';
            window.charts[id] = new Chart(el, {
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
