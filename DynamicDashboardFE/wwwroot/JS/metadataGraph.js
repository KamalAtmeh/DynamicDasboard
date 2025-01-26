function initializeCytoscape(elements) {
    const cy = cytoscape({
        container: document.getElementById('cy'),
        elements: elements,
        style: [
            {
                selector: 'node',
                style: {
                    label: 'data(label)',
                    'text-valign': 'center',
                    'text-halign': 'center',
                    'background-color': '#666',
                    'color': '#fff',
                    'shape': 'rectangle',
                    'padding': '10px'
                }
            },
            {
                selector: 'node[parent]',
                style: {
                    'background-color': '#999',
                    'shape': 'ellipse',
                    'padding': '5px'
                }
            },
            {
                selector: 'edge',
                style: {
                    'label': 'data(label)',
                    'width': 2,
                    'line-color': '#999',
                    'curve-style': 'bezier',
                    'target-arrow-color': '#999',
                    'target-arrow-shape': 'triangle'
                }
            }
        ],
        layout: {
            name: 'cose',
            animate: true,
            fit: true,
            padding: 10
        }
    });

    // Handle node clicks (tables and columns)
    cy.on('tap', 'node', function (event) {
        const node = event.target;
        const id = node.data('id');

        if (id.startsWith('table_')) {
            const tableId = id.split('_')[1];
            DotNet.invokeMethodAsync('DynamicDashboardFE', 'OnTableClick', tableId);
        } else if (id.startsWith('column_')) {
            const columnId = id.split('_')[1];
            DotNet.invokeMethodAsync('DynamicDashboardFE', 'OnColumnClick', columnId);
        }
    });

    // Handle edge clicks (relationships)
    cy.on('tap', 'edge', function (event) {
        const edge = event.target;
        const relationshipId = edge.data('id').split('_')[1];
        DotNet.invokeMethodAsync('DynamicDashboardFE', 'OnRelationshipClick', relationshipId);
    });
}