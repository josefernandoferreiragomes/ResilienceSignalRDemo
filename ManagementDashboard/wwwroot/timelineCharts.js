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
            // stabilize canvas pixel size to avoid resize feedback loops
            try {
                el.style.display = 'block';
                el.style.width = el.clientWidth + 'px';        // lock CSS width
                const cssHeight = el.getAttribute('height') ? parseInt(el.getAttribute('height'), 10) : el.clientHeight;
                el.style.height = cssHeight + 'px';
                const ratio = window.devicePixelRatio || 1;
                el.height = Math.floor(cssHeight * ratio);
                el.width = Math.floor(el.clientWidth * ratio);
            } catch (err) { /* ignore sizing errors */ }
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
                    responsive: false,
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
            // stabilize canvas pixel size to avoid resize feedback loops
            try {
                el.style.display = 'block';
                el.style.width = el.clientWidth + 'px';        // lock CSS width
                const cssHeight = el.getAttribute('height') ? parseInt(el.getAttribute('height'), 10) : el.clientHeight;
                el.style.height = cssHeight + 'px';
                const ratio = window.devicePixelRatio || 1;
                el.height = Math.floor(cssHeight * ratio);
                el.width = Math.floor(el.clientWidth * ratio);
            } catch (err) { }
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

            window.charts[id] = new Chart(ctx, {
                type: 'scatter',
                data: {
                    labels: labels,
                    datasets: [{
                        label: datasetLabel || 'Status',
                        data: labels.map((l, i) => ({ x: l, y: data[i] })),
                        pointBackgroundColor: pointBg,
                        borderColor: 'transparent',
                        backgroundColor: pointBg,
                        showLine: false,
                        pointRadius: 6
                    }]
                },
                options: {
                    responsive: false,
                    maintainAspectRatio: false,
                    parsing: false,
                    scales: {
                        x: {
                            type: 'category'
                        },
                        y: {
                            title: { display: true, text: 'HTTP Status' }
                        }
                    },
                    plugins: {
                        tooltip: {
                            callbacks: {
                                label: function (ctx) { return 'Status: ' + ctx.raw.y; }
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
            if (window.charts[id]) { try { window.charts[id].destroy(); } catch(e) {} }
            // stabilize canvas pixel size to avoid resize feedback loops
            try {
                el.style.display = 'block';
                el.style.width = el.clientWidth + 'px';        // lock CSS width
                const cssHeight = el.getAttribute('height') ? parseInt(el.getAttribute('height'), 10) : el.clientHeight;
                el.style.height = cssHeight + 'px';
                const ratio = window.devicePixelRatio || 1;
                el.height = Math.floor(cssHeight * ratio);
                el.width = Math.floor(el.clientWidth * ratio);
            } catch (err) { }
            const ctx = el.getContext && el.getContext('2d');
            if (!ctx) { console.warn('Unable to get 2d context for', id); return; }
            window.charts[id] = new Chart(ctx, {
                type: 'scatter',
                data: {
                    labels: labels,
                    datasets: [
                        {
                            label: 'Initial Attempts',
                            data: labels.map((l,i) => ({ x: l, y: initialData[i] })),
                            pointBackgroundColor: 'seagreen',
                            pointRadius: 6
                        },
                        {
                            label: 'Retries',
                            data: labels.map((l,i) => ({ x: l, y: retryData[i] })),
                            pointBackgroundColor: 'orange',
                            pointRadius: 6
                        }
                    ]
                },
                options: {
                    responsive: false,
                    maintainAspectRatio: false,
                    parsing: false,
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
