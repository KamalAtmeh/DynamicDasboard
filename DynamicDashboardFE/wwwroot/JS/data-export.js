// File: DynamicDashboardFE/wwwroot/js/data-export.js

/**
 * Helper to save data as a file for download
 */
function saveAsFile(filename, base64Data) {
    const link = document.createElement('a');
    link.href = `data:application/octet-stream;base64,${base64Data}`;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

/**
 * Creates a pulse effect on a table row
 */
function pulseTableRow(rowElement) {
    if (!rowElement) return;

    rowElement.classList.add('pulse-highlight');

    setTimeout(() => {
        rowElement.classList.remove('pulse-highlight');
    }, 1500);
}

/**
 * Flash highlight effect for cells
 */
function flashCellHighlight(cellElement, color) {
    if (!cellElement) return;

    const originalBackground = cellElement.style.backgroundColor;
    const originalTransition = cellElement.style.transition;

    cellElement.style.transition = 'background-color 0.5s ease';
    cellElement.style.backgroundColor = color || 'rgba(59, 130, 246, 0.2)';

    setTimeout(() => {
        cellElement.style.backgroundColor = originalBackground;

        setTimeout(() => {
            cellElement.style.transition = originalTransition;
        }, 500);
    }, 1000);
}

// Register functions globally
window.dataExport = {
    saveAsFile: saveAsFile,
    pulseTableRow: pulseTableRow,
    flashCellHighlight: flashCellHighlight
};