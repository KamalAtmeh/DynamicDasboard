// File: DynamicDashboardFE/wwwroot/js/testVisualizations.js

window.renderTestVisualizations = function (structureData, diffData, sunburstData) {
    // Render structure visualization
    renderStructureViz(structureData);

    // Render data diff visualization
    renderDataDiffViz(diffData);

    // Render sunburst chart
    renderSunburstViz(sunburstData);
};

function renderStructureViz(data) {
    const container = document.getElementById('structureViz');
    if (!container) return;

    // Clear container
    container.innerHTML = '';

    // Calculate metrics
    const expectedOnly = data.expectedColumns.filter(c =>
        !data.actualColumns.includes(c));

    const actualOnly = data.actualColumns.filter(c =>
        !data.expectedColumns.includes(c));

    const common = data.expectedColumns.filter(c =>
        data.actualColumns.includes(c));

    // Create visualization
    const vizEl = document.createElement('div');
    vizEl.className = 'structure-comparison';

    // Column comparison
    const columnsEl = document.createElement('div');
    columnsEl.className = 'column-comparison';

    // Common columns
    const commonEl = document.createElement('div');
    commonEl.className = 'column-section';
    commonEl.innerHTML = `
        <h4>Common Columns</h4>
        <div class="column-count">${common.length}</div>
        <div class="column-list">
            ${common.map(c => `<div class="column-tag common">${c}</div>`).join('')}
        </div>
    `;

    // Missing columns
    const missingEl = document.createElement('div');
    missingEl.className = 'column-section';
    missingEl.innerHTML = `
        <h4>Missing Columns</h4>
        <div class="column-count">${expectedOnly.length}</div>
        <div class="column-list">
            ${expectedOnly.map(c => `<div class="column-tag missing">${c}</div>`).join('')}
        </div>
    `;

    // Extra columns
    const extraEl = document.createElement('div');
    extraEl.className = 'column-section';
    extraEl.innerHTML = `
        <h4>Extra Columns</h4>
        <div class="column-count">${actualOnly.length}</div>
        <div class="column-list">
            ${actualOnly.map(c => `<div class="column-tag extra">${c}</div>`).join('')}
        </div>
    `;

    columnsEl.appendChild(commonEl);
    columnsEl.appendChild(missingEl);
    columnsEl.appendChild(extraEl);

    // Row comparison
    const rowsEl = document.createElement('div');
    rowsEl.className = 'row-comparison';

    const rowDiff = data.rowData.expected - data.rowData.actual;
    const rowStatus = rowDiff === 0 ? 'match' : (rowDiff > 0 ? 'missing' : 'extra');

    rowsEl.innerHTML = `
        <div class="row-section">
            <h4>Row Count</h4>
            <div class="row-stats ${rowStatus}">
                <div class="stat">
                    <span class="label">Expected</span>
                    <span class="value">${data.rowData.expected}</span>
                </div>
                <div class="stat">
                    <span class="label">Actual</span>
                    <span class="value">${data.rowData.actual}</span>
                </div>
                <div class="stat">
                    <span class="label">Difference</span>
                    <span class="value">${rowDiff > 0 ? '+' + rowDiff : rowDiff}</span>
                </div>
            </div>
        </div>
    `;

    vizEl.appendChild(columnsEl);
    vizEl.appendChild(rowsEl);

    container.appendChild(vizEl);

    // Add CSS styles
    const style = document.createElement('style');
    style.textContent = `
        .structure-comparison {
            display: flex;
            flex-direction: column;
            gap: 1.5rem;
            width: 100%;
        }
        
        .column-comparison {
            display: flex;
            justify-content: space-between;
            gap: 1rem;
        }
        
        .column-section, .row-section {
            flex: 1;
            padding: 1rem;
            background-color: #f8f9fa;
            border-radius: 0.5rem;
        }
        
        .column-section h4, .row-section h4 {
            margin: 0 0 0.75rem 0;
            font-size: 0.9rem;
            color: #495057;
        }
        
        .column-count {
            font-size: 1.5rem;
            font-weight: 600;
            margin-bottom: 0.75rem;
        }
        
        .column-list {
            display: flex;
            flex-wrap: wrap;
            gap: 0.5rem;
            max-height: 100px;
            overflow-y: auto;
        }
        
        .column-tag {
            padding: 0.25rem 0.5rem;
            border-radius: 0.25rem;
            font-size: 0.75rem;
        }
        
        .column-tag.common {
            background-color: #d4edda;
            color: #155724;
        }
        
        .column-tag.missing {
            background-color: #f8d7da;
            color: #721c24;
        }
        
        .column-tag.extra {
            background-color: #d1ecf1;
            color: #0c5460;
        }
        
        .row-stats {
            display: flex;
            justify-content: space-between;
        }
        
        .row-stats .stat {
            text-align: center;
        }
        
        .row-stats .label {
            display: block;
            margin-bottom: 0.25rem;
            font-size: 0.8rem;
        }
        
        .row-stats .value {
            font-size: 1.25rem;
            font-weight: 600;
        }
        
        .row-stats.match .value {
            color: #28a745;
        }
        
        .row-stats.missing .value {
            color: #dc3545;
        }
        
        .row-stats.extra .value {
            color: #17a2b8;
        }
    `;

    document.head.appendChild(style);
}

function renderDataDiffViz(data) {
    const container = document.getElementById('dataViz');
    if (!container) return;

    // Clear container
    container.innerHTML = '';

    // Calculate total
    const total = data.common + data.different + data.missing + data.extra;

    if (total === 0) {
        container.innerHTML = '<div class="no-data-message">No data available for comparison</div>';
        return;
    }

    // Calculate percentages
    const commonPct = Math.round((data.common / total) * 100);
    const differentPct = Math.round((data.different / total) * 100);
    const missingPct = Math.round((data.missing / total) * 100);
    const extraPct = Math.round((data.extra / total) * 100);

    // Create visualization
    const vizEl = document.createElement('div');
    vizEl.className = 'data-diff-viz';

    // Create bars
    vizEl.innerHTML = `
        <div class="diff-stats">
            <div class="stat-item">
                <div class="stat-label">Matching Values</div>
                <div class="stat-value">${data.common}</div>
                <div class="stat-percent">${commonPct}%</div>
            </div>
            <div class="stat-item">
                <div class="stat-label">Different Values</div>
                <div class="stat-value">${data.different}</div>
                <div class="stat-percent">${differentPct}%</div>
            </div>
            <div class="stat-item">
                <div class="stat-label">Missing Values</div>
                <div class="stat-value">${data.missing}</div>
                <div class="stat-percent">${missingPct}%</div>
            </div>
            <div class="stat-item">
                <div class="stat-label">Extra Values</div>
                <div class="stat-value">${data.extra}</div>
                <div class="stat-percent">${extraPct}%</div>
            </div>
        </div>
        
        <div class="diff-bar">
            <div class="diff-segment common" style="width: ${commonPct}%" title="Matching: ${commonPct}%"></div>
            <div class="diff-segment different" style="width: ${differentPct}%" title="Different: ${differentPct}%"></div>
            <div class="diff-segment missing" style="width: ${missingPct}%" title="Missing: ${missingPct}%"></div>
            <div class="diff-segment extra" style="width: ${extraPct}%" title="Extra: ${extraPct}%"></div>
        </div>
    `;

    container.appendChild(vizEl);

    // Add CSS styles
    const style = document.createElement('style');
    style.textContent = `
        .data-diff-viz {
            width: 100%;
            padding: 1rem;
        }
        
        .diff-stats {
            display: flex;
            justify-content: space-between;
            margin-bottom: 1.5rem;
        }
        
        .stat-item {
            text-align: center;
            padding: 0 0.5rem;
        }
        
        .stat-label {
            font-size: 0.8rem;
            color: #6c757d;
            margin-bottom: 0.25rem;
        }
        
        .stat-value {
            font-size: 1.25rem;
            font-weight: 600;
            margin-bottom: 0.25rem;
        }
        
        .stat-percent {
            font-size: 0.9rem;
            color: #495057;
        }
        
        .diff-bar {
            height: 30px;
            width: 100%;
            display: flex;
            border-radius: 4px;
            overflow: hidden;
        }
        
        .diff-segment {
            height: 100%;
            transition: width 0.5s ease;
        }
        
        .diff-segment.common {
            background-color: #28a745;
        }
        
        .diff-segment.different {
            background-color: #ffc107;
        }
        
        .diff-segment.missing {
            background-color: #dc3545;
        }
        
        .diff-segment.extra {
            background-color: #17a2b8;
        }
        
        .no-data-message {
            text-align: center;
            color: #6c757d;
            font-style: italic;
            padding: 2rem;
        }
    `;

    document.head.appendChild(style);
}

function renderSunburstViz(data) {
    const container = document.getElementById('sunburstViz');
    if (!container) return;

    // Clear container
    container.innerHTML = '';

    // Create visualization
    const vizEl = document.createElement('div');
    vizEl.className = 'score-gauges';

    // Create score gauges
    vizEl.innerHTML = `
        <div class="gauge-container">
            <div class="gauge-title">Overall Match Score</div>
            <div class="gauge-group">
                <div class="gauge-wrapper">
                    <div class="gauge-label">SQL</div>
                    <div class="gauge-outer">
                        <div class="gauge-inner ${getScoreClass(data.sqlMatch)}" style="width: ${data.sqlMatch * 100}%"></div>
                    </div>
                    <div class="gauge-value">${Math.round(data.sqlMatch * 100)}%</div>
                </div>
                
                <div class="gauge-wrapper">
                    <div class="gauge-label">Explanation</div>
                    <div class="gauge-outer">
                        <div class="gauge-inner ${getScoreClass(data.explanationMatch)}" style="width: ${data.explanationMatch * 100}%"></div>
                    </div>
                    <div class="gauge-value">${Math.round(data.explanationMatch * 100)}%</div>
                </div>
                
                <div class="gauge-wrapper">
                    <div class="gauge-label">Data</div>
                    <div class="gauge-outer">
                        <div class="gauge-inner ${getScoreClass(data.dataMatch)}" style="width: ${data.dataMatch * 100}%"></div>
                    </div>
                    <div class="gauge-value">${Math.round(data.dataMatch * 100)}%</div>
                </div>
            </div>
            
            <div class="overall-score ${getScoreClass((data.sqlMatch + data.explanationMatch + data.dataMatch) / 3)}">
                Overall: ${Math.round(((data.sqlMatch + data.explanationMatch + data.dataMatch) / 3) * 100)}%
            </div>
        </div>
    `;

    container.appendChild(vizEl);

    // Add CSS styles
    const style = document.createElement('style');
    style.textContent = `
        .score-gauges {
            width: 100%;
            padding: 1rem;
            display: flex;
            justify-content: center;
        }
        
        .gauge-container {
            width: 100%;
            max-width: 600px;
        }
        
        .gauge-title {
            text-align: center;
            font-size: 1.1rem;
            margin-bottom: 1.5rem;
            color: #343a40;
        }
        
        .gauge-group {
            display: flex;
            flex-direction: column;
            gap: 1.5rem;
        }
        
        .gauge-wrapper {
            display: flex;
            align-items: center;
            gap: 1rem;
        }
        
        .gauge-label {
            width: 100px;
            text-align: right;
            font-weight: 500;
            color: #495057;
        }
        
        .gauge-outer {
            flex: 1;
            height: 12px;
            background-color: #e9ecef;
            border-radius: 6px;
            overflow: hidden;
        }
        
        .gauge-inner {
            height: 100%;
            border-radius: 6px;
            transition: width 1s ease;
        }
        
        .gauge-inner.excellent {
            background-color: #28a745;
        }
        
        .gauge-inner.good {
            background-color: #17a2b8;
        }
        
        .gauge-inner.fair {
            background-color: #ffc107;
        }
        
        .gauge-inner.poor {
            background-color: #dc3545;
        }
        
        .gauge-value {
            width: 60px;
            text-align: left;
            font-weight: 600;
            color: #343a40;
        }
        
        .overall-score {
            margin-top: 2rem;
            text-align: center;
            font-size: 1.2rem;
            font-weight: 700;
            padding: 0.5rem;
            border-radius: 0.5rem;
        }
        
        .overall-score.excellent {
            background-color: #d4edda;
            color: #155724;
        }
        
        .overall-score.good {
            background-color: #d1ecf1;
            color: #0c5460;
        }
        
        .overall-score.fair {
            background-color: #fff3cd;
            color: #856404;
        }
        
        .overall-score.poor {
            background-color: #f8d7da;
            color: #721c24;
        }
    `;

    document.head.appendChild(style);
}

function getScoreClass(score) {
    if (score >= 0.9) return 'excellent';
    if (score >= 0.7) return 'good';
    if (score >= 0.5) return 'fair';
    return 'poor';
}