/**
 * StoryCanvas.js
 * JavaScript module for the Data Storytelling Canvas component
 */

// Create a namespace to avoid polluting the global scope
window.storyCanvas = (function () {
    // Private properties
    let dotNetHelper = null;
    let chartInstances = {};
    let darkModeEnabled = false;
    let isDragging = false;

    // Color schemes for charts
    const colorSchemes = {
        default: ['#4361ee', '#4895ef', '#4cc9f0', '#f72585', '#f8961e', '#06d6a0', '#8338ec', '#3a0ca3'],
        monochrome: ['#4361ee', '#5171f0', '#6081f2', '#7090f4', '#80a0f6', '#90aff8', '#a0bffa', '#b0cefc'],
        warm: ['#f72585', '#f94096', '#fb5ca6', '#fc77b7', '#fd93c7', '#fea7d0', '#febdd9', '#ffd4e3'],
        cool: ['#4cc9f0', '#60cff2', '#74d5f4', '#88dbf6', '#9ce1f8', '#b0e7fa', '#c4edfc', '#d8f3fe'],
        earth: ['#b6c197', '#a3b78e', '#90ad86', '#7da37d', '#6a9975', '#578f6c', '#448564', '#307b5b'],
    };

    // Chart.js defaults
    function setChartDefaults() {
        Chart.defaults.font.family = "'Inter', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
        Chart.defaults.font.size = 12;
        Chart.defaults.color = darkModeEnabled ? '#e9ecef' : '#495057';
        Chart.defaults.plugins.tooltip.backgroundColor = darkModeEnabled ? 'rgba(255, 255, 255, 0.9)' : 'rgba(0, 0, 0, 0.8)';
        Chart.defaults.plugins.tooltip.titleColor = darkModeEnabled ? '#212529' : '#fff';
        Chart.defaults.plugins.tooltip.bodyColor = darkModeEnabled ? '#495057' : '#fff';
        Chart.defaults.plugins.tooltip.borderColor = darkModeEnabled ? 'rgba(0, 0, 0, 0.1)' : 'rgba(255, 255, 255, 0.1)';
        Chart.defaults.plugins.tooltip.borderWidth = 1;
        Chart.defaults.plugins.tooltip.displayColors = true;
        Chart.defaults.plugins.tooltip.padding = 10;
        Chart.defaults.plugins.tooltip.cornerRadius = 6;
    }

    /**
     * Initializes the StoryCanvas
     * @param {object} helper - The .NET helper object for callbacks
     */
    function initialize(helper) {
        dotNetHelper = helper;

        // Load Chart.js library dynamically
        function waitForChart() {
            if (typeof Chart !== 'undefined') {
                setChartDefaults();
                setupDragAndDrop();
                document.addEventListener('keydown', handleKeyDown);
                window.addEventListener('resize', handleResize);
                console.log('StoryCanvas initialized');
                return true;
            } else {
                console.log('Chart not available yet, retrying...');
                setTimeout(waitForChart, 100);
            }
        }

        waitForChart();
       
            setChartDefaults();
        

        // Set up drag and drop event listeners
        setupDragAndDrop();

        // Set up keyboard event listeners
        document.addEventListener('keydown', handleKeyDown);

        // Set up resize listener for responsive charts
        window.addEventListener('resize', handleResize);

        console.log('StoryCanvas initialized');

        return true;
    }

    /**
     * Sets up drag and drop event listeners
     */
    function setupDragAndDrop() {
        document.addEventListener('dragstart', () => {
            isDragging = true;
        });

        document.addEventListener('dragend', () => {
            isDragging = false;
        });
    }

    /**
     * Handles window resize events
     */
    function handleResize() {
        // Debounce resize events
        if (this.resizeTimeout) clearTimeout(this.resizeTimeout);

        this.resizeTimeout = setTimeout(() => {
            // Resize all active charts
            for (const id in chartInstances) {
                if (chartInstances.hasOwnProperty(id)) {
                    chartInstances[id].resize();
                }
            }
        }, 250);
    }

    /**
     * Handles keyboard events
     * @param {KeyboardEvent} event - The keyboard event
     */
    function handleKeyDown(event) {
        // Forward keyboard events to .NET component
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('HandleKeyDown', event.key);
        }
    }

    /**
     * Renders a visualization based on type and data
     * @param {string} id - Element ID to render the visualization in
     * @param {string} type - Type of visualization (bar, line, pie, etc.)
     * @param {string} dataJson - JSON string of data
     * @param {string} configJson - JSON string of visualization config
     */
    function renderVisualization(id, type, dataJson, configJson) {
        // Parse JSON data
        const data = JSON.parse(dataJson);
        const config = JSON.parse(configJson);

        // Get the container element
        const container = document.getElementById(`viz-content-${id}`);
        if (!container) {
            console.error(`Container not found: viz-content-${id}`);
            return;
        }

        // Destroy existing chart if it exists
        if (chartInstances[id]) {
            chartInstances[id].destroy();
            delete chartInstances[id];
        }

        // Clear container
        container.innerHTML = '';

        // Create canvas element
        const canvas = document.createElement('canvas');
        canvas.id = `chart-${id}`;
        container.appendChild(canvas);

        // Select chart colors
        const colors = colorSchemes[config.colorScheme || 'default'];

        // Render based on visualization type
        switch (type) {
            case 'bar':
                renderBarChart(id, data, config, colors);
                break;
            case 'line':
                renderLineChart(id, data, config, colors);
                break;
            case 'pie':
                renderPieChart(id, data, config, colors);
                break;
            case 'scatter':
                renderScatterChart(id, data, config, colors);
                break;
            case 'heatmap':
                renderHeatMap(id, data, config, colors);
                break;
            case 'gauge':
                renderGaugeChart(id, data, config, colors);
                break;
            case 'text':
                renderTextBlock(id, data, config);
                break;
            case 'map':
                renderMapVisualization(id, data, config, colors);
                break;
            default:
                container.innerHTML = `<div class="viz-error">Unsupported visualization type: ${type}</div>`;
        }
    }

    /**
     * Renders a bar chart
     */
    function renderBarChart(id, data, config, colors) {
        const canvas = document.getElementById(`chart-${id}`);
        const ctx = canvas.getContext('2d');

        // Extract data using config properties
        const xAxis = config.xAxis || Object.keys(data[0])[0];
        const yAxis = config.yAxis || Object.keys(data[0])[1];

        // Group data if needed
        let chartData = data;
        if (data.length > 20) {
            // Group data for better visualization
            const grouped = groupDataByCategory(data, xAxis, yAxis);
            chartData = Object.keys(grouped).map(key => ({
                [xAxis]: key,
                [yAxis]: grouped[key]
            }));
        }

        // Sort data by yAxis value
        chartData.sort((a, b) => parseFloat(b[yAxis]) - parseFloat(a[yAxis]));

        // Limit data points for readability
        if (chartData.length > 15) {
            chartData = chartData.slice(0, 15);
        }

        // Create chart configuration
        const chartConfig = {
            type: 'bar',
            data: {
                labels: chartData.map(item => item[xAxis]),
                datasets: [{
                    label: yAxis,
                    data: chartData.map(item => item[yAxis]),
                    backgroundColor: colors[0],
                    borderColor: darkenColor(colors[0], 0.2),
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: config.showLegend !== false,
                        position: 'top',
                    },
                    tooltip: {
                        enabled: config.showTooltips !== false
                    },
                    title: {
                        display: false,
                        text: config.title || ''
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            display: config.showGrid !== false
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                },
                animation: {
                    duration: 1000,
                    easing: 'easeOutQuart'
                }
            }
        };

        // Create and store the chart
        chartInstances[id] = new Chart(ctx, chartConfig);
    }

    /**
     * Renders a line chart
     */
    function renderLineChart(id, data, config, colors) {
        const canvas = document.getElementById(`chart-${id}`);
        const ctx = canvas.getContext('2d');

        // Extract data using config properties
        const xAxis = config.xAxis || Object.keys(data[0])[0];
        const yAxis = config.yAxis || Object.keys(data[0])[1];

        // Sort data by xAxis
        const chartData = [...data].sort((a, b) => {
            // Try to parse as date
            const dateA = new Date(a[xAxis]);
            const dateB = new Date(b[xAxis]);

            if (!isNaN(dateA) && !isNaN(dateB)) {
                return dateA - dateB;
            }

            // Fallback to string comparison
            return String(a[xAxis]).localeCompare(String(b[xAxis]));
        });

        // Create chart configuration
        const chartConfig = {
            type: 'line',
            data: {
                labels: chartData.map(item => item[xAxis]),
                datasets: [{
                    label: yAxis,
                    data: chartData.map(item => item[yAxis]),
                    backgroundColor: addAlpha(colors[0], 0.2),
                    borderColor: colors[0],
                    borderWidth: 2,
                    tension: 0.4,
                    fill: true,
                    pointBackgroundColor: colors[0],
                    pointBorderColor: '#fff',
                    pointRadius: 4,
                    pointHoverRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: config.showLegend !== false,
                        position: 'top',
                    },
                    tooltip: {
                        enabled: config.showTooltips !== false
                    },
                    title: {
                        display: false,
                        text: config.title || ''
                    }
                },
                scales: {
                    y: {
                        beginAtZero: false,
                        grid: {
                            display: config.showGrid !== false
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                },
                animation: {
                    duration: 1500,
                    easing: 'easeOutQuart'
                }
            }
        };

        // Create and store the chart
        chartInstances[id] = new Chart(ctx, chartConfig);
    }

    /**
     * Renders a pie chart
     */
    function renderPieChart(id, data, config, colors) {
        const canvas = document.getElementById(`chart-${id}`);
        const ctx = canvas.getContext('2d');

        // Extract data using config properties
        const category = config.category || Object.keys(data[0])[0];
        const value = config.value || Object.keys(data[0])[1];

        // Group data by category
        const grouped = groupDataByCategory(data, category, value);

        // Convert grouped data for chart
        const labels = Object.keys(grouped);
        const values = Object.values(grouped);

        // Create chart configuration
        const chartConfig = {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    data: values,
                    backgroundColor: colors.slice(0, labels.length),
                    borderColor: 'white',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: config.showLegend !== false,
                        position: 'right',
                    },
                    tooltip: {
                        enabled: config.showTooltips !== false
                    },
                    title: {
                        display: false,
                        text: config.title || ''
                    }
                },
                animation: {
                    animateRotate: true,
                    animateScale: true,
                    duration: 1500,
                    easing: 'easeOutQuart'
                }
            }
        };

        // Create and store the chart
        chartInstances[id] = new Chart(ctx, chartConfig);
    }

    /**
     * Renders a scatter chart
     */
    function renderScatterChart(id, data, config, colors) {
        const canvas = document.getElementById(`chart-${id}`);
        const ctx = canvas.getContext('2d');

        // Extract data using config properties
        const xAxis = config.xAxis || Object.keys(data[0])[0];
        const yAxis = config.yAxis || Object.keys(data[0])[1];

        // Create chart configuration
        const chartConfig = {
            type: 'scatter',
            data: {
                datasets: [{
                    label: `${xAxis} vs ${yAxis}`,
                    data: data.map(item => ({
                        x: item[xAxis],
                        y: item[yAxis]
                    })),
                    backgroundColor: addAlpha(colors[0], 0.7),
                    borderColor: colors[0],
                    borderWidth: 1,
                    pointRadius: 6,
                    pointHoverRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: config.showLegend !== false,
                        position: 'top',
                    },
                    tooltip: {
                        enabled: config.showTooltips !== false
                    },
                    title: {
                        display: false,
                        text: config.title || ''
                    }
                },
                scales: {
                    x: {
                        title: {
                            display: true,
                            text: xAxis
                        },
                        grid: {
                            display: config.showGrid !== false
                        }
                    },
                    y: {
                        title: {
                            display: true,
                            text: yAxis
                        },
                        grid: {
                            display: config.showGrid !== false
                        }
                    }
                },
                animation: {
                    duration: 1500,
                    easing: 'easeOutQuart'
                }
            }
        };

        // Create and store the chart
        chartInstances[id] = new Chart(ctx, chartConfig);
    }

    /**
     * Renders a heat map
     */
    function renderHeatMap(id, data, config, colors) {
        const container = document.getElementById(`viz-content-${id}`);
        container.innerHTML = '';

        // Extract data using config properties
        const xAxis = config.xAxis || Object.keys(data[0])[0];
        const yAxis = config.yAxis || Object.keys(data[0])[1];
        const value = config.value || Object.keys(data[0])[2];

        // Get unique x and y values
        const xValues = [...new Set(data.map(item => item[xAxis]))].sort();
        const yValues = [...new Set(data.map(item => item[yAxis]))].sort();

        // Create the heatmap grid
        const heatmapContainer = document.createElement('div');
        heatmapContainer.className = 'heatmap-container';
        heatmapContainer.style.display = 'grid';
        heatmapContainer.style.gridTemplateColumns = `auto ${new Array(xValues.length).fill('1fr').join(' ')}`;
        heatmapContainer.style.gridTemplateRows = `auto ${new Array(yValues.length).fill('1fr').join(' ')}`;
        heatmapContainer.style.gap = '1px';
        heatmapContainer.style.width = '100%';
        heatmapContainer.style.height = '100%';

        // Add x-axis labels (top row)
        const topLeft = document.createElement('div');
        topLeft.className = 'heatmap-header';
        heatmapContainer.appendChild(topLeft);

        // Add x-axis headers
        for (const x of xValues) {
            const xHeader = document.createElement('div');
            xHeader.className = 'heatmap-header';
            xHeader.textContent = x;
            xHeader.style.padding = '8px';
            xHeader.style.textAlign = 'center';
            xHeader.style.fontWeight = 'bold';
            heatmapContainer.appendChild(xHeader);
        }

        // Find min and max values for color scaling
        const values = data.map(item => parseFloat(item[value])).filter(v => !isNaN(v));
        const minValue = Math.min(...values);
        const maxValue = Math.max(...values);

        // Add y-axis labels and data cells
        for (const y of yValues) {
            // Add y-axis header
            const yHeader = document.createElement('div');
            yHeader.className = 'heatmap-header';
            yHeader.textContent = y;
            yHeader.style.padding = '8px';
            yHeader.style.fontWeight = 'bold';
            yHeader.style.display = 'flex';
            yHeader.style.alignItems = 'center';
            heatmapContainer.appendChild(yHeader);

            // Add data cells for this row
            for (const x of xValues) {
                const cellData = data.find(item => item[xAxis] === x && item[yAxis] === y);
                const cell = document.createElement('div');
                cell.className = 'heatmap-cell';

                if (cellData) {
                    const cellValue = parseFloat(cellData[value]);
                    if (!isNaN(cellValue)) {
                        const intensity = (cellValue - minValue) / (maxValue - minValue);
                        cell.style.backgroundColor = getColorForIntensity(intensity, colors[0]);
                        cell.textContent = cellValue.toLocaleString();
                        cell.title = `${x}, ${y}: ${cellValue.toLocaleString()}`;
                    }
                }

                cell.style.display = 'flex';
                cell.style.justifyContent = 'center';
                cell.style.alignItems = 'center';
                cell.style.color = 'white';
                cell.style.textShadow = '0 0 2px rgba(0,0,0,0.5)';
                cell.style.fontWeight = 'bold';
                cell.style.transition = 'transform 0.2s ease';

                // Add hover effect
                cell.addEventListener('mouseenter', () => {
                    cell.style.transform = 'scale(1.05)';
                    cell.style.zIndex = '1';
                });

                cell.addEventListener('mouseleave', () => {
                    cell.style.transform = 'scale(1)';
                    cell.style.zIndex = '0';
                });

                heatmapContainer.appendChild(cell);
            }
        }

        // Add legend
        const legend = document.createElement('div');
        legend.className = 'heatmap-legend';
        legend.style.display = 'flex';
        legend.style.alignItems = 'center';
        legend.style.justifyContent = 'center';
        legend.style.marginTop = '10px';

        const legendGradient = document.createElement('div');
        legendGradient.style.width = '200px';
        legendGradient.style.height = '20px';
        legendGradient.style.background = `linear-gradient(to right, ${getColorForIntensity(0, colors[0])}, ${getColorForIntensity(1, colors[0])})`;
        legendGradient.style.borderRadius = '3px';
        legend.appendChild(legendGradient);

        const legendMin = document.createElement('div');
        legendMin.textContent = minValue.toLocaleString();
        legendMin.style.marginRight = '10px';
        legend.appendChild(legendMin);

        const legendMax = document.createElement('div');
        legendMax.textContent = maxValue.toLocaleString();
        legendMax.style.marginLeft = '10px';
        legend.appendChild(legendMax);

        // Append the container and legend to the parent
        container.appendChild(heatmapContainer);
        container.appendChild(legend);

        // Animated entrance
        gsapFadeIn(heatmapContainer, { y: 20, duration: 0.8 });
        gsapFadeIn(legend, { y: 20, duration: 0.8, delay: 0.3 });
    }

    /**
     * Renders a gauge chart
     */
    function renderGaugeChart(id, data, config, colors) {
        const canvas = document.getElementById(`chart-${id}`);
        const ctx = canvas.getContext('2d');

        // Extract data using config properties
        const value = config.value || Object.keys(data[0])[0];
        const min = config.min || 0;
        const max = config.max || 100;

        // Calculate the gauge value
        let gaugeValue = 0;
        if (typeof value === 'string') {
            // If value is a column name, calculate average
            gaugeValue = data.reduce((sum, item) => sum + parseFloat(item[value] || 0), 0) / data.length;
        } else {
            // If value is a direct number
            gaugeValue = parseFloat(value);
        }

        // Normalize to 0-1 range
        const normalizedValue = (gaugeValue - min) / (max - min);

        // Create chart configuration
        const chartConfig = {
            type: 'doughnut',
            data: {
                datasets: [{
                    data: [normalizedValue, 1 - normalizedValue],
                    backgroundColor: [
                        getColorForIntensity(normalizedValue, colors[0]),
                        'rgba(200, 200, 200, 0.2)'
                    ],
                    borderWidth: 0,
                    circumference: 180,
                    rotation: 270
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '75%',
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        enabled: false
                    }
                },
                animation: {
                    duration: 1500,
                    easing: 'easeOutQuart'
                }
            },
            plugins: [{
                id: 'gaugeText',
                afterDraw: (chart) => {
                    const { ctx, width, height } = chart;
                    ctx.restore();

                    // Draw the value
                    const fontSize = Math.min(width, height) / 10;
                    ctx.font = `bold ${fontSize}px 'Inter', sans-serif`;
                    ctx.textBaseline = 'middle';
                    ctx.textAlign = 'center';

                    const text = `${gaugeValue.toLocaleString()}`;
                    const textX = width / 2;
                    const textY = height - height / 3;

                    ctx.fillStyle = darkModeEnabled ? '#e9ecef' : '#495057';
                    ctx.fillText(text, textX, textY);

                    // Draw the label
                    const labelFontSize = fontSize * 0.6;
                    ctx.font = `${labelFontSize}px 'Inter', sans-serif`;
                    const label = config.title || '';
                    ctx.fillText(label, textX, textY + fontSize);

                    // Draw the min/max values
                    const smallFontSize = fontSize * 0.4;
                    ctx.font = `${smallFontSize}px 'Inter', sans-serif`;
                    ctx.fillStyle = darkModeEnabled ? '#adb5bd' : '#6c757d';
                    ctx.fillText(min.toLocaleString(), width * 0.1, height - height / 4);
                    ctx.fillText(max.toLocaleString(), width * 0.9, height - height / 4);

                    ctx.save();
                }
            }]
        };

        // Create and store the chart
        chartInstances[id] = new Chart(ctx, chartConfig);
    }

    /**
     * Renders a text block
     */
    function renderTextBlock(id, data, config) {
        const container = document.getElementById(`viz-content-${id}`);
        container.innerHTML = '';

        // Create text container
        const textContainer = document.createElement('div');
        textContainer.className = 'text-block';
        textContainer.style.padding = '1rem';
        textContainer.style.overflow = 'auto';
        textContainer.style.height = '100%';
        textContainer.style.opacity = '0';
        textContainer.style.transform = 'translateY(20px)';

        // Create title element if provided
        if (config.title) {
            const title = document.createElement('h3');
            title.textContent = config.title;
            title.style.marginTop = '0';
            title.style.marginBottom = '0.75rem';
            title.style.fontSize = '1.125rem';
            title.style.fontWeight = '600';
            textContainer.appendChild(title);
        }

        // Create content element
        const content = document.createElement('div');
        content.innerHTML = config.content || 'Add text content here...';
        content.style.lineHeight = '1.6';
        content.style.color = darkModeEnabled ? '#e9ecef' : '#495057';
        textContainer.appendChild(content);

        // Add to container
        container.appendChild(textContainer);

        // Animate entrance
        gsapFadeIn(textContainer, { y: 20, duration: 0.8 });
    }

    /**
     * Renders a map visualization (simplified version)
     */
    function renderMapVisualization(id, data, config, colors) {
        const container = document.getElementById(`viz-content-${id}`);
        container.innerHTML = '';

        // Create placeholder for map (since we can't use actual maps)
        const mapPlaceholder = document.createElement('div');
        mapPlaceholder.className = 'map-placeholder';
        mapPlaceholder.style.width = '100%';
        mapPlaceholder.style.height = '100%';
        mapPlaceholder.style.backgroundColor = '#f8f9fa';
        mapPlaceholder.style.borderRadius = '0.5rem';
        mapPlaceholder.style.display = 'flex';
        mapPlaceholder.style.flexDirection = 'column';
        mapPlaceholder.style.justifyContent = 'center';
        mapPlaceholder.style.alignItems = 'center';
        mapPlaceholder.style.padding = '1rem';
        mapPlaceholder.style.opacity = '0';
        mapPlaceholder.style.transform = 'scale(0.95)';

        // Map icon
        const icon = document.createElement('div');
        icon.innerHTML = '<i class="fas fa-map-marked-alt"></i>';
        icon.style.fontSize = '3rem';
        icon.style.color = colors[0];
        icon.style.marginBottom = '1rem';
        mapPlaceholder.appendChild(icon);

        // Map title
        const title = document.createElement('h3');
        title.textContent = config.title || 'Map Visualization';
        title.style.marginBottom = '0.5rem';
        mapPlaceholder.appendChild(title);

        // Map description
        const description = document.createElement('p');
        description.textContent = 'Geographic visualization would be displayed here.';
        description.style.marginBottom = '1.5rem';
        description.style.textAlign = 'center';
        description.style.color = '#6c757d';
        mapPlaceholder.appendChild(description);

        // Add to container
        container.appendChild(mapPlaceholder);

        // Animate entrance
        gsapFadeIn(mapPlaceholder, { scale: 0.95, duration: 0.8 });
    }

    /**
     * Renders a presentation slide
     */
    function renderPresentationSlide(slideIndex, dataJson) {
        const data = JSON.parse(dataJson);
        const container = document.getElementById(`presentation-viz-${slideIndex}`);

        if (!container) return;

        container.innerHTML = '';

        // Create canvas element
        const canvas = document.createElement('canvas');
        canvas.id = `presentation-chart-${slideIndex}`;
        container.appendChild(canvas);

        // Determine visualization type based on slide index
        const vizTypes = ['bar', 'line', 'pie', 'gauge'];
        const vizType = vizTypes[slideIndex % vizTypes.length];

        // Get color scheme
        const colors = colorSchemes.default;

        // Create config
        const config = {
            showLegend: true,
            showTooltips: true,
            showGrid: true
        };

        // Render the visualization
        if (vizType === 'bar') {
            renderBarChart(`presentation-slide-${slideIndex}`, data, {
                xAxis: Object.keys(data[0])[0],
                yAxis: Object.keys(data[0])[1],
                ...config
            }, colors);
        } else if (vizType === 'line') {
            renderLineChart(`presentation-slide-${slideIndex}`, data, {
                xAxis: Object.keys(data[0])[0],
                yAxis: Object.keys(data[0])[1],
                ...config
            }, colors);
        } else if (vizType === 'pie') {
            renderPieChart(`presentation-slide-${slideIndex}`, data, {
                category: Object.keys(data[0])[0],
                value: Object.keys(data[0])[1],
                ...config
            }, colors);
        } else if (vizType === 'gauge') {
            renderGaugeChart(`presentation-slide-${slideIndex}`, data, {
                value: Object.keys(data[0])[1],
                min: 0,
                max: 100,
                ...config
            }, colors);
        }
    }

    /* ANIMATION FUNCTIONS */

    /**
     * Fades in an element with GSAP-like animation
     */
    function gsapFadeIn(element, options = {}) {
        const { x = 0, y = 0, scale = 1, opacity = 0, duration = 0.5, delay = 0 } = options;

        // Set initial state
        element.style.opacity = '0';
        element.style.transform = `translate(${x}px, ${y}px) scale(${scale})`;
        element.style.transition = `opacity ${duration}s ease, transform ${duration}s ease`;
        element.style.transitionDelay = `${delay}s`;

        // Trigger animation
        setTimeout(() => {
            element.style.opacity = '1';
            element.style.transform = 'translate(0, 0) scale(1)';
        }, 50);
    }

    /**
     * Animates the initial elements when the component loads
     */
    function animateInitialElements() {
        // Animate header
        const header = document.querySelector('.story-header');
        if (header) {
            gsapFadeIn(header, { y: -20, duration: 0.5 });
        }

        // Animate each section with staggered delay
        const sections = ['.palette', '.canvas', '.insights'];
        sections.forEach((selector, index) => {
            const element = document.querySelector(selector);
            if (element) {
                gsapFadeIn(element, { y: 20, duration: 0.6, delay: 0.1 * (index + 1) });
            }
        });

        // Animate viz items
        const vizItems = document.querySelectorAll('.viz-item');
        vizItems.forEach((item, index) => {
            gsapFadeIn(item, { y: 10, duration: 0.5, delay: 0.05 * (index + 1) });
        });

        // Animate scene tabs
        const sceneTabs = document.querySelectorAll('.scene-tab');
        sceneTabs.forEach((tab, index) => {
            gsapFadeIn(tab, { y: -10, duration: 0.4, delay: 0.07 * (index + 1) });
        });

        // Animate dropzones
        const dropzones = document.querySelectorAll('.dropzone');
        dropzones.forEach((zone, index) => {
            gsapFadeIn(zone, { opacity: 0, duration: 0.4, delay: 0.1 * (index + 1) });
        });

        // Animate insights
        const insights = document.querySelectorAll('.insight-card');
        insights.forEach((insight, index) => {
            gsapFadeIn(insight, { y: 10, duration: 0.5, delay: 0.1 * (index + 1) });
        });
    }

    /**
     * Animates a newly created visualization
     */
    function animateNewVisualization(id) {
        const element = document.getElementById(id);
        if (element) {
            element.style.opacity = '0';
            element.style.transform = 'translateY(20px)';

            setTimeout(() => {
                element.style.opacity = '1';
                element.style.transform = 'translateY(0)';
                element.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
            }, 50);
        }
    }

    /**
     * Animates removing a visualization
     */
    function animateRemoveVisualization(id) {
        const element = document.getElementById(id);
        if (element) {
            element.style.opacity = '0';
            element.style.transform = 'scale(0.95)';
            element.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
        }
    }

    /**
     * Animates a pulsing effect on an insight
     */
    function pulseInsight(index) {
        const insights = document.querySelectorAll('.insight-card');
        if (index >= 0 && index < insights.length) {
            const insight = insights[index];

            // Add pulse class
            insight.classList.add('insight-pulse');

            // Remove pulse class after animation completes
            setTimeout(() => {
                insight.classList.remove('insight-pulse');
            }, 4000);
        }
    }

    /**
     * Animates a new insight being added
     */
    function animateNewInsight(index) {
        const insights = document.querySelectorAll('.insight-card');
        if (index >= 0 && index < insights.length) {
            const insight = insights[index];

            // Add new-insight class for animation
            insight.classList.add('new-insight');

            // Remove class after animation completes
            setTimeout(() => {
                insight.classList.remove('new-insight');
            }, 1000);
        }
    }

    /* PRESENTATION MODE */

    /**
     * Starts presentation mode
     */
    function startPresentationMode() {
        document.body.style.overflow = 'hidden';

        // Find active slide
        const activeSlide = document.querySelector('.presentation-slide.active');
        if (activeSlide) {
            gsapFadeIn(activeSlide, { opacity: 0, scale: 0.95, duration: 0.8 });
        }
    }

    /**
     * Ends presentation mode
     */
    function endPresentationMode() {
        document.body.style.overflow = '';
    }

    /**
     * Transitions to the next slide
     */
    function transitionToNextSlide() {
        const currentSlide = document.querySelector('.presentation-slide.active');
        const nextSlide = currentSlide?.nextElementSibling;

        if (currentSlide && nextSlide && nextSlide.classList.contains('presentation-slide')) {
            // Animate current slide out
            currentSlide.style.opacity = '0';
            currentSlide.style.transform = 'translateX(-50px)';
            currentSlide.style.transition = 'opacity 0.5s ease, transform 0.5s ease';

            setTimeout(() => {
                currentSlide.classList.remove('active');
                currentSlide.classList.add('prev');

                // Animate next slide in
                nextSlide.classList.add('active');
                nextSlide.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
                nextSlide.style.transitionDelay = '0.1s';

                setTimeout(() => {
                    nextSlide.style.opacity = '1';
                    nextSlide.style.transform = 'translateX(0)';
                    nextSlide.style.visibility = 'visible';
                }, 50);
            }, 500);
        }
    }

    /**
     * Transitions to the previous slide
     */
    function transitionToPrevSlide() {
        const currentSlide = document.querySelector('.presentation-slide.active');
        const prevSlides = document.querySelectorAll('.presentation-slide.prev');
        const prevSlide = prevSlides[prevSlides.length - 1];

        if (currentSlide && prevSlide) {
            // Animate current slide out
            currentSlide.style.opacity = '0';
            currentSlide.style.transform = 'translateX(50px)';
            currentSlide.style.transition = 'opacity 0.5s ease, transform 0.5s ease';

            setTimeout(() => {
                currentSlide.classList.remove('active');

                // Animate previous slide in
                prevSlide.classList.remove('prev');
                prevSlide.classList.add('active');
                prevSlide.style.visibility = 'visible';
                prevSlide.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
                prevSlide.style.transitionDelay = '0.1s';

                setTimeout(() => {
                    prevSlide.style.opacity = '1';
                    prevSlide.style.transform = 'translateX(0)';
                }, 50);
            }, 500);
        }
    }

    /**
     * Hides the loading overlay with animation
     */
    function hideLoadingOverlay() {
        const overlay = document.querySelector('.loading-overlay');
        if (overlay) {
            overlay.style.opacity = '0';
            overlay.style.transition = 'opacity 0.5s ease';

            setTimeout(() => {
                overlay.style.display = 'none';
            }, 500);
        }
    }

    /* UTILITY FUNCTIONS */

    /**
     * Groups data by category and aggregates values
     */
    function groupDataByCategory(data, categoryKey, valueKey) {
        const grouped = {};

        for (const item of data) {
            const category = item[categoryKey];
            const value = parseFloat(item[valueKey]) || 0;

            if (!grouped[category]) {
                grouped[category] = 0;
            }

            grouped[category] += value;
        }

        return grouped;
    }

    /**
     * Darkens a color by the specified amount
     */
    function darkenColor(color, amount) {
        // Convert hex to RGB
        let r, g, b;

        if (color.startsWith('#')) {
            const hex = color.substring(1);
            r = parseInt(hex.substring(0, 2), 16);
            g = parseInt(hex.substring(2, 4), 16);
            b = parseInt(hex.substring(4, 6), 16);
        } else if (color.startsWith('rgb')) {
            const rgb = color.match(/\d+/g);
            r = parseInt(rgb[0]);
            g = parseInt(rgb[1]);
            b = parseInt(rgb[2]);
        } else {
            return color;
        }

        // Darken
        r = Math.max(0, Math.round(r * (1 - amount)));
        g = Math.max(0, Math.round(g * (1 - amount)));
        b = Math.max(0, Math.round(b * (1 - amount)));

        return `rgb(${r}, ${g}, ${b})`;
    }

    /**
     * Adds alpha channel to a color
     */
    function addAlpha(color, alpha) {
        // Convert hex to RGB
        let r, g, b;

        if (color.startsWith('#')) {
            const hex = color.substring(1);
            r = parseInt(hex.substring(0, 2), 16);
            g = parseInt(hex.substring(2, 4), 16);
            b = parseInt(hex.substring(4, 6), 16);
        } else if (color.startsWith('rgb')) {
            const rgb = color.match(/\d+/g);
            r = parseInt(rgb[0]);
            g = parseInt(rgb[1]);
            b = parseInt(rgb[2]);
        } else {
            return color;
        }

        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }

    /**
     * Gets a color based on intensity value (0-1)
     */
    function getColorForIntensity(intensity, baseColor) {
        // For heat map visualization
        if (intensity <= 0.25) {
            return addAlpha(baseColor, 0.2 + intensity * 2);
        } else if (intensity <= 0.5) {
            return addAlpha(baseColor, 0.5 + (intensity - 0.25));
        } else if (intensity <= 0.75) {
            return baseColor;
        } else {
            return darkenColor(baseColor, (intensity - 0.75) * 0.5);
        }
    }

    /**
     * Toggles dark mode for visualizations
     */
    function toggleDarkMode(enabled) {
        darkModeEnabled = enabled;
        setChartDefaults();

        // Update all charts
        for (const id in chartInstances) {
            if (chartInstances.hasOwnProperty(id)) {
                chartInstances[id].update();
            }
        }
    }

    // Public API
    return {
        initialize,
        renderVisualization,
        hideLoadingOverlay,
        animateInitialElements,
        animateNewVisualization,
        animateRemoveVisualization,
        pulseInsight,
        animateNewInsight,
        startPresentationMode,
        endPresentationMode,
        transitionToNextSlide,
        transitionToPrevSlide,
        renderPresentationSlide,
        toggleDarkMode
    };
})();