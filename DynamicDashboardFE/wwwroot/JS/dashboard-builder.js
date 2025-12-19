/* ============================================================ */
/* DASHBOARD BUILDER - DRAG & DROP + INTERACTIONS               */
/* ============================================================ */

window.DashboardBuilder = {
    // ==================== STATE ==================== draggedElement: null, draggedData: null, dropZones: [], gridSize:

{
    cols: 12, rowHeight: 80
}

,
// ==================== INITIALIZATION ====================
init: function() {
    console .log('Dashboard Builder initialized');
    this .setupGlobalListeners();
}

,
setupGlobalListeners: function() {
    // Prevent default drag behaviors on document document.addEventListener('dragover', (e) => {
            if (this.draggedData) {
                e.preventDefault();
            }
        });
    document .addEventListener('drop', (e) => {
            e.preventDefault();
        });
    // Handle escape key to cancel drag document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.draggedData) {
                this.cancelDrag();
            }
        });
}

,
// ==================== DRAG START ====================
startDrag: function(element, componentData) {
    this .draggedElement = element;
    this .draggedData = componentData;
    // Add dragging class element.classList.add('is-dragging');
    document .body.classList.add('is-dragging-active');
    // Create ghost element this.createDragGhost(element, componentData);
    console .log('Started dragging:', componentData.name);
    return true;
}

,
startDragExisting: function(element, componentId, gridWidth, gridHeight) {
    this .draggedElement = element;
    this .draggedData =

{
    type: 'existing', componentId: componentId, gridWidth: gridWidth, gridHeight: gridHeight
}

;

element.classList.add('is-dragging');
document.body.classList.add('is-dragging-active');

console.log('Started dragging existing component:', componentId);
return true;
}

,
// ==================== DRAG GHOST ====================
createDragGhost: function(element, data) {
    // Remove existing ghost this.removeDragGhost();
    const ghost = document.createElement('div');
    ghost .id = 'drag-ghost';
    ghost .className = 'db-drag-ghost';
    ghost .innerHTML = ` <div class="db-ghost-icon" style="background: ${data.color || '#3B82F6'}"> <i class="${data.icon || 'fas fa-cube'}"></i> </div> <span>$

{
    data .name || 'Component'
}

</span >
`;

document.body.appendChild(ghost);

// Follow mouse
document.addEventListener('mousemove', this.moveDragGhost);
document.addEventListener('dragover', this.moveDragGhost);
}

,
moveDragGhost: function(e) {
    const ghost = document.getElementById('drag-ghost');
    if (ghost)

{
    ghost .style.left = (e.clientX + 15) + 'px';
    ghost .style.top = (e.clientY + 15) + 'px';
}

}

,
removeDragGhost: function() {
    const ghost = document.getElementById('drag-ghost');
    if (ghost)

{
    ghost .remove();
}

document.removeEventListener('mousemove', this.moveDragGhost);
document.removeEventListener('dragover', this.moveDragGhost);
}

,
// ==================== DRAG END ====================
endDrag: function() {
    if (this.draggedElement)

{
    this .draggedElement.classList.remove('is-dragging');
}

document.body.classList.remove('is-dragging-active');
this.removeDragGhost();
this.removeDropIndicator();

this.draggedElement = null;
this.draggedData = null;

console.log('Drag ended');
}

,
cancelDrag: function() {
    this .endDrag();
    console .log('Drag cancelled');
}

,
// ==================== DROP ZONE ====================
setupDropZone: function(canvasElement) {
    if (!canvasElement) return;
    canvasElement .addEventListener('dragenter', (e) => {
            e.preventDefault();
            canvasElement.classList.add('drag-over');
        });
    canvasElement .addEventListener('dragleave', (e) => {
            if (!canvasElement.contains(e.relatedTarget)) {
                canvasElement.classList.remove('drag-over');
            }
        });
    canvasElement .addEventListener('dragover', (e) => {
            e.preventDefault();
            this.updateDropIndicator(canvasElement, e);
        });
    console .log('Drop zone configured');
}

,
// ==================== DROP INDICATOR ====================
updateDropIndicator: function(canvas, event) {
    const rect = canvas.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    // Calculate grid position const colWidth = rect.width / this.gridSize.cols;
    const gridX = Math.floor(x / colWidth);
    const gridY = Math.floor(y / this.gridSize.rowHeight);
    // Get dragged component size const width = this.draggedData?.gridWidth || this.draggedData?.defaultWidth || 4;
    const height = this.draggedData?.gridHeight || this.draggedData?.defaultHeight || 2;
    // Clamp to grid bounds const clampedX = Math.max(0, Math.min(gridX, this.gridSize.cols - width));
    const clampedY = Math.max(0, gridY);
    this .showDropIndicator(canvas, clampedX, clampedY, width, height);
    return

{
    gridX: clampedX, gridY: clampedY
}

;
}

,
showDropIndicator: function(canvas, gridX, gridY, width, height) {
    let indicator = document.getElementById('drop-indicator');
    if (!indicator)

{
    indicator = document.createElement('div');
    indicator .id = 'drop-indicator';
    indicator .className = 'db-drop-indicator-js';
    indicator .innerHTML = '<div class="db-drop-indicator-inner"><i class="fas fa-plus"></i></div>';
    canvas .appendChild(indicator);
}

const colWidth = canvas.offsetWidth / this.gridSize.cols;

indicator.style.left = (gridX * colWidth) + 'px';
indicator.style.top = (gridY * this.gridSize.rowHeight) + 'px';
indicator.style.width = (width * colWidth) + 'px';
indicator.style.height = (height * this.gridSize.rowHeight) + 'px';
indicator.style.display = 'flex';
}

,
removeDropIndicator: function() {
    const indicator = document.getElementById('drop-indicator');
    if (indicator)

{
    indicator .style.display = 'none';
}

}

,
// ==================== CALCULATE DROP POSITION ====================
calculateDropPosition: function(canvasElement, event) {
    if (!canvasElement) return

{
    gridX: 0, gridY: 0
}

;

const rect = canvasElement.getBoundingClientRect();
const x = event.clientX - rect.left;
const y = event.clientY - rect.top;

const colWidth = rect.width / this.gridSize.cols;
const gridX = Math.floor(x / colWidth);
const gridY = Math.floor(y / this.gridSize.rowHeight);

const width = this.draggedData?.gridWidth || this.draggedData?.defaultWidth || 4;

return {
    gridX: Math.max(0, Math.min(gridX, this.gridSize.cols - width)), gridY: Math.max(0, gridY)
}

;
}

,
// ==================== SORTABLE GRID ITEMS ====================
makeGridItemsSortable: function(containerSelector) {
    const container = document.querySelector(containerSelector);
    if (!container) return;
    const items = container.querySelectorAll('.db-grid-item');
    items .forEach(item => {
            const header = item.querySelector('.db-item-header');
            if (header) {
                header.style.cursor = 'grab';
                
                header.addEventListener('mousedown', () => {
                    header.style.cursor = 'grabbing';
                });
                
                header.addEventListener('mouseup', () => {
                    header.style.cursor = 'grab';
                });
            }
        });
}

,
// ==================== RESIZE FUNCTIONALITY ====================
initResize: function(element, componentId, direction, dotNetHelper) {
    const item = element.closest('.db-grid-item');
    if (!item) return;
    const canvas = item.closest('.db-canvas');
    if (!canvas) return;
    const startRect = item.getBoundingClientRect();
    const canvasRect = canvas.getBoundingClientRect();
    const colWidth = canvasRect.width / this.gridSize.cols;
    const initialWidth = item.offsetWidth;
    const initialHeight = item.offsetHeight;
    const onMouseMove = (e) =>

{
    let newWidth = initialWidth;
    let newHeight = initialHeight;
    if (direction.includes('e'))

{
    newWidth = e.clientX - startRect.left;
}

if (direction.includes('s')) {
    newHeight = e.clientY - startRect.top;
}

// Snap to grid
const newGridWidth = Math.max(1, Math.min(12, Math.round(newWidth / colWidth)));
const newGridHeight = Math.max(1, Math.round(newHeight / this.gridSize.rowHeight));

// Visual feedback
item.style.gridColumn = `span $ {
    newGridWidth
}

`;
item.style.gridRow = `span $ {
    newGridHeight
}

`;
}

;

const onMouseUp = (e) = > {
    document .removeEventListener('mousemove', onMouseMove);
    document .removeEventListener('mouseup', onMouseUp);
    const finalRect = item.getBoundingClientRect();
    const newGridWidth = Math.max(1, Math.min(12, Math.round(finalRect.width / colWidth)));
    const newGridHeight = Math.max(1, Math.round(finalRect.height / this.gridSize.rowHeight));
    // Notify Blazor if (dotNetHelper)

{
    dotNetHelper .invokeMethodAsync('OnComponentResized', componentId, newGridWidth, newGridHeight);
}

item.style.gridColumn = '';
item.style.gridRow = '';
}
;

document.addEventListener('mousemove', onMouseMove);
document.addEventListener('mouseup', onMouseUp);
}

,
// ==================== UTILITY FUNCTIONS ====================
getDraggedData: function() {
    return this.draggedData;
}

,
setGridSize: function(cols, rowHeight) {
    this .gridSize.cols = cols;
    this .gridSize.rowHeight = rowHeight;
}

,
// Focus title input
focusElement: function(element) {
    if (element)

{
    element .focus();
    element .select();
}

}

,
// Copy to clipboard
copyToClipboard: async function(text) {
    try

{
    await navigator.clipboard.writeText(text);
    return true;
}

catch (err) {
    console .error('Failed to copy:', err);
    return false;
}

}

,
// Download JSON
downloadJson: function(data, filename) {
    const json = JSON.stringify(data, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a .href = url;
    a .download = filename || 'dashboard.json';
    document .body.appendChild(a);
    a .click();
    document .body.removeChild(a);
    URL .revokeObjectURL(url);
}

,
// Keyboard shortcuts
setupKeyboardShortcuts: function(dotNetHelper) {
    document .addEventListener('keydown', (e) => {
            // Only handle shortcuts when not in input
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
                return;
            }
            
            // Ctrl/Cmd + S = Save
            if ((e.ctrlKey || e.metaKey) && e.key === 's') {
                e.preventDefault();
                dotNetHelper.invokeMethodAsync('OnKeyboardSave');
            }
            
            // Ctrl/Cmd + Z = Undo
            if ((e.ctrlKey || e.metaKey) && e.key === 'z' && !e.shiftKey) {
                e.preventDefault();
                dotNetHelper.invokeMethodAsync('OnKeyboardUndo');
            }
            
            // Ctrl/Cmd + Shift + Z or Ctrl + Y = Redo
            if ((e.ctrlKey || e.metaKey) && (e.key === 'y' || (e.key === 'z' && e.shiftKey))) {
                e.preventDefault();
                dotNetHelper.invokeMethodAsync('OnKeyboardRedo');
            }
            
            // Delete = Delete selected
            if (e.key === 'Delete' || e.key === 'Backspace') {
                dotNetHelper.invokeMethodAsync('OnKeyboardDelete');
            }
            
            // Ctrl/Cmd + D = Duplicate
            if ((e.ctrlKey || e.metaKey) && e.key === 'd') {
                e.preventDefault();
                dotNetHelper.invokeMethodAsync('OnKeyboardDuplicate');
            }
        });
}

}

;

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.DashboardBuilder.init();
});

// Also initialize when Blazor is ready (for WASM)
if (typeof Blazor !== 'undefined') {
    Blazor .addEventListener('enhancedload', () => {
        window.DashboardBuilder.init();
    });
}
