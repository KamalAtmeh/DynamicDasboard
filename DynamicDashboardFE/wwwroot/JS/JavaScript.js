// Initialize all charts on the dashboard
function initializeCharts() {
    initMonthlySalesChart();
    initQuarterlyPerformanceChart();
    initTopProductsChart();
    initCustomerAcquisitionChart();
    initSalesHeatmapChart();
    initMarketingRevenueChart();
}

// Monthly Sales by Region
function initMonthlySalesChart() {
    const ctx = document.getElementById('monthlySalesChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
            datasets: [
                {
                    label: 'North',
                    data: [65, 59, 80, 81, 56, 55],
                    backgroundColor: '#4e73df'
                },
                {
                    label: 'South',
                    data: [28, 48, 40, 19, 86, 27],
                    backgroundColor: '#1cc88a'
                },
                {
                    label: 'East',
                    data: [45, 25, 16, 36, 67, 18],
                    backgroundColor: '#36b9cc'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                }
            }
        }
    });
}

// Q1 vs Q2 Performance
function initQuarterlyPerformanceChart() {
    const ctx = document.getElementById('quarterlyPerformanceChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'radar',
        data: {
            labels: ['Revenue', 'Profit', 'Growth', 'Customer Satisfaction', 'Market Share'],
            datasets: [
                {
                    label: 'Q1',
                    data: [65, 59, 90, 81, 56],
                    backgroundColor: 'rgba(78, 115, 223, 0.2)',
                    borderColor: '#4e73df',
                    pointBackgroundColor: '#4e73df'
                },
                {
                    label: 'Q2',
                    data: [28, 48, 40, 19, 96],
                    backgroundColor: 'rgba(28, 200, 138, 0.2)',
                    borderColor: '#1cc88a',
                    pointBackgroundColor: '#1cc88a'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                r: {
                    angleLines: {
                        display: true
                    }
                }
            }
        }
    });
}

// Top Products by Revenue
function initTopProductsChart() {
    const ctx = document.getElementById('topProductsChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['Product A', 'Product B', 'Product C', 'Product D', 'Product E'],
            datasets: [{
                data: [35, 25, 20, 15, 5],
                backgroundColor: [
                    '#4e73df', '#1cc88a', '#36b9cc', '#f6c23e', '#e74a3b'
                ],
                hoverOffset: 4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                }
            }
        }
    });
}

// Customer Acquisition Trends
function initCustomerAcquisitionChart() {
    const ctx = document.getElementById('customerAcquisitionChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
            datasets: [{
                label: 'New Customers',
                data: [12, 19, 15, 20, 25, 30],
                borderColor: '#4e73df',
                backgroundColor: 'rgba(78, 115, 223, 0.1)',
                tension: 0.4,
                fill: true
            },
            {
                label: 'Returning Customers',
                data: [20, 25, 28, 30, 36, 42],
                borderColor: '#1cc88a',
                backgroundColor: 'rgba(28, 200, 138, 0.1)',
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                }
            },
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    });
}

// Sales Heatmap by Location (simplified version using Chart.js)
function initSalesHeatmapChart() {
    const ctx = document.getElementById('salesHeatmapChart');
    if (!ctx) return;

    // For a real heatmap, you would use a specialized library
    // This is a simplified representation using a bubble chart
    new Chart(ctx, {
        type: 'bubble',
        data: {
            datasets: [{
                label: 'Sales by Location',
                data: [
                    { x: 10, y: 20, r: 15 },
                    { x: 30, y: 40, r: 10 },
                    { x: 50, y: 30, r: 20 },
                    { x: 70, y: 50, r: 8 },
                    { x: 20, y: 60, r: 12 },
                    { x: 80, y: 20, r: 25 }
                ],
                backgroundColor: [
                    'rgba(78, 115, 223, 0.7)',
                    'rgba(28, 200, 138, 0.7)',
                    'rgba(54, 185, 204, 0.7)',
                    'rgba(246, 194, 62, 0.7)',
                    'rgba(231, 74, 59, 0.7)',
                    'rgba(133, 135, 150, 0.7)'
                ]
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    min: 0,
                    max: 100,
                    title: {
                        display: true,
                        text: 'Longitude'
                    }
                },
                y: {
                    min: 0,
                    max: 100,
                    title: {
                        display: true,
                        text: 'Latitude'
                    }
                }
            }
        }
    });
}

// Marketing-Revenue Relationship
function initMarketingRevenueChart() {
    const ctx = document.getElementById('marketingRevenueChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'scatter',
        data: {
            datasets: [{
                label: 'Marketing-Revenue Correlation',
                data: [
                    { x: 10, y: 20 },
                    { x: 15, y: 25 },
                    { x: 20, y: 30 },
                    { x: 25, y: 45 },
                    { x: 30, y: 50 },
                    { x: 35, y: 65 },
                    { x: 40, y: 70 },
                    { x: 45, y: 80 },
                    { x: 50, y: 90 }
                ],
                backgroundColor: 'rgba(78, 115, 223, 0.8)'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    title: {
                        display: true,
                        text: 'Marketing Spend ($K)'
                    }
                },
                y: {
                    title: {
                        display: true,
                        text: 'Revenue ($K)'
                    }
                }
            }
        }
    });
}

// Call this function when page loads
window.initDashboard = function () {
    setTimeout(initializeCharts, 500); // Wait for canvas elements to be rendered
};
