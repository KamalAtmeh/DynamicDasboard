/**
 * Schema Visualization script for Dynamic Dashboard
 * Uses Cytoscape.js to render database schema visualizations
 */

window.schemaVisualization = {
    cy: null,
    dotNetHelper: null,
    container: null,

    // Check if Cytoscape.js is loaded
    isCytoscapeLoaded: function () {
        return typeof cytoscape !== 'undefined';
    },

    // Load Cytoscape.js dynamically if not already loaded
    loadCytoscape: async function () {
        if (this.isCytoscapeLoaded()) return Promise.resolve(true);

        return new Promise((resolve, reject) => {
            // Load Cytoscape.js from CDN
            const script = document.createElement('script');
            script.src = 'https://cdnjs.cloudflare.com/ajax/libs/cytoscape/3.21.1/cytoscape.min.js';
            script.integrity = 'sha512-H1fK+v/+qZX0cW0VC0hNUJdZDDo9lANPQW06iZZqJt3zn1QTYm4XQ4yWLwz+cbQJa+G9GqgXDGUb5ejtnEGmA==';
            script.crossOrigin = 'anonymous';
            script.onload = () => resolve(true);
            script.onerror = () => reject(new Error('Failed to load Cytoscape.js'));
            document.head.appendChild(script);
        });
    },

    // Initialize the visualization
    initialize: async function (dotNetHelper, container) {
        this.dotNetHelper = dotNetHelper;
        this.container = container;

        // Ensure Cytoscape is loaded
        if (!this.isCytoscapeLoaded()) {
            await this.loadCytoscape();
        }
    },

    // Render the schema elements
    render: function (elements) {
        // Destroy previous instance if it exists
        if (this.cy) {
            this.cy.destroy();
        }

        // Get the container element
        const containerElement = document.querySelector(this.container);
        if (!containerElement) {
            console.error('Container element not found:', this.container);
            return;
        }

        // Initialize Cytoscape with the elements and styling
        this.cy = cytoscape({
            container: containerElement,
            elements: elements,
            layout: {
                name: 'cose',
                nodeDimensionsIncludeLabels: true,
                idealEdgeLength: 100,
                nodeOverlap: 20,
                padding: 30,
                randomize: false,
                componentSpacing: 100,
                animate: true
            },
            style: [
                // Table nodes
                {
                    selector: 'node[type="table"]',
                    style: {
                        'shape': 'roundrectangle',
                        'background-color': '#4e73df',
                        'label': 'data(label)',
                        'color': 'white',
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'text-wrap': 'wrap',
                        'text-max-width': '100px',
                        'width': 'label',
                        'height': 'label',
                        'padding': '15px'
                    }
                },
                // Column nodes
                {
                    selector: 'node[type="column"]',
                    style: {
                        'shape': 'roundrectangle',
                        'background-color': '#f8f9fa',
                        'border-color': '#d1d3e2',
                        'border-width': 1,
                        'label': 'data(label)',
                        'color': '#3a3b45',
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'text-wrap': 'wrap',
                        'width': 'label',
                        'height': 'label',
                        'padding': '5px'
                    }
                },
                // Lookup column nodes
                {
                    selector: 'node[type="column"][isLookup=true]',
                    style: {
                        'background-color': '#36b9cc',
                        'color': 'white'
                    }
                },
                // Primary key column nodes
                {
                    selector: 'node[type="column"][isPrimary=true]',
                    style: {
                        'background-color': '#f6c23e',
                        'color': 'white',
                        'border-width': 2,
                        'border-color': '#e0b339'
                    }
                },
                // Parent-child relationship (compound nodes)
                {
                    selector: 'node[type="table"] node[type="column"]',
                    style: {
                        'text-margin-y': -5
                    }
                },
                // Normal relationships
                {
                    selector: 'edge',
                    style: {
                        'width': 2,
                        'line-color': '#dddfeb',
                        'target-arrow-color': '#dddfeb',
                        'target-arrow-shape': 'triangle',
                        'curve-style': 'bezier',
                        'label': 'data(label)',
                        'text-background-color': 'white',
                        'text-background-opacity': 0.7,
                        'text-background-padding': 2,
                        'font-size': '10px'
                    }
                },
                // Suggested relationships
                {
                    selector: 'edge[type="suggested"]',
                    style: {
                        'line-color': '#36b9cc',
                        'target-arrow-color': '#36b9cc',
                        'line-style': 'dashed',
                        'line-dash-pattern': [6, 3]
                    }
                },
                // Primary key relationships
                {
                    selector: 'edge[type="primary"]',
                    style: {
                        'line-color': '#4e73df',
                        'target-arrow-color': '#4e73df',
                        'width': 3
                    }
                }
            ],
            // Interaction options
            minZoom: 0.2,
            maxZoom: 3,
            wheelSensitivity: 0.2
        });

        // Add event handlers
        this.cy.on('tap', 'node', (evt) => {
            const node = evt.target;
            // Call back to .NET component
            this.dotNetHelper.invokeMethodAsync(
                'HandleNodeClick',
                node.id(),
                node.data('type')
            );
        });

        // Double-click on background to reset view
        this.cy.on('dblclick', (evt) => {
            if (evt.target === this.cy) {
                this.resetView();
            }
        });

        // Fit the graph to the container
        this.cy.fit();
    },

    // Zoom in function
    zoomIn: function () {
        if (!this.cy) return;
        const currentZoom = this.cy.zoom();
        this.cy.zoom({
            level: currentZoom * 1.2,
            renderedPosition: { x: this.cy.width() / 2, y: this.cy.height() / 2 }
        });
    },

    // Zoom out function
    zoomOut: function () {
        if (!this.cy) return;
        const currentZoom = this.cy.zoom();
        this.cy.zoom({
            level: currentZoom / 1.2,
            renderedPosition: { x: this.cy.width() / 2, y: this.cy.height() / 2 }
        });
    },

    // Reset view function
    resetView: function () {
        if (!this.cy) return;
        this.cy.fit();
    },

    // Export the visualization as an image
    exportImage: function (format = 'png') {
        if (!this.cy) return null;
        return this.cy.png({ full: true, scale: 2, output: format });
    },

    // Update specific node properties
    updateNodeProperty: function (nodeId, property, value) {
        if (!this.cy) return;
        const node = this.cy.$id(nodeId);
        if (node) {
            node.data(property, value);
        }
    },

    // Highlight specific node
    highlightNode: function (nodeId) {
        if (!this.cy) return;
        // Reset all nodes to default
        this.cy.nodes().removeClass('highlighted');

        // Highlight the specified node
        const node = this.cy.$id(nodeId);
        if (node) {
            node.addClass('highlighted');
        }
    }
};

// File download helper function
window.downloadFile = function (filename, contentType, content) {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.href = url;
    link.download = filename;

    document.body.appendChild(link);
    link.click();

    // Clean up
    setTimeout(() => {
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }, 100);
};

// Trigger the click event on the file input element
window.triggerFileInput = function (inputId) {
    const element = document.getElementById(inputId);
    if (element) {
        element.click();
    }
};