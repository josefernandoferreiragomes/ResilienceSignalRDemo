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
            if (window.charts[id]) {
                try { window.charts[id].destroy(); } catch(e) { /* ignore */ }
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
                    labels: labels,
                    datasets: [{
                        label: 'State',
                        data: data,
                        borderColor: 'royalblue',
                        backgroundColor: 'rgba(65,105,225,0.2)',
                        fill: false,
                        pointRadius: 3,
                        tension: 0.0,
                        stepped: 'before'
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            ticks: {
                                callback: function (val) { return yCallback ? yCallback(val) : val; }
                            },
                            min: 0,
                            max: 1
                        },
                        x: {
                            type: 'category'
                        }
                    },
                    plugins: {
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    return context.parsed.y === 1 ? 'Open' : (context.parsed.y === 0.5 ? 'HalfOpen' : 'Closed');
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

    function renderStatus(id, labels, data, datasetLabel) {
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
            if (window.charts[id]) {
                try { window.charts[id].destroy(); } catch(e) { }
            }     
            el.style.display = 'block';
            const ctx = el.getContext && el.getContext('2d');
            if (!ctx) {
                console.warn('Unable to get 2d context for', id);
                return;
            }

            // data: numeric array of status codes; generate per-point colors
            const pointBg = data.map(s => {
                if (s >= 500) return 'red';
                if (s >= 400) return 'orange';
                if (s >= 200) return 'seagreen';
                if (s === 0) return 'gray';
                return 'steelblue';
            });

            // Debug: print incoming labels/data and color mapping
            try {
                console.log('[timelineCharts] renderStatus labels:', labels);
                console.log('[timelineCharts] renderStatus data:', data);
                console.log('[timelineCharts] renderStatus pointBg sample:', pointBg.slice(0, 10));
                console.log('[timelineCharts] status>=500 count:', data.filter(d => d >= 500).length);
            } catch (e) { /* ignore */ }

            // Convert labels+data into scatter points using numeric x (index) and y=value.
            // Use labels array only for tick/tooltip display to avoid parsing string timestamps.
            const points = labels.map((lbl, i) => ({ x: i, y: data[i] }));

            window.charts[id] = new Chart(ctx, {
                type: 'scatter',
                data: {
                    labels: labels,
                    datasets: [{
                        label: datasetLabel || 'Status',
                        data: points,
                        pointBackgroundColor: pointBg,
                        backgroundColor: pointBg,
                        borderColor: 'transparent',
                        showLine: false,
                        pointRadius: 6
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    //parsing: false,
                    scales: {
                        x: {
                            type: 'linear',
                            ticks: {
                                callback: function (val, index) {
                                    // val is numeric index; return corresponding label if available
                                    var idx = Math.round(val);
                                    return labels[idx] ?? '';
                                }
                            }
                        },
                        y: {
                            title: { display: true, text: 'HTTP Status' }
                        }
                    },
                    plugins: {
                        tooltip: {
                            callbacks: {
                                label: function (ctx) { return 'Status: ' + (ctx.parsed && ctx.parsed.y !== undefined ? ctx.parsed.y : (ctx.raw && ctx.raw.y !== undefined ? ctx.raw.y : ctx.raw)); }
                            }
                        }
                    }
                }
            });
        } catch (e) { console.error(e); }
    }

    function renderAttempts(id, labels, initialData, retryData) {
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
            if (window.charts[id]) { try { window.charts[id].destroy(); } catch (e) { } }
            el.style.display = 'block';
            const ctx = el.getContext && el.getContext('2d');
            if (!ctx) { console.warn('Unable to get 2d context for', id); return; }
            window.charts[id] = new Chart(ctx, {
                type: 'scatter',
                data: {
                    labels: labels,
                    datasets: [
                        {
                            label: 'Initial Attempts',
                            data: initialData,   // plain array
                            pointBackgroundColor: 'seagreen',
                            pointRadius: 6
                        },
                        {
                            label: 'Retries',
                            data: retryData,   // plain array
                            pointBackgroundColor: 'orange',
                            pointRadius: 6
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    //parsing: false,
                    scales: { x: { type: 'category' }, y: { display: false } },
                    plugins: { tooltip: { callbacks: { label: function(ctx){ return ctx.dataset.label + ': ' + ctx.raw.x; } } } }
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
