// File: DynamicDashboardFE/wwwroot/js/testDashboard.js

// Save file to client
window.saveAsFile = function (filename, bytesBase64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Render success rate donut chart
window.renderSuccessRateChart = function (success, failed) {
    // Check if the container exists
    const container = document.getElementById('successRateChart');
    if (!container) return;

    // Clear existing chart if any
    container.innerHTML = '';

    // Calculate percentages
    const total = success + failed;
    const successPercent = total > 0 ? Math.round((success / total) * 100) : 0;
    const failedPercent = total > 0 ? Math.round((failed / total) * 100) : 0;

    // Set up chart dimensions
    const size = 150;
    const thickness = 30;
    const radius = (size - thickness) / 2;
    const centerX = size / 2;
    const centerY = size / 2;

    // Create SVG
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('width', size);
    svg.setAttribute('height', size);
    svg.setAttribute('viewBox', `0 0 ${size} ${size}`);
    container.appendChild(svg);

    // Create background circle
    const backgroundCircle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
    backgroundCircle.setAttribute('cx', centerX);
    backgroundCircle.setAttribute('cy', centerY);
    backgroundCircle.setAttribute('r', radius);
    backgroundCircle.setAttribute('fill', 'none');
    backgroundCircle.setAttribute('stroke', '#e9ecef');
    backgroundCircle.setAttribute('stroke-width', thickness);
    svg.appendChild(backgroundCircle);

    // Create success arc
    if (successPercent > 0) {
        const successArc = createArc(centerX, centerY, radius, 0, (successPercent / 100) * 360, '#28a745', thickness);
        svg.appendChild(successArc);
    }

    // Create failed arc
    if (failedPercent > 0) {
        const failedArc = createArc(centerX, centerY, radius, (successPercent / 100) * 360, 360, '#dc3545', thickness);
        svg.appendChild(failedArc);
    }

    // Add center text
    const textGroup = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    svg.appendChild(textGroup);

    const percentText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
    percentText.setAttribute('x', centerX);
    percentText.setAttribute('y', centerY);
    percentText.setAttribute('text-anchor', 'middle');
    percentText.setAttribute('dominant-baseline', 'central');
    percentText.setAttribute('font-size', '24');
    percentText.setAttribute('font-weight', 'bold');
    percentText.setAttribute('fill', successPercent >= 70 ? '#28a745' : (successPercent >= 50 ? '#ffc107' : '#dc3545'));
    percentText.textContent = `${successPercent}%`;
    textGroup.appendChild(percentText);

    const labelText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
    labelText.setAttribute('x', centerX);
    labelText.setAttribute('y', centerY + 20);
    labelText.setAttribute('text-anchor', 'middle');
    labelText.setAttribute('dominant-baseline', 'central');
    labelText.setAttribute('font-size', '12');
    labelText.setAttribute('fill', '#6c757d');
    labelText.textContent = 'Success Rate';
    textGroup.appendChild(labelText);

    // Helper function to create arcs
    function createArc(cx, cy, radius, startAngle, endAngle, color, thickness) {
        // Convert angles to radians
        const startRad = (startAngle - 90) * (Math.PI / 180);
        const endRad = (endAngle - 90) * (Math.PI / 180);

        // Calculate start and end points
        const x1 = cx + radius * Math.cos(startRad);
        const y1 = cy + radius * Math.sin(startRad);
        const x2 = cx + radius * Math.cos(endRad);
        const y2 = cy + radius * Math.sin(endRad);

        // Create the arc path
        const largeArcFlag = endAngle - startAngle <= 180 ? '0' : '1';
        const path = [
            `M ${x1} ${y1}`,
            `A ${radius} ${radius} 0 ${largeArcFlag} 1 ${x2} ${y2}`
        ].join(' ');

        // Create the path element
        const arc = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        arc.setAttribute('d', path);
        arc.setAttribute('fill', 'none');
        arc.setAttribute('stroke', color);
        arc.setAttribute('stroke-width', thickness);
        arc.setAttribute('stroke-linecap', 'round');

        return arc;
    }
};