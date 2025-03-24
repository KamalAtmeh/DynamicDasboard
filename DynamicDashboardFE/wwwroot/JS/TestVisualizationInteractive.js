// File: DynamicDashboardFE/wwwroot/js/testVisualizations.js

/**
 * Test Visualization and Comparison Helper Functions
 * Provides interactive data visualizations for test case comparisons
 */

// Initialize visualization module
(function () {
    // Verify required dependencies
    if (typeof ApexCharts === 'undefined') {
        console.error('ApexCharts library is required for test visualizations');
        return;
    }

    if (typeof d3 === 'undefined') {
        console.error('D3.js library is required for test visualizations');
        return;
    }

    console.log('Test visualization module loaded successfully');
})();

/**
 * Renders test visualizations based on the provided data
 * @param {Object} structureData - Column and row comparison data
 * @param {Object} diffData - Data difference metrics
 * @param {Object} sunburstData - Match scores for sunburst chart
 */
function renderTestVisualizations(structureData, diffData, sunburstData) {
    console.log('Rendering test visualizations with data:', {
        structure: structureData,
        diff: diffData,
        sunburst: sunburstData
    });

    // Structure visualization (columns and rows)
    renderStructureVisualization(structureData);

    // Data differences visualization
    renderDiffVisualization(diffData);

    // Sunburst visualization for match scores
    renderSunburstVisualization(sunburstData);
}

/**
 * Renders a visualization showing the structure comparison (columns and rows)
 * @param {Object} data - Structure comparison data
 */
function renderStructureVisualization(data) {
    if (!data) return;

    const container = document.getElementById('structureViz');
    if (!container) return;

    // Clear previous visualization
    container.innerHTML = '';

    // Calculate column overlap
    const expectedColumns = data.expectedColumns || [];
    const actualColumns = data.actualColumns || [];

    const commonColumns = expectedColumns.filter(col =>
        actualColumns.some(actCol => actCol.toLowerCase() === col.toLowerCase())
    );

    const onlyInExpected = expectedColumns.filter(col =>
        !actualColumns.some(actCol => actCol.toLowerCase() === col.toLowerCase())
    );

    const onlyInActual = actualColumns.filter(col =>
        !expectedColumns.some(expCol => expCol.toLowerCase() === col.toLowerCase())
    );

    // Column comparison chart
    const columnOptions = {
        series: [{
            name: 'Columns',
            data: [
                commonColumns.length,
                onlyInExpected.length,
                onlyInActual.length
            ]
        }],
        chart: {
            type: 'bar',
            height: 200,
            toolbar: {
                show: false
            }
        },
        plotOptions: {
            bar: {
                horizontal: false,
                columnWidth: '55%',
                borderRadius: 4
            },
        },
        dataLabels: {
            enabled: true
        },
        colors: ['#28a745', '#dc3545', '#007bff'],
        xaxis: {
            categories: ['Common', 'Only in Expected', 'Only in Actual'],
        },
        title: {
            text: 'Column Comparison',
            align: 'center',
            style: {
                fontSize: '14px'
            }
        }
    };

    // Row comparison chart
    const rowData = data.rowData || { expected: 0, actual: 0 };
    const rowOptions = {
        series: [{
            name: 'Rows',
            data: [rowData.expected, rowData.actual]
        }],
        chart: {
            type: 'bar',
            height: 200,
            toolbar: {
                show: false
            }
        },
        plotOptions: {
            bar: {
                horizontal: false,
                columnWidth: '55%',
                borderRadius: 4
            },
        },
        dataLabels: {
            enabled: true
        },
        colors: ['#28a745', '#007bff'],
        xaxis: {
            categories: ['Expected', 'Actual'],
        },
        title: {
            text: 'Row Count Comparison',
            align: 'center',
            style: {
                fontSize: '14px'
            }
        }
    };

    // Create charts container
    const chartsContainer = document.createElement('div');
    chartsContainer.style.display = 'flex';
    chartsContainer.style.flexWrap = 'wrap';
    chartsContainer.style.justifyContent = 'space-around';
    container.appendChild(chartsContainer);

    // Column chart
    const columnChartDiv = document.createElement('div');
    columnChartDiv.style.width = '48%';
    columnChartDiv.style.minWidth = '300px';
    chartsContainer.appendChild(columnChartDiv);
    new ApexCharts(columnChartDiv, columnOptions).render();

    // Row chart
    const rowChartDiv = document.createElement('div');
    rowChartDiv.style.width = '48%';
    rowChartDiv.style.minWidth = '300px';
    chartsContainer.appendChild(rowChartDiv);
    new ApexCharts(rowChartDiv, rowOptions).render();
}

/**
 * Renders a visualization showing data differences
 * @param {Object} diffData - Data difference metrics
 */
function renderDiffVisualization(diffData) {
    if (!diffData) return;

    const container = document.getElementById('dataViz');
    if (!container) return;

    // Clear previous visualization
    container.innerHTML = '';

    // Pie chart for data differences
    const options = {
        series: [
            diffData.common || 0,
            diffData.different || 0,
            diffData.missing || 0,
            diffData.extra || 0
        ],
        chart: {
            width: 380,
            type: 'pie',
            toolbar: {
                show: false
            }
        },
        labels: ['Matching Values', 'Different Values', 'Missing Values', 'Extra Values'],
        colors: ['#28a745', '#ffc107', '#dc3545', '#17a2b8'],
        title: {
            text: 'Data Value Comparison',
            align: 'center',
            style: {
                fontSize: '16px'
            }
        },
        legend: {
            position: 'bottom'
        },
        responsive: [{
            breakpoint: 480,
            options: {
                chart: {
                    width: 300
                },
                legend: {
                    position: 'bottom'
                }
            }
        }]
    };

    // Create and render chart
    const chart = new ApexCharts(container, options);
    chart.render();

    // Add percentage text
    const total = (diffData.common || 0) + (diffData.different || 0) +
        (diffData.missing || 0) + (diffData.extra || 0);

    if (total > 0) {
        const percentageDiv = document.createElement('div');
        percentageDiv.style.textAlign = 'center';
        percentageDiv.style.marginTop = '1rem';
        percentageDiv.style.fontWeight = 'bold';

        const matchPercentage = Math.round((diffData.common || 0) * 100 / total);
        const percentageClass = matchPercentage >= 90 ? 'text-success' :
            matchPercentage >= 70 ? 'text-primary' :
                matchPercentage >= 50 ? 'text-warning' : 'text-danger';

        percentageDiv.innerHTML = `<span class="${percentageClass}">${matchPercentage}% Match</span>`;
        container.appendChild(percentageDiv);
    }
}

/**
 * Renders a sunburst visualization showing match scores
 * @param {Object} data - Match score data
 */
function renderSunburstVisualization(data) {
    if (!data) return;

    const container = document.getElementById('sunburstViz');
    if (!container) return;

    // Clear previous visualization
    container.innerHTML = '';

    // Format values as percentages
    const sqlMatch = Math.round((data.sqlMatch || 0) * 100);
    const explanationMatch = Math.round((data.explanationMatch || 0) * 100);
    const dataMatch = Math.round((data.dataMatch || 0) * 100);

    // Calculate overall match (weighted average)
    const overallMatch = Math.round(
        (sqlMatch * 0.4) + (explanationMatch * 0.2) + (dataMatch * 0.4)
    );

    // Create radial bar chart for all metrics
    const options = {
        series: [sqlMatch, explanationMatch, dataMatch, overallMatch],
        chart: {
            height: 350,
            type: 'radialBar',
        },
        plotOptions: {
            radialBar: {
                dataLabels: {
                    name: {
                        fontSize: '16px',
                    },
                    value: {
                        fontSize: '16px',
                        formatter: function (val) {
                            return val + '%';
                        }
                    },
                    total: {
                        show: true,
                        label: 'Overall',
                        formatter: function () {
                            return overallMatch + '%';
                        }
                    }
                },
                track: {
                    background: '#f2f2f2',
                }
            }
        },
        labels: ['SQL Match', 'Explanation', 'Data Match', 'Overall'],
        colors: ['#007bff', '#17a2b8', '#28a745', '#6f42c1'],
        title: {
            text: 'Match Score Summary',
            align: 'center',
            style: {
                fontSize: '16px'
            }
        }
    };

    const chart = new ApexCharts(container, options);
    chart.render();
}

/**
 * Renders a success rate donut chart on the results page
 * @param {number} successCount - Number of successful tests
 * @param {number} failedCount - Number of failed tests
 */
function renderSuccessRateChart(successCount, failedCount) {
    const container = document.getElementById('successRateChart');
    if (!container) return;

    // Clear previous chart
    container.innerHTML = '';

    const total = successCount + failedCount;
    const successRate = total > 0 ? Math.round((successCount / total) * 100) : 0;

    const options = {
        series: [successRate],
        chart: {
            height: 200,
            type: 'radialBar',
            toolbar: {
                show: false
            }
        },
        plotOptions: {
            radialBar: {
                startAngle: -135,
                endAngle: 135,
                hollow: {
                    margin: 0,
                    size: '70%',
                },
                track: {
                    background: '#f7f7f7',
                    strokeWidth: '67%',
                    margin: 0,
                },
                dataLabels: {
                    name: {
                        show: true,
                        color: '#888',
                        fontSize: '13px',
                        offsetY: -10
                    },
                    value: {
                        color: '#111',
                        fontSize: '30px',
                        show: true,
                        formatter: function (val) {
                            return val + '%';
                        }
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
                gradientToColors: ['#28a745'],
                inverseColors: true,
                opacityFrom: 1,
                opacityTo: 1,
                stops: [0, 100]
            }
        },
        stroke: {
            lineCap: 'round'
        },
        labels: ['Success Rate'],
        colors: ['#007bff'],
        title: {
            text: `${successCount} of ${total} tests passed`,
            align: 'center',
            style: {
                fontSize: '14px',
                color: '#555'
            }
        }
    };

    const chart = new ApexCharts(container, options);
    chart.render();
}

/**
 * Helper function to copy text to clipboard
 * @param {string} text - Text to copy
 * @returns {Promise<void>}
 */
async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        console.log('Text copied to clipboard');
        return true;
    } catch (err) {
        console.error('Failed to copy: ', err);
        return false;
    }
}

/**
 * Saves a file to the user's device
 * @param {string} filename - Name of the file to save
 * @param {string} base64Data - Base64-encoded file data
 */
function saveAsFile(filename, base64Data) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = `data:application/octet-stream;base64,${base64Data}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}