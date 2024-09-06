let pathCount = 1;

// Create a connection to the SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/progressHub")
    .build();

// Start the connection and handle progress updates
connection.on("ReceiveProgress", (message) => {
    const resultElement = document.getElementById("result");
    resultElement.textContent += "\n" + message;
});

connection.start().catch(err => console.error(err.toString()));

// Load paths when a set is selected
document.getElementById('setSelector').addEventListener('change', async () => {
    const setId = document.getElementById('setSelector').value;
    const response = await fetch(`/load/${setId}`);
    const data = await response.json();

    // Clear existing rows
    document.getElementById('pathsContainer').innerHTML = '';
    pathCount = data.sourcePaths.length;

    data.sourcePaths.forEach((sourcePath, index) => {
        const destinationPath = data.destinationPaths[index] || '';
        addPathRow(index + 1, sourcePath, destinationPath);
    });
});

// Add new source and destination path inputs dynamically
document.getElementById('addPathButton').addEventListener('click', () => {
    pathCount++;
    addPathRow(pathCount);
});

// Execute logic
document.getElementById('executeButton').addEventListener('click', async () => {
    const { sourcePaths, destinationPaths } = getAllPaths();
    const setId = document.getElementById('setSelector').value;

    await fetch(`/execute/${setId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ sourcePaths, destinationPaths })
    });
});

// Save inputs
document.getElementById('saveButton').addEventListener('click', async () => {
    const { sourcePaths, destinationPaths } = getAllPaths();
    const setId = document.getElementById('setSelector').value;

    const response = await fetch(`/save/${setId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ sourcePaths, destinationPaths })
    });

    const data = await response.json();
    alert(data.message);
});

// Load inputs
document.getElementById('loadButton').addEventListener('click', async () => {
    const setId = document.getElementById('setSelector').value;
    const response = await fetch(`/load/${setId}`);
    const data = await response.json();

    // Clear existing rows
    document.getElementById('pathsContainer').innerHTML = '';
    pathCount = data.sourcePaths.length;

    data.sourcePaths.forEach((sourcePath, index) => {
        const destinationPath = data.destinationPaths[index] || '';
        addPathRow(index + 1, sourcePath, destinationPath);
    });
});

// Get all source and destination paths from the form
function getAllPaths() {
    const sourcePaths = [];
    const destinationPaths = [];

    for (let i = 1; i <= pathCount; i++) {
        const sourceInput = document.getElementById(`sourcePath${i}`);
        const destinationInput = document.getElementById(`destinationPath${i}`);
        if (sourceInput && destinationInput) {
            sourcePaths.push(sourceInput.value);
            destinationPaths.push(destinationInput.value);
        }
    }

    return { sourcePaths, destinationPaths };
}

// Function to add a new path row
function addPathRow(index, sourcePath = '', destinationPath = '') {
    const container = document.getElementById('pathsContainer');
    const newInputRow = document.createElement('div');
    newInputRow.className = 'input-row';
    newInputRow.innerHTML = `
        <label for="sourcePath${index}">Source ${index}:</label>
        <input type="text" id="sourcePath${index}" name="sourcePath${index}" value="${sourcePath}" required>

        <label for="destinationPath${index}">Destination ${index}:</label>
        <input type="text" id="destinationPath${index}" name="destinationPath${index}" value="${destinationPath}" required>
    `;
    container.appendChild(newInputRow);
}
