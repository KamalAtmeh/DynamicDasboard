// schemaVisualization.js
let cy = null;
let dotNetHelper = null;

window.schemaVisualization = {
    initialize: function (helper, container) {
        dotNetHelper = helper;

        // Load Cytoscape if not already loaded
        if (typeof cytoscape === 'undefined') {
            return loadCytoscape().then(() => {
                console.log('Cytoscape loaded successfully');
            });
        }

        return Promise.resolve();
    },

    render: function (elements) {
        if (typeof cytoscape === 'undefined') {
            console.error('Cytoscape not loaded');
            return;
        }

        // Destroy existing instance if it exists
        if (cy) {
            cy.destroy();
        }

        // Create new instance
        cy = cytoscape({
            container: document.querySelector('#schema-visualization'),
            elements: elements,
            style: [
                {
                    selector: 'node[type="table"]',
                    style: {
                        'shape': 'roundrectangle',
                        'background-color': '#4e73df',
                        'color': 'white',
                        'label': 'data(label)',
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'width': 'label',
                        'height': 'label',
                        'padding': '20px',
                        'text-wrap': 'wrap',
                        'text-max-width': '200px'
                    }
                },
                {
                    selector: 'node[type="column"]',
                    style: {
                        'shape': 'roundrectangle',
                        'background-color': '#f8f9fa',
                        'border-color': '#d1d3e2',
                        'border-width': 1,
                        'color': '#3a3b45',
                        'label': 'data(label)',
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'width': 'label',
                        'height': 'label',
                        'padding': '10px',
                        'text-wrap': 'wrap',
                        'text-max-width': '150px'
                    }
                },
                {
                    selector: 'node[type="column"][isLookup=true]',
                    style: {
                        'background-color': '#36b9cc',
                        'color': 'white'
                    }
                },
                {
                    selector: 'edge',
                    style: {
                        'width': 2,
                        'line-color': '#dddfeb',
                        'target-arrow-color': '#dddfeb',
                        'target-arrow-shape': 'triangle',
                        'curve-style': 'bezier',
                        'label': 'data(label)',
                        'font-size': '10px',
                        'text-rotation': 'autorotate'
                    }
                },
                {
                    selector: 'edge[type="suggested"]',
                    style: {
                        'line-color': '#36b9cc',
                        'target-arrow-color': '#36b9cc',
                        'line-style': 'dashed',
                        'line-dash-pattern': [6, 3]
                    }
                }
            ],
            layout: {
                name: 'cose',
                nodeOverlap: 20,
                padding: 30,
                nodeDimensionsIncludeLabels: true,
                idealEdgeLength: 100,
                animate: false,
                componentSpacing: 100,
                nodeRepulsion: 4500
            }
        });

        // Add event handlers
        cy.on('tap', 'node', function (evt) {
            const node = evt.target;
            dotNetHelper.invokeMethodAsync('HandleNodeClick', node.id(), node.data('type'));
        });

        // Add tooltips for nodes (could use a library like cytoscape-popper for better tooltips)
        cy.on('mouseover', 'node', function (evt) {
            const node = evt.target;
            const description = node.data('description');

            if (description) {
                // Simple tooltip implementation
                const tooltip = document.createElement('div');
                tooltip.id = 'cy-tooltip';
                tooltip.style.position = 'absolute';
                tooltip.style.backgroundColor = 'rgba(0, 0, 0, 0.8)';
                tooltip.style.color = 'white';
                tooltip.style.padding = '5px 10px';
                tooltip.style.borderRadius = '3px';
                tooltip.style.maxWidth = '200px';
                tooltip.style.zIndex = '999';
                tooltip.textContent = description;

                const renderedPosition = node.renderedPosition();
                const containerRect = cy.container().getBoundingClientRect();

                tooltip.style.left = (containerRect.left + renderedPosition.x) + 'px';
                tooltip.style.top = (containerRect.top + renderedPosition.y - 30) + 'px';

                document.body.appendChild(tooltip);
            }
        });

        cy.on('mouseout', 'node', function () {
            const tooltip = document.getElementById('cy-tooltip');
            if (tooltip) {
                tooltip.remove();
            }
        });
    },

    zoomIn: function () {
        if (cy) {
            cy.zoom(cy.zoom() * 1.2);
        }
    },

    zoomOut: function () {
        if (cy) {
            cy.zoom(cy.zoom() / 1.2);
        }
    },

    resetView: function () {
        if (cy) {
            cy.fit();
        }
    }
};

// Helper function to load Cytoscape dynamically
function loadCytoscape() {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/cytoscape/3.23.0/cytoscape.min.js';
        script.integrity = 'sha512-gEWKnYYa1/1c3iSLLNbWXuS4RZXIVIqCOdfBYsL1+oG8NH42+RCtarasbJGCFah9IXQk+qjOahHl8GBAZJJXIg==';
        script.crossOrigin = 'anonymous';
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

// Add download utility function
window.downloadFile = function (filename, contentType, content) {
    const a = document.createElement('a');
    const file = new Blob([content], { type: contentType });
    a.href = URL.createObjectURL(file);
    a.download = filename;
    a.click();
    URL.revokeObjectURL(a.href);
};