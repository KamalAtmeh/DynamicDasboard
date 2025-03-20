/**
 * StoryCanvas.js
 * JavaScript module for the Data Storytelling Canvas component
 * Uses ApexCharts for primary visualizations with D3.js for enhanced animations
 */
// Check if window is defined to avoid errors in server-side environments
//if (typeof window !== 'undefined') {
//    // Create the namespace if it doesn't exist
//    window.storyCanvas = window.storyCanvas || {};

//    // Define the initialize function that is called from Blazor
//    window.storyCanvas.initialize = function (dotNetHelper) {
//        console.log("StoryCanvas initialized with .NET helper:", dotNetHelper);
//        // Store the .NET helper for callbacks
//        window.storyCanvas._dotNetHelper = dotNetHelper;
        
//        // Add a debug statement to confirm initialization
//        console.log("StoryCanvas initialization complete");
        
//        return true;
//    };

//    // Add the setAnimationsEnabled function
//    window.storyCanvas.setAnimationsEnabled = function (enabled) {
//        console.log("Animations enabled:", enabled);
//        // Implementation will come later
//        return true;
//    };

//    // Add the hideLoadingOverlay function
//    window.storyCanvas.hideLoadingOverlay = function () {
//        console.log("Hiding loading overlay");
//        // Find and hide loading overlay
//        const overlay = document.querySelector('.loading-overlay');
//        if (overlay) {
//            overlay.style.opacity = '0';
//            overlay.style.transition = 'opacity 0.5s ease';
            
//            setTimeout(() => {
//                overlay.style.display = 'none';
//            }, 500);
//        }
//        return true;
//    };

//    // Add the animateInitialElements function
//    window.storyCanvas.animateInitialElements = function () {
//        console.log("Animating initial elements");
//        // Simple implementation for now
//        return true;
//    };

//    // Add the renderVisualization function stub
//    window.storyCanvas.renderVisualization = function (id, type, dataJson, configJson) {
//        console.log("Rendering visualization:", id, type);
//        console.log("Data:", dataJson);
//        console.log("Config:", configJson);
//        // Simple implementation for debugging
//        return true;
//    };
//}

// Create a namespace to avoid polluting the global scope
window.storyCanvas = (function () {
    // Private properties
    let dotNetHelper = null;
    let chartInstances = {};
    let darkModeEnabled = false;
    let isDragging = false;
    let d3Animations = {};

    // Color schemes for charts
    const colorSchemes = {
        default: ['#4361ee', '#4895ef', '#4cc9f0', '#f72585', '#f8961e', '#06d6a0', '#8338ec', '#3a0ca3'],
        monochrome: ['#4361ee', '#5171f0', '#6081f2', '#7090f4', '#80a0f6', '#90aff8', '#a0bffa', '#b0cefc'],
        warm: ['#f72585', '#f94096', '#fb5ca6', '#fc77b7', '#fd93c7', '#fea7d0', '#febdd9', '#ffd4e3'],
        cool: ['#4cc9f0', '#60cff2', '#74d5f4', '#88dbf6', '#9ce1f8', '#b0e7fa', '#c4edfc', '#d8f3fe'],
        earth: ['#b6c197', '#a3b78e', '#90ad86', '#7da37d', '#6a9975', '#578f6c', '#448564', '#307b5b'],
    };

    /**
     * Initializes the StoryCanvas
     * @param {object} helper - The .NET helper object for callbacks
     */
    function initialize(helper) {
        dotNetHelper = helper;

        // Load ApexCharts and D3 libraries dynamically
        function waitForLibraries() {
            if (typeof ApexCharts !== 'undefined' && typeof d3 !== 'undefined') {
                setupDragAndDrop();
                document.addEventListener('keydown', handleKeyDown);
                window.addEventListener('resize', handleResize);
                console.log('StoryCanvas initialized with ApexCharts and D3');
                return true;
            } else {
                console.log('Libraries not available yet, retrying...');
                setTimeout(waitForLibraries, 100);
            }
        }

        waitForLibraries();

        // Set up drag and drop event listeners
        setupDragAndDrop();

        // Set up keyboard event listeners
        document.addEventListener('keydown', handleKeyDown);

        // Set up resize listener for responsive charts
        window.addEventListener('resize', handleResize);

        console.log('StoryCanvas initialization started');

        return true;
    }

    function setAnimationsEnabled(enabled) {
        // This would be added to your JavaScript file
        const animationSettings = {
            enabled: enabled,
            easing: 'easeinout',
            speed: enabled ? 800 : 10, // Fast speed when disabled (effectively no animation)
        };

        // Update all existing charts
        for (const id in chartInstances) {
            if (chartInstances.hasOwnProperty(id) && chartInstances[id].chart) {
                chartInstances[id].chart.updateOptions({
                    chart: {
                        animations: animationSettings
                    }
                });
            }
        }
    }

    // Add to public API
    return {
        // Existing methods...
        setAnimationsEnabled,
    };

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
                    if (chartInstances[id].chart) {
                        chartInstances[id].chart.updateOptions({
                            chart: {
                                width: '100%'
                            }
                        });
                    }
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
            if (chartInstances[id].chart) {
                chartInstances[id].chart.destroy();
            }
            delete chartInstances[id];
        }

        // Clear container
        container.innerHTML = '';

        // Create chart container element
        const chartContainer = document.createElement('div');
        chartContainer.id = `chart-${id}`;
        chartContainer.style.width = '100%';
        chartContainer.style.height = '100%';
        container.appendChild(chartContainer);

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
     * Renders a bar chart using ApexCharts
     */
    function renderBarChart(id, data, config, colors) {
        const chartElement = document.getElementById(`chart-${id}`);

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

        // Create chart options
        const options = {
            series: [{
                name: yAxis,
                data: chartData.map(item => item[yAxis])
            }],
            chart: {
                type: 'bar',
                height: '100%',
                fontFamily: "'Inter', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
                animations: {
                    enabled: true,
                    easing: 'easeinout',
                    speed: 800,
                    animateGradually: {
                        enabled: true,
                        delay: 150
                    },
                    dynamicAnimation: {
                        enabled: true,
                        speed: 350
                    }
                },
                toolbar: {
                    show: true,
                    tools: {
                        download: true,
                        selection: true,
                        zoom: true,
                        zoomin: true,
                        zoomout: true,
                        pan: true,
                        reset: true
                    }
                },
                background: 'transparent'
            },
            colors: [colors[0]],
            plotOptions: {
                bar: {
                    borderRadius: 4,
                    distributed: false,
                    dataLabels: {
                        position: 'top'
                    }
                }
            },
            dataLabels: {
                enabled: chartData.length <= 7,
                formatter: function (val) {
                    return val.toLocaleString();
                },
                offsetY: -20,
                style: {
                    fontSize: '12px',
                    colors: ["#304758"]
                }
            },
            grid: {
                show: config.showGrid !== false,
                borderColor: '#e0e0e0',
                strokeDashArray: 2
            },
            xaxis: {
                categories: chartData.map(item => item[xAxis]),
                labels: {
                    style: {
                        fontSize: '12px'
                    }
                },
                axisBorder: {
                    show: true
                },
                axisTicks: {
                    show: true
                },
                title: {
                    text: xAxis,
                    style: {
                        fontSize: '12px',
                        fontWeight: 600
                    }
                }
            },
            yaxis: {
                labels: {
                    formatter: function (val) {
                        return val.toLocaleString();
                    },
                    style: {
                        fontSize: '12px'
                    }
                },
                title: {
                    text: yAxis,
                    style: {
                        fontSize: '12px',
                        fontWeight: 600
                    }
                }
            },
            tooltip: {
                enabled: config.showTooltips !== false,
                y: {
                    formatter: function (val) {
                        return val.toLocaleString();
                    }
                }
            },
            legend: {
                show: config.showLegend !== false,
                position: 'top',
                fontSize: '13px'
            },
            theme: {
                mode: darkModeEnabled ? 'dark' : 'light'
            }
        };

        // Create and store the chart
        const chart = new ApexCharts(chartElement, options);
        chart.render();

        // Store the chart instance
        chartInstances[id] = {
            chart: chart,
            type: 'bar'
        };

        // Add D3 animation enhancement
        enhanceWithD3Animation(id, chartElement, chartData, xAxis, yAxis);
    }

    /**
     * Enhances the chart with D3 animations
     */
    function enhanceWithD3Animation(id, element, data, xKey, yKey) {
        // Clean up any existing D3 animations
        if (d3Animations[id]) {
            d3Animations[id].cleanup();
            delete d3Animations[id];
        }

        // Create hover effect for the bars using D3
        try {
            setTimeout(() => {
                const bars = d3.select(element).selectAll('.apexcharts-bar-series .apexcharts-bar-area');

                const originalColors = [];

                bars.each(function () {
                    originalColors.push(d3.select(this).attr('fill'));
                });

                bars.on('mouseenter', function (event, d) {
                    d3.select(this)
                        .transition()
                        .duration(300)
                        .attr('filter', 'url(#drop-shadow)')
                        .attr('stroke-width', 2)
                        .attr('stroke', '#ffffff');
                })
                    .on('mouseleave', function () {
                        d3.select(this)
                            .transition()
                            .duration(300)
                            .attr('filter', null)
                            .attr('stroke-width', 0);
                    });

                // Store animation info for cleanup
                d3Animations[id] = {
                    cleanup: function () {
                        bars.on('mouseenter', null).on('mouseleave', null);
                    }
                };
            }, 1000); // Wait for ApexCharts to finish its rendering
        } catch (e) {
            console.log('D3 enhancement skipped', e);
        }
    }

    /**
     * Renders a line chart using ApexCharts
     */
    function renderLineChart(id, data, config, colors) {
        const chartElement = document.getElementById(`chart-${id}`);

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

        // Create chart options
        const options = {
            series: [{
                name: yAxis,
                data: chartData.map(item => item[yAxis])
            }],
            chart: {
                type: 'line',
                height: '100%',
                fontFamily: "'Inter', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
                dropShadow: {
                    enabled: true,
                    color: colors[0],
                    top: 3,
                    left: 2,
                    blur: 4,
                    opacity: 0.2
                },
                toolbar: {
                    show: true
                },
                animations: {
                    enabled: true,
                    easing: 'easeinout',
                    speed: 1000,
                    animateGradually: {
                        enabled: true,
                        delay: 150
                    },
                    dynamicAnimation: {
                        enabled: true,
                        speed: 550
                    }
                }
            },
            colors: [colors[0]],
            stroke: {
                curve: 'smooth',
                width: 3
            },
            fill: {
                type: 'gradient',
                gradient: {
                    shade: 'dark',
                    gradientToColors: [colors[1] || colors[0]],
                    shadeIntensity: 1,
                    type: 'horizontal',
                    opacityFrom: 0.7,
                    opacityTo: 0.2
                },
            },
            markers: {
                size: 5,
                colors: [colors[0]],
                strokeColors: '#fff',
                strokeWidth: 2,
                hover: {
                    size: 8,
                }
            },
            grid: {
                show: config.showGrid !== false,
                borderColor: '#e0e0e0',
                strokeDashArray: 2,
                position: 'back'
            },
            xaxis: {
                categories: chartData.map(item => item[xAxis]),
                title: {
                    text: xAxis
                }
            },
            yaxis: {
                title: {
                    text: yAxis
                },
                labels: {
                    formatter: function (val) {
                        return val.toLocaleString();
                    }
                }
            },
            tooltip: {
                enabled: config.showTooltips !== false,
                shared: true,
                intersect: false,
                y: {
                    formatter: function (val) {
                        return val.toLocaleString();
                    }
                }
            },
            legend: {
                show: config.showLegend !== false,
                position: 'top'
            },
            theme: {
                mode: darkModeEnabled ? 'dark' : 'light'
            }
        };

        // Create and store the chart
        const chart = new ApexCharts(chartElement, options);
        chart.render();

        chartInstances[id] = {
            chart: chart,
            type: 'line'
        };

        // Add D3 animation enhancement for line path
        enhanceLineChartWithD3(id, chartElement, chartData, xAxis, yAxis);
    }

    /**
     * Enhances a line chart with D3 animations
     */
    function enhanceLineChartWithD3(id, element, data, xKey, yKey) {
        // Clean up any existing D3 animations
        if (d3Animations[id]) {
            d3Animations[id].cleanup();
            delete d3Animations[id];
        }

        // Add path animation
        setTimeout(() => {
            try {
                const linePath = d3.select(element).select('.apexcharts-line-series .apexcharts-series path');
                const totalLength = linePath.node().getTotalLength();

                // Set up initial state
                linePath
                    .attr("stroke-dasharray", totalLength)
                    .attr("stroke-dashoffset", totalLength)
                    .transition()
                    .duration(1500)
                    .ease(d3.easeLinear)
                    .attr("stroke-dashoffset", 0);

                // Add interactive effects
                const dots = d3.select(element).selectAll('.apexcharts-series-markers circle');

                dots.on('mouseenter', function () {
                    d3.select(this)
                        .transition()
                        .duration(300)
                        .attr('r', 8);
                })
                    .on('mouseleave', function () {
                        d3.select(this)
                            .transition()
                            .duration(300)
                            .attr('r', 5);
                    });

                // Store animation info for cleanup
                d3Animations[id] = {
                    cleanup: function () {
                        dots.on('mouseenter', null).on('mouseleave', null);
                    }
                };
            } catch (e) {
                console.log('D3 line enhancement skipped', e);
            }
        }, 500);
    }

    /**
     * Renders a pie chart using ApexCharts
     */
    function renderPieChart(id, data, config, colors) {
        const chartElement = document.getElementById(`chart-${id}`);

        // Extract data using config properties
        const category = config.category || Object.keys(data[0])[0];
        const value = config.value || Object.keys(data[0])[1];

        // Group data by category
        const grouped = groupDataByCategory(data, category, value);

        // Convert grouped data for chart
        const labels = Object.keys(grouped);
        const values = Object.values(grouped);

        // Create chart options
        const options = {
            series: values,
            chart: {
                type: 'pie',
                height: '100%',
                fontFamily: "'Inter', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
                toolbar: {
                    show: true
                },
                animations: {
                    enabled: true,
                    easing: 'easeinout',
                    speed: 800,
                    animateGradually: {
                        enabled: true,
                        delay: 150
                    },
                    dynamicAnimation: {
                        enabled: true,
                        speed: 350
                    }
                }
            },
            colors: colors,
            labels: labels,
            legend: {
                show: config.showLegend !== false,
                position: 'right',
                fontSize: '13px',
                formatter: function (seriesName, opts) {
                    return seriesName + ":  " + opts.w.globals.series[opts.seriesIndex].toLocaleString();
                }
            },
            plotOptions: {
                pie: {
                    donut: {
                        size: '0%'
                    },
                    expandOnClick: true
                }
            },
            dataLabels: {
                enabled: true,
                formatter: function (val, opts) {
                    return opts.w.globals.labels[opts.seriesIndex] + ": " + val.toFixed(1) + "%";
                },
                style: {
                    fontSize: '12px',
                    fontFamily: "'Inter', sans-serif",
                    fontWeight: 'normal'
                },
                dropShadow: {
                    enabled: true
                }
            },
            tooltip: {
                enabled: config.showTooltips !== false,
                y: {
                    formatter: function (val) {
                        return val.toLocaleString();
                    }
                }
            },
            responsive: [{
                breakpoint: 480,
                options: {
                    chart: {
                        height: 300
                    },
                    legend: {
                        position: 'bottom'
                    }
                }
            }],
            theme: {
                mode: darkModeEnabled ? 'dark' : 'light'
            }
        };

        // Create and store the chart
        const chart = new ApexCharts(chartElement, options);
        chart.render();

        chartInstances[id] = {
            chart: chart,
            type: 'pie'
        };

        // Add D3 enhancements for pie slices
        enhancePieChartWithD3(id, chartElement, labels, values);
    }

    /**
     * Enhances a pie chart with D3 animations
     */
    function enhancePieChartWithD3(id, element, labels, values) {
        // Clean up any existing D3 animations
        if (d3Animations[id]) {
            d3Animations[id].cleanup();
            delete d3Animations[id];
        }

        setTimeout(() => {
            try {
                const pieSlices = d3.select(element).selectAll('.apexcharts-pie-series path');

                pieSlices.on('mouseenter', function () {
                    d3.select(this)
                        .transition()
                        .duration(200)
                        .attr('transform', 'translate(5, -5) scale(1.03)');
                })
                    .on('mouseleave', function () {
                        d3.select(this)
                            .transition()
                            .duration(200)
                            .attr('transform', 'translate(0, 0) scale(1)');
                    });

                // Store animation info for cleanup
                d3Animations[id] = {
                    cleanup: function () {
                        pieSlices.on('mouseenter', null).on('mouseleave', null);
                    }
                };
            } catch (e) {
                console.log('D3 pie enhancement skipped', e);
            }
        }, 1000);
    }

    /**
     * Renders a scatter chart using ApexCharts
     */
    function renderScatterChart(id, data, config, colors) {
        const chartElement = document.getElementById(`chart-${id}`);

        // Extract data using config properties
        const xAxis = config.xAxis || Object.keys(data[0])[0];
        const yAxis = config.yAxis || Object.keys(data[0])[1];

        // Prepare data for scatter plot
        const series = [{
            name: `${xAxis} vs ${yAxis}`,
            data: data.map(item => ({
                x: item[xAxis],
                y: item[yAxis]
            }))
        }];

        // Create chart options
        const options = {
            series: series,
            chart: {
                type: 'scatter',
                height: '100%',
                zoom: {
                    enabled: true,
                    type: 'xy'
                },
                toolbar: {
                    show: true
                },
                animations: {
                    enabled: true,
                    easing: 'easeinout',
                    speed: 800,
                    animateGradually: {
                        enabled: true,
                        delay: 150
                    },
                    dynamicAnimation: {
                        enabled: true,
                        speed: 350
                    }
                }
            },
            colors: [colors[0]],
            xaxis: {
                title: {
                    text: xAxis
                },
                tickAmount: 10
            },
            yaxis: {
                title: {
                    text: yAxis
                },
                tickAmount: 7
            },
            markers: {
                size: 6,
                strokeWidth: 1,
                hover: {
                    size: 8
                }
            },
            tooltip: {
                enabled: config.showTooltips !== false,
                x: {
                    formatter: function (val) {
                        return val.toLocaleString();
                    }
                },
                y: {
                    formatter: function (val) {
                        return val.toLocaleString();
                    }
                }
            },
            grid: {
                show: config.showGrid !== false,
                xaxis: {
                    lines: {
                        show: true
                    }
                },
                yaxis: {
                    lines: {
                        show: true
                    }
                }
            },
            legend: {
                show: config.showLegend !== false,
                position: 'top'
            },
            theme: {
                mode: darkModeEnabled ? 'dark' : 'light'
            }
        };

        // Create and store the chart
        const chart = new ApexCharts(chartElement, options);
        chart.render();

        chartInstances[id] = {
            chart: chart,
            type: 'scatter'
        };

        // Add D3 animation enhancements
        enhanceScatterChartWithD3(id, chartElement, data, xAxis, yAxis);
    }

    /**
     * Enhances a scatter chart with D3 animations
     */
    function enhanceScatterChartWithD3(id, element, data, xKey, yKey) {
        // Clean up any existing D3 animations
        if (d3Animations[id]) {
            d3Animations[id].cleanup();
            delete d3Animations[id];
        }

        setTimeout(() => {
            try {
                const points = d3.select(element).selectAll('.apexcharts-series-markers circle');

                // Add entrance animation with randomized delay
                points.each(function (d, i) {
                    const point = d3.select(this);
                    const delay = Math.random() * 500;

                    // Store original radius
                    const originalRadius = point.attr('r');

                    point
                        .attr('opacity', 0)
                        .attr('r', 0)
                        .transition()
                        .delay(delay)
                        .duration(800)
                        .attr('opacity', 1)
                        .attr('r', originalRadius);
                });

                // Add interactive hover effects
                points.on('mouseenter', function () {
                    d3.select(this)
                        .transition()
                        .duration(300)
                        .attr('r', 10)
                        .attr('stroke-width', 2);
                })
                    .on('mouseleave', function () {
                        d3.select(this)
                            .transition()
                            .duration(300)
                            .attr('r', 6)
                            .attr('stroke-width', 1);
                    });

                // Store animation info for cleanup
                d3Animations[id] = {
                    cleanup: function () {
                        points.on('mouseenter', null).on('mouseleave', null);
                    }
                };
            } catch (e) {
                console.log('D3 scatter enhancement skipped', e);
            }
        }, 800);
    }

    /**
     * Renders a heat map using ApexCharts
     */
    function renderHeatMap(id, data, config, colors) {
        const chartElement = document.getElementById(`chart-${id}`);

        // Extract data using config properties
        const xAxis = config.xAxis || Object.keys(data[0])[0];
        const yAxis = config.yAxis || Object.keys(data[0])[1];
        const value = config.value || Object.keys(data[0])[2];

        // Get unique x and y values
        const xValues = [...new Set(data.map(item => item[xAxis]))].sort();
        const yValues = [...new Set(data.map(item => item[yAxis]))].sort();

        // Prepare data for heatmap
        const series = yValues.map(y => {
            return {
                name: y,
                data: xValues.map(x => {
                    const matchingItem = data.find(item => item[xAxis] === x && item[yAxis] === y);
                    return {
                        x: x,
                        y: y,
                        value: matchingItem ? matchingItem[value] : 0
                    };
                })
            };
        });

        // Create chart options
        const options = {
            series: series,
            chart: {
                type: 'heatmap',
                height: '100%',
                toolbar: {
                    show: true
                },
                animations: {
                    enabled: true,
                    easing: 'easeinout',
                    speed: 800
                }
            },
            dataLabels: {
                enabled: series[0].data.length <= 10,
                formatter: function (val) {
                    return val !== null ? val.toLocaleString() : '';
                }
            },
            colors: [colors[0]],
            title: {
                text: config.title || '',
                align: 'center',
                style: {
                    fontSize: '14px'
                }
            },
            plotOptions: {
                heatmap: {
                    shadeIntensity: 0.5,
                    radius: 0,
                    colorScale: {
                        ranges: [{
                            from: 0,
                            to: 0,
                            color: '#EFEFEF',
                            name: 'No Data'
                        }]
                    }
                }
            },
            xaxis: {
                categories: xValues,
                title: {
                    text: xAxis
                }
            },
            yaxis: {
                title: {
                    text: yAxis
                }
            },
            tooltip: {
                enabled: config.showTooltips !== false,
                custom: function ({ series, seriesIndex, dataPointIndex, w }) {
                    const x = w.globals.labels[dataPointIndex];
                    const y = w.globals.seriesNames[seriesIndex];
                    const value = series[seriesIndex][dataPointIndex];
                    return `<div class="apexcharts-tooltip-custom">
                        <span><strong>${xAxis}:</strong> ${x}</span><br>
                        <span><strong>${yAxis}:</strong> ${y}</span><br>
                        <span><strong>${config.value || 'Value'}:</strong> ${value.toLocaleString()}</span>
                    </div>`;
                }
            },
            theme: {
                mode: darkModeEnabled ? 'dark' : 'light'
            }
        };

        // Create and store the chart
        const chart = new ApexCharts(chartElement, options);
        chart.render();

        chartInstances[id] = {
            chart: chart,
            type: 'heatmap'
        };

        // Add D3 animation enhancements
        enhanceHeatmapWithD3(id, chartElement, series);
    }

    /**
     * Enhances a heatmap with D3 animations
     */
    function enhanceHeatmapWithD3(id, element, data) {
        // Clean up any existing D3 animations
        if (d3Animations[id]) {
            d3Animations[id].cleanup();
            delete d3Animations[id];
        }

        setTimeout(() => {
            try {
                const cells = d3.select(element).selectAll('.apexcharts-heatmap-rect');

                // Add entrance animation
                cells.each(function (d, i) {
                    const cell = d3.select(this);
                    const delay = Math.random() * 800; // Random delay for each cell

                    cell
                        .attr('opacity', 0)
                        .transition()
                        .delay(delay)
                        .duration(500)
                        .attr('opacity', 1);
                });

                // Add hover effect
                cells.on('mouseenter', function () {
                    d3.select(this)
                        .transition()
                        .duration(200)
                        .attr('stroke', '#ffffff')
                        .attr('stroke-width', 2);
                })
                    .on('mouseleave', function () {
                        d3.select(this)
                            .transition()
                            .duration(200)
                            .attr('stroke', 'none')
                            .attr('stroke-width', 0);
                    });

                // Store animation info for cleanup
                d3Animations[id] = {
                    cleanup: function () {
                        cells.on('mouseenter', null).on('mouseleave', null);
                    }
                };
            } catch (e) {
                console.log('D3 heatmap enhancement skipped', e);
            }
        }, 800);
    }

    /**
     * Renders a gauge chart using ApexCharts
     */
    function renderGaugeChart(id, data, config, colors) {
        const chartElement = document.getElementById(`chart-${id}`);

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

        // Create chart options
        const options = {
            series: [gaugeValue],
            chart: {
                height: '100%',
                type: 'radialBar',
                toolbar: {
                    show: true
                },
                animations: {
                    enabled: true,
                    easing: 'easeinout',
                    speed: 800,
                    animateGradually: {
                        enabled: true,
                        delay: 150
                    },
                    dynamicAnimation: {
                        enabled: true,
                        speed: 350
                    }
                }
            },
            plotOptions: {
                radialBar: {
                    startAngle: -135,
                    endAngle: 135,
                    hollow: {
                        margin: 0,
                        size: '70%',
                        background: '#fff',
                        image: undefined,
                        imageOffsetX: 0,
                        imageOffsetY: 0,
                        position: 'front',
                        dropShadow: {
                            enabled: true,
                            top: 3,
                            left: 0,
                            blur: 4,
                            opacity: 0.24
                        }
                    },
                    track: {
                        background: '#fff',
                        strokeWidth: '67%',
                        margin: 0,
                        dropShadow: {
                            enabled: true,
                            top: -3,
                            left: 0,
                            blur: 4,
                            opacity: 0.35
                        }
                    },
                    dataLabels: {
                        show: true,
                        name: {
                            offsetY: -10,
                            show: true,
                            color: '#888',
                            fontSize: '17px'
                        },
                        value: {
                            formatter: function (val) {
                                return parseFloat(val).toFixed(1) + "%";
                            },
                            color: '#111',
                            fontSize: '36px',
                            show: true
                        }
                    }
                }
            },
            fill: {
                type: 'gradient',
                gradient: {
                    shade: 'dark',
                    type: 'horizontal',
                    shadeIntensity: 0.5,
                    gradientToColors: [colors[1] || colors[0]],
                    inverseColors: true,
                    opacityFrom: 1,
                    opacityTo: 1,
                    stops: [0, 100]
                }
            },
            stroke: {
                lineCap: 'round'
            },
            labels: [config.title || 'Value'],
            theme: {
                mode: darkModeEnabled ? 'dark' : 'light'
            }
        };

        // Create and store the chart
        const chart = new ApexCharts(chartElement, options);
        chart.render();

        chartInstances[id] = {
            chart: chart,
            type: 'gauge'
        };

        // Add D3 animation enhancement
        enhanceGaugeWithD3(id, chartElement, gaugeValue, min, max);
    }

    /**
     * Enhances a gauge chart with D3 animations
     */
    function enhanceGaugeWithD3(id, element, value, min, max) {
        // Clean up any existing D3 animations
        if (d3Animations[id]) {
            d3Animations[id].cleanup();
            delete d3Animations[id];
        }

        setTimeout(() => {
            try {
                // Add a subtle pulse animation to the gauge value
                const valueText = d3.select(element).select('.apexcharts-radial-series').select('text');

                if (valueText.node()) {
                    setInterval(() => {
                        valueText
                            .transition()
                            .duration(1000)
                            .attr('font-size', '38px')
                            .transition()
                            .duration(1000)
                            .attr('font-size', '36px');
                    }, 2000);

                    // Store animation info for cleanup
                    d3Animations[id] = {
                        cleanup: function () {
                            // No specific cleanup needed
                        }
                    };
                }
            } catch (e) {
                console.log('D3 gauge enhancement skipped', e);
            }
        }, 1000);
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

        // Animate entrance with D3
        animateTextBlockWithD3(textContainer);

        // Store as a non-chart instance
        chartInstances[id] = {
            type: 'text',
            element: textContainer
        };
    }

    /**
     * Animates a text block with D3
     */
    function animateTextBlockWithD3(element) {
        try {
            d3.select(element)
                .style('opacity', 0)
                .style('transform', 'translateY(20px)')
                .transition()
                .duration(600)
                .style('opacity', 1)
                .style('transform', 'translateY(0px)');

            // Apply a staggered animation to text paragraphs
            const paragraphs = d3.select(element).selectAll('p');
            paragraphs.each(function (d, i) {
                d3.select(this)
                    .style('opacity', 0)
                    .transition()
                    .delay(300 + (i * 100))
                    .duration(500)
                    .style('opacity', 1);
            });
        } catch (e) {
            console.log('D3 text animation skipped', e);
            // Fallback to CSS animation
            element.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
            setTimeout(() => {
                element.style.opacity = '1';
                element.style.transform = 'translateY(0)';
            }, 50);
        }
    }

    /**
     * Renders a map visualization with D3.js
     */
    function renderMapVisualization(id, data, config, colors) {
        const container = document.getElementById(`viz-content-${id}`);
        container.innerHTML = '';

        // For a proper map, we'd use D3's geo capabilities here
        // This is a simplified version that shows a basic map placeholder

        // Create SVG container for the map
        const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        svg.setAttribute("width", "100%");
        svg.setAttribute("height", "100%");
        svg.style.overflow = "visible";
        container.appendChild(svg);

        // Use D3 to create a simple map visualization
        try {
            const width = container.clientWidth;
            const height = container.clientHeight;

            const d3svg = d3.select(svg)
                .attr("viewBox", `0 0 ${width} ${height}`)
                .style("font-family", "'Inter', 'Segoe UI', Roboto, sans-serif");

            // Create a sample map with regions
            const regionData = [
                { name: "North", x: width * 0.3, y: height * 0.3, radius: 50, value: 75 },
                { name: "South", x: width * 0.3, y: height * 0.7, radius: 40, value: 55 },
                { name: "East", x: width * 0.7, y: height * 0.3, radius: 45, value: 65 },
                { name: "West", x: width * 0.7, y: height * 0.7, radius: 55, value: 85 }
            ];

            // Draw background
            d3svg.append("rect")
                .attr("width", width)
                .attr("height", height)
                .attr("fill", "#f8f9fa")
                .attr("rx", 10)
                .attr("ry", 10);

            // Draw connections
            d3svg.selectAll(".connection")
                .data([
                    { source: regionData[0], target: regionData[1] },
                    { source: regionData[0], target: regionData[2] },
                    { source: regionData[1], target: regionData[3] },
                    { source: regionData[2], target: regionData[3] }
                ])
                .enter()
                .append("line")
                .attr("x1", d => d.source.x)
                .attr("y1", d => d.source.y)
                .attr("x2", d => d.target.x)
                .attr("y2", d => d.target.y)
                .attr("stroke", "#ccc")
                .attr("stroke-width", 2)
                .attr("stroke-dasharray", "4,4")
                .style("opacity", 0)
                .transition()
                .delay((d, i) => i * 200)
                .duration(1000)
                .style("opacity", 0.5);

            // Draw regions
            const regions = d3svg.selectAll(".region")
                .data(regionData)
                .enter()
                .append("g")
                .attr("class", "region")
                .attr("transform", d => `translate(${d.x}, ${d.y})`)
                .style("opacity", 0);

            regions.transition()
                .delay((d, i) => i * 300)
                .duration(800)
                .style("opacity", 1);

            regions.append("circle")
                .attr("r", 0)
                .attr("fill", (d, i) => colors[i % colors.length])
                .attr("opacity", 0.7)
                .transition()
                .delay((d, i) => i * 300 + 200)
                .duration(1000)
                .attr("r", d => d.radius);

            regions.append("text")
                .attr("text-anchor", "middle")
                .attr("dy", "0.3em")
                .attr("fill", "white")
                .attr("font-weight", "bold")
                .text(d => d.name)
                .style("font-size", "0px")
                .transition()
                .delay((d, i) => i * 300 + 500)
                .duration(500)
                .style("font-size", "14px");

            regions.append("text")
                .attr("text-anchor", "middle")
                .attr("dy", "1.5em")
                .attr("fill", "white")
                .text(d => d.value)
                .style("font-size", "0px")
                .transition()
                .delay((d, i) => i * 300 + 800)
                .duration(500)
                .style("font-size", "18px");

            // Add interactions
            regions
                .on("mouseenter", function (event, d) {
                    d3.select(this).select("circle")
                        .transition()
                        .duration(300)
                        .attr("r", d.radius * 1.1)
                        .attr("opacity", 0.9);

                    d3.select(this).selectAll("text")
                        .transition()
                        .duration(300)
                        .style("font-size", function () {
                            return parseFloat(d3.select(this).style("font-size")) * 1.2 + "px";
                        });
                })
                .on("mouseleave", function (event, d) {
                    d3.select(this).select("circle")
                        .transition()
                        .duration(300)
                        .attr("r", d.radius)
                        .attr("opacity", 0.7);

                    d3.select(this).selectAll("text")
                        .transition()
                        .duration(300)
                        .style("font-size", function () {
                            return (parseFloat(d3.select(this).style("font-size")) / 1.2) + "px";
                        });
                });

            // Add title
            d3svg.append("text")
                .attr("x", width / 2)
                .attr("y", 25)
                .attr("text-anchor", "middle")
                .attr("font-size", "18px")
                .attr("font-weight", "bold")
                .text(config.title || "Regional Distribution")
                .style("opacity", 0)
                .transition()
                .delay(1200)
                .duration(800)
                .style("opacity", 1);

            // Add a legend
            const legend = d3svg.append("g")
                .attr("transform", `translate(${width - 100}, ${height - 80})`)
                .style("opacity", 0)
                .transition()
                .delay(1500)
                .duration(800)
                .style("opacity", 1);

            const legendTitle = d3svg.append("text")
                .attr("x", width - 100)
                .attr("y", height - 100)
                .attr("text-anchor", "start")
                .attr("font-size", "12px")
                .attr("font-weight", "bold")
                .text("Value Legend")
                .style("opacity", 0)
                .transition()
                .delay(1500)
                .duration(800)
                .style("opacity", 1);

            // Store chart instance
            chartInstances[id] = {
                type: 'map',
                d3svg: d3svg,
                element: container
            };
        } catch (e) {
            console.log('D3 map visualization error', e);

            // Fallback to simple placeholder
            const placeholderDiv = document.createElement('div');
            placeholderDiv.className = 'map-placeholder';
            placeholderDiv.style.width = '100%';
            placeholderDiv.style.height = '100%';
            placeholderDiv.style.backgroundColor = '#f8f9fa';
            placeholderDiv.style.borderRadius = '0.5rem';
            placeholderDiv.style.display = 'flex';
            placeholderDiv.style.flexDirection = 'column';
            placeholderDiv.style.justifyContent = 'center';
            placeholderDiv.style.alignItems = 'center';
            placeholderDiv.style.padding = '1rem';
            placeholderDiv.style.opacity = '0';
            placeholderDiv.style.transform = 'scale(0.95)';

            // Map icon
            const icon = document.createElement('div');
            icon.innerHTML = '<i class="fas fa-map-marked-alt"></i>';
            icon.style.fontSize = '3rem';
            icon.style.color = colors[0];
            icon.style.marginBottom = '1rem';
            placeholderDiv.appendChild(icon);

            // Map title
            const title = document.createElement('h3');
            title.textContent = config.title || 'Map Visualization';
            title.style.marginBottom = '0.5rem';
            placeholderDiv.appendChild(title);

            // Map description
            const description = document.createElement('p');
            description.textContent = 'Geographic visualization would be displayed here.';
            description.style.marginBottom = '1.5rem';
            description.style.textAlign = 'center';
            description.style.color = '#6c757d';
            placeholderDiv.appendChild(description);

            // Clear container and add fallback
            container.innerHTML = '';
            container.appendChild(placeholderDiv);

            // Animate entrance
            setTimeout(() => {
                placeholderDiv.style.opacity = '1';
                placeholderDiv.style.transform = 'scale(1)';
                placeholderDiv.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
            }, 50);

            // Store chart instance
            chartInstances[id] = {
                type: 'map',
                element: placeholderDiv
            };
        }
    }

    /**
     * Renders a presentation slide
     */
    function renderPresentationSlide(slideIndex, dataJson) {
        const data = JSON.parse(dataJson);
        const container = document.getElementById(`presentation-viz-${slideIndex}`);

        if (!container) return;

        container.innerHTML = '';

        // Create chart container element
        const chartContainer = document.createElement('div');
        chartContainer.id = `presentation-chart-${slideIndex}`;
        chartContainer.style.width = '100%';
        chartContainer.style.height = '100%';
        container.appendChild(chartContainer);

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
     * Animates the initial elements when the component loads
     */
    function animateInitialElements() {
        try {
            // Use D3 for more powerful animations

            // Animate header
            d3.select('.story-header')
                .style('opacity', 0)
                .style('transform', 'translateY(-20px)')
                .transition()
                .duration(500)
                .style('opacity', 1)
                .style('transform', 'translateY(0)');

            // Animate each section with staggered delay
            const sections = ['.palette', '.canvas', '.insights'];
            sections.forEach((selector, index) => {
                d3.select(selector)
                    .style('opacity', 0)
                    .style('transform', 'translateY(20px)')
                    .transition()
                    .delay(100 * (index + 1))
                    .duration(600)
                    .style('opacity', 1)
                    .style('transform', 'translateY(0)');
            });

            // Animate viz items with staggered entrance
            d3.selectAll('.viz-item')
                .style('opacity', 0)
                .style('transform', 'translateY(10px)')
                .each(function (d, i) {
                    d3.select(this)
                        .transition()
                        .delay(50 * (i + 1))
                        .duration(500)
                        .style('opacity', 1)
                        .style('transform', 'translateY(0)');
                });

            // Animate scene tabs
            d3.selectAll('.scene-tab')
                .style('opacity', 0)
                .style('transform', 'translateY(-10px)')
                .each(function (d, i) {
                    d3.select(this)
                        .transition()
                        .delay(70 * (i + 1))
                        .duration(400)
                        .style('opacity', 1)
                        .style('transform', 'translateY(0)');
                });

            // Animate dropzones
            d3.selectAll('.dropzone')
                .style('opacity', 0)
                .each(function (d, i) {
                    d3.select(this)
                        .transition()
                        .delay(100 * (i + 1))
                        .duration(400)
                        .style('opacity', 1);
                });

            // Animate insights with staggered entrances and subtle bounce
            d3.selectAll('.insight-card')
                .style('opacity', 0)
                .style('transform', 'translateY(10px)')
                .each(function (d, i) {
                    d3.select(this)
                        .transition()
                        .delay(100 * (i + 1))
                        .duration(500)
                        .style('opacity', 1)
                        .style('transform', 'translateY(0)')
                        .on("end", function () {
                            // Add subtle bounce
                            d3.select(this)
                                .transition()
                                .duration(200)
                                .style('transform', 'translateY(-3px)')
                                .transition()
                                .duration(200)
                                .style('transform', 'translateY(0)');
                        });
                });
        } catch (e) {
            console.log('D3 animation error, falling back to CSS transitions', e);

            // Fallback to CSS animations
            // Animate header
            const header = document.querySelector('.story-header');
            if (header) {
                header.style.opacity = '0';
                header.style.transform = 'translateY(-20px)';
                header.style.transition = 'opacity 0.5s ease, transform 0.5s ease';

                setTimeout(() => {
                    header.style.opacity = '1';
                    header.style.transform = 'translateY(0)';
                }, 50);
            }

            // Animate each section with staggered delay
            const sections = ['.palette', '.canvas', '.insights'];
            sections.forEach((selector, index) => {
                const element = document.querySelector(selector);
                if (element) {
                    element.style.opacity = '0';
                    element.style.transform = 'translateY(20px)';
                    element.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
                    element.style.transitionDelay = `${0.1 * (index + 1)}s`;

                    setTimeout(() => {
                        element.style.opacity = '1';
                        element.style.transform = 'translateY(0)';
                    }, 50);
                }
            });

            // Animate other elements
            const animateElements = (selector, delay, yOffset) => {
                const elements = document.querySelectorAll(selector);
                elements.forEach((item, index) => {
                    item.style.opacity = '0';
                    item.style.transform = `translateY(${yOffset}px)`;
                    item.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
                    item.style.transitionDelay = `${delay * (index + 1)}s`;

                    setTimeout(() => {
                        item.style.opacity = '1';
                        item.style.transform = 'translateY(0)';
                    }, 50);
                });
            };

            animateElements('.viz-item', 0.05, 10);
            animateElements('.scene-tab', 0.07, -10);
            animateElements('.dropzone', 0.1, 0);
            animateElements('.insight-card', 0.1, 10);
        }
    }

    /**
     * Animates a newly created visualization
     */
    function animateNewVisualization(id) {
        try {
            const element = document.getElementById(id);
            if (element) {
                d3.select(element)
                    .style('opacity', 0)
                    .style('transform', 'translateY(20px)')
                    .transition()
                    .duration(500)
                    .style('opacity', 1)
                    .style('transform', 'translateY(0)')
                    .transition()
                    .delay(500)
                    .duration(200)
                    .style('transform', 'translateY(-3px)')
                    .transition()
                    .duration(200)
                    .style('transform', 'translateY(0)');
            }
        } catch (e) {
            console.log('D3 animation error, falling back to CSS transitions', e);

            const element = document.getElementById(id);
            if (element) {
                element.style.opacity = '0';
                element.style.transform = 'translateY(20px)';
                element.style.transition = 'opacity 0.5s ease, transform 0.5s ease';

                setTimeout(() => {
                    element.style.opacity = '1';
                    element.style.transform = 'translateY(0)';

                    // Add simple bounce effect
                    setTimeout(() => {
                        element.style.transform = 'translateY(-3px)';
                        setTimeout(() => {
                            element.style.transform = 'translateY(0)';
                        }, 200);
                    }, 500);
                }, 50);
            }
        }
    }

    /**
     * Animates removing a visualization
     */
    function animateRemoveVisualization(id) {
        try {
            const element = document.getElementById(id);
            if (element) {
                d3.select(element)
                    .transition()
                    .duration(300)
                    .style('opacity', 0)
                    .style('transform', 'scale(0.95)')
                    .remove();
            }
        } catch (e) {
            console.log('D3 animation error, falling back to CSS transitions', e);

            const element = document.getElementById(id);
            if (element) {
                element.style.opacity = '0';
                element.style.transform = 'scale(0.95)';
                element.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
            }
        }
    }

    /**
     * Animates a pulsing effect on an insight
     */
    function pulseInsight(index) {
        try {
            const insights = document.querySelectorAll('.insight-card');
            if (index >= 0 && index < insights.length) {
                const insight = insights[index];

                d3.select(insight)
                    .transition()
                    .duration(500)
                    .style('transform', 'scale(1.05)')
                    .style('box-shadow', '0 8px 16px rgba(0,0,0,0.1)')
                    .transition()
                    .duration(500)
                    .style('transform', 'scale(1)')
                    .style('box-shadow', '0 2px 4px rgba(0,0,0,0.05)')
                    .transition()
                    .duration(500)
                    .style('transform', 'scale(1.05)')
                    .style('box-shadow', '0 8px 16px rgba(0,0,0,0.1)')
                    .transition()
                    .duration(500)
                    .style('transform', 'scale(1)')
                    .style('box-shadow', '0 2px 4px rgba(0,0,0,0.05)');
            }
        } catch (e) {
            console.log('D3 animation error, falling back to CSS transitions', e);

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
    }

    /**
     * Animates a new insight being added
     */
    function animateNewInsight(index) {
        try {
            const insights = document.querySelectorAll('.insight-card');
            if (index >= 0 && index < insights.length) {
                const insight = insights[index];

                d3.select(insight)
                    .style('opacity', 0)
                    .style('transform', 'translateX(-20px)')
                    .transition()
                    .duration(500)
                    .style('opacity', 1)
                    .style('transform', 'translateX(0)')
                    .transition()
                    .delay(500)
                    .duration(200)
                    .style('transform', 'translateX(-5px)')
                    .transition()
                    .duration(200)
                    .style('transform', 'translateX(0)');
            }
        } catch (e) {
            console.log('D3 animation error, falling back to CSS transitions', e);

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
    }

    /* PRESENTATION MODE */

    /**
     * Starts presentation mode
     */
    function startPresentationMode() {
        try {
            document.body.style.overflow = 'hidden';

            // Add entrance animation with D3
            d3.select('.presentation-mode')
                .style('opacity', 0)
                .style('visibility', 'visible')
                .transition()
                .duration(800)
                .style('opacity', 1);

            // Find active slide
            const activeSlide = document.querySelector('.presentation-slide.active');
            if (activeSlide) {
                d3.select(activeSlide)
                    .style('opacity', 0)
                    .style('transform', 'translateY(50px) scale(0.95)')
                    .style('visibility', 'visible')
                    .transition()
                    .delay(400)
                    .duration(800)
                    .style('opacity', 1)
                    .style('transform', 'translateY(0) scale(1)');
            }
        } catch (e) {
            console.log('D3 animation error, falling back to CSS transitions', e);

            document.body.style.overflow = 'hidden';

            // Find active slide
            const activeSlide = document.querySelector('.presentation-slide.active');
            if (activeSlide) {
                activeSlide.style.opacity = '0';
                activeSlide.style.transform = 'scale(0.95)';
                activeSlide.style.transition = 'opacity 0.8s ease, transform 0.8s ease';

                setTimeout(() => {
                    activeSlide.style.opacity = '1';
                    activeSlide.style.transform = 'scale(1)';
                }, 400);
            }
        }
    }

    /**
     * Ends presentation mode
     */
    function endPresentationMode() {
        try {
            // Add exit animation with D3
            d3.select('.presentation-mode')
                .transition()
                .duration(500)
                .style('opacity', 0)
                .on('end', function () {
                    d3.select(this).style('visibility', 'hidden');
                    document.body.style.overflow = '';
                });
        } catch (e) {
            console.log('D3 animation error, falling back to CSS transitions', e);

            const presentationMode = document.querySelector('.presentation-mode');
            if (presentationMode) {
                presentationMode.style.opacity = '0';
                presentationMode.style.transition = 'opacity 0.5s ease';

                setTimeout(() => {
                    presentationMode.style.visibility = 'hidden';
                    document.body.style.overflow = '';
                }, 500);
            }
        }
    }

    /**
     * Transitions to the next slide
     */
    function transitionToNextSlide() {
        try {
            const currentSlide = document.querySelector('.presentation-slide.active');
            const nextSlide = currentSlide?.nextElementSibling;

            if (currentSlide && nextSlide && nextSlide.classList.contains('presentation-slide')) {
                // Animate current slide out
                d3.select(currentSlide)
                    .transition()
                    .duration(500)
                    .style('opacity', 0)
                    .style('transform', 'translateX(-50px)')
                    .on('end', function () {
                        // Update classes
                        currentSlide.classList.remove('active');
                        currentSlide.classList.add('prev');
                        nextSlide.classList.add('active');

                        // Animate next slide in
                        d3.select(nextSlide)
                            .style('opacity', 0)
                            .style('transform', 'translateX(50px)')
                            .style('visibility', 'visible')
                            .transition()
                            .delay(100)
                            .duration(500)
                            .style('opacity', 1)
                            .style('transform', 'translateX(0)');
                    });
            }
        } catch (e) {
            console.log('D3 animation error, falling back to CSS transitions', e);

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
                    nextSlide.style.visibility = 'visible';

                    setTimeout(() => {
                        nextSlide.style.opacity = '1';
                        nextSlide.style.transform = 'translateX(0)';
                    }, 50);
                }, 500);
            }
        }
    }

    /**
     * Transitions to the previous slide
     */
    function transitionToPrevSlide() {
        try {
            const currentSlide = document.querySelector('.presentation-slide.active');
            const prevSlides = document.querySelectorAll('.presentation-slide.prev');
            const prevSlide = prevSlides[prevSlides.length - 1];

            if (currentSlide && prevSlide) {
                // Animate current slide out
                d3.select(currentSlide)
                    .transition()
                    .duration(500)
                    .style('opacity', 0)
                    .style('transform', 'translateX(50px)')
                    .on('end', function () {
                        // Update classes
                        currentSlide.classList.remove('active');
                        prevSlide.classList.remove('prev');
                        prevSlide.classList.add('active');

                        // Animate previous slide in
                        d3.select(prevSlide)
                            .style('opacity', 0)
                            .style('transform', 'translateX(-50px)')
                            .style('visibility', 'visible')
                            .transition()
                            .delay(100)
                            .duration(500)
                            .style('opacity', 1)
                            .style('transform', 'translateX(0)');
                    });
            }
        } catch (e) {
            console.log('D3 animation error, falling back to CSS transitions', e);

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
                    prevSlide.classList.remove('prev');
                    prevSlide.classList.add('active');

                    // Animate previous slide in
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
    }

    /**
     * Hides the loading overlay with animation
     */
    function hideLoadingOverlay() {
        try {
            const overlay = document.querySelector('.loading-overlay');
            if (overlay) {
                d3.select(overlay)
                    .transition()
                    .duration(500)
                    .style('opacity', 0)
                    .on('end', function () {
                        overlay.style.display = 'none';
                    });
            }
        } catch (e) {
            console.log('D3 animation error, falling back to CSS transitions', e);

            const overlay = document.querySelector('.loading-overlay');
            if (overlay) {
                overlay.style.opacity = '0';
                overlay.style.transition = 'opacity 0.5s ease';

                setTimeout(() => {
                    overlay.style.display = 'none';
                }, 500);
            }
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

        // Update all charts
        for (const id in chartInstances) {
            if (chartInstances.hasOwnProperty(id) && chartInstances[id].chart) {
                chartInstances[id].chart.updateOptions({
                    theme: {
                        mode: darkModeEnabled ? 'dark' : 'light'
                    }
                });
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