// wwwroot/js/toolbar.js

class Toolbar {
    constructor(toolbarElementId) {
        this.buttons = [];
        this.toolbarElement = document.getElementById(toolbarElementId);
        this.initDragAndDrop();
        this.initMessageHandler();
    }

    /**
     * Adds a button configuration to the toolbar and renders it.
     * @param {Object} button - The button configuration object.
     */
    addButton(button) {
        this.buttons.push(button);
        this.renderButton(button);
    }

    /**
     * Renders a single button based on its configuration.
     * @param {Object} button - The button configuration object.
     */
    renderButton(button) {
        const buttonElement = document.createElement('button');
        buttonElement.className = 'toolbar-button';
        buttonElement.textContent = button.label;
        buttonElement.onclick = () => this.executeButton(button);
        buttonElement.ondblclick = () => this.openConfigDialog(button);
        buttonElement.draggable = true;
        buttonElement.dataset.id = button.id;

        // Optional: Add tooltip
        if (button.tooltip) {
            const tooltip = document.createElement('span');
            tooltip.className = 'tooltip';
            tooltip.textContent = button.tooltip;
            buttonElement.appendChild(tooltip);
        }

        this.toolbarElement.appendChild(buttonElement);
    }

    /**
     * Opens the configuration dialog for a specific button.
     * @param {Object} button - The button configuration object.
     */
    openConfigDialog(button) {
        window.chrome.webview.postMessage({ type: 'openConfigDialog', button: button });
    }

    /**
     * Initializes drag-and-drop functionality for reordering buttons.
     */
    initDragAndDrop() {
        this.toolbarElement.addEventListener('dragstart', this.dragStart.bind(this));
        this.toolbarElement.addEventListener('dragover', this.dragOver.bind(this));
        this.toolbarElement.addEventListener('drop', this.drop.bind(this));
    }

    /**
     * Handles the dragstart event.
     * @param {DragEvent} e - The drag event.
     */
    dragStart(e) {
        if (e.target.classList.contains('toolbar-button')) {
            e.dataTransfer.setData('text/plain', e.target.dataset.id);
            e.dataTransfer.effectAllowed = 'move';
        }
    }

    /**
     * Handles the dragover event.
     * @param {DragEvent} e - The drag event.
     */
    dragOver(e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
    }

    /**
     * Handles the drop event to reorder buttons.
     * @param {DragEvent} e - The drag event.
     */
    drop(e) {
        e.preventDefault();
        const draggedButtonId = e.dataTransfer.getData('text');
        const targetButton = e.target.closest('button.toolbar-button');
        if (!targetButton) return;

        const draggedButtonElement = this.toolbarElement.querySelector(`[data-id='${draggedButtonId}']`);
        if (draggedButtonElement && draggedButtonElement !== targetButton) {
            const allButtons = Array.from(this.toolbarElement.children);
            const fromIndex = allButtons.indexOf(draggedButtonElement);
            const toIndex = allButtons.indexOf(targetButton);

            if (fromIndex < toIndex) {
                this.toolbarElement.insertBefore(draggedButtonElement, targetButton.nextSibling);
            } else {
                this.toolbarElement.insertBefore(draggedButtonElement, targetButton);
            }

            // Update the buttons array to reflect the new order
            const [reorderedButton] = this.buttons.splice(fromIndex, 1);
            this.buttons.splice(toIndex, 0, reorderedButton);

            // Save the new order
            this.saveButtonOrder();
        }
    }

    /**
     * Saves the current order of buttons by sending a message to the backend.
     */
    saveButtonOrder() {
        const buttonOrder = this.buttons.map(button => button.id);
        window.chrome.webview.postMessage({ type: 'saveButtonOrder', order: buttonOrder });
    }

    /**
     * Executes the action associated with a button.
     * @param {Object} button - The button configuration object.
     */
    async executeButton(button) {
        try {
            let response = '';
            switch (button.type) {
                case 'script':
                    response = await window.chrome.webview.hostObjects.scriptExecutor.ExecuteScriptAsync(
                        button.config.scriptType,
                        button.config.command,
                        button.config.adminRights
                    );
                    this.appendToTerminal(`Executed ${button.label}:\n${response}`, 'info');
                    break;
                case 'application':
                    response = await window.chrome.webview.hostObjects.systemService.ExecuteApplicationAsync(
                        button.config.path,
                        button.config.arguments,
                        button.config.adminRights
                    );
                    this.appendToTerminal(`Launched ${button.label}`, 'info');
                    break;
                case 'url':
                    await window.chrome.webview.hostObjects.systemService.OpenUrlAsync(button.config.url);
                    this.appendToTerminal(`Opened URL: ${button.config.url}`, 'info');
                    break;
                case 'plugin':
                    response = await window.chrome.webview.hostObjects.pluginService.ExecutePluginAsync(button.id);
                    this.appendToTerminal(`Executed Plugin ${button.label}:\n${response}`, 'info');
                    break;
                default:
                    this.appendToTerminal(`Unknown button type: ${button.type}`, 'warning');
            }
        } catch (error) {
            this.appendToTerminal(`Error executing ${button.label}:\n${error}`, 'error');
        }
    }

    /**
     * Appends messages to the terminal window (if embedded) or logs them.
     * For this implementation, it's handled by TerminalService.
     * This function can be customized as needed.
     */
    appendToTerminal(message, level = 'info') {
        // Placeholder for additional frontend terminal logging if needed
        console.log(`[${level.toUpperCase()}] ${message}`);
    }

    /**
     * Initializes the message handler to receive messages from the backend.
     */
    initMessageHandler() {
        window.chrome.webview.addEventListener('message', event => {
            const data = JSON.parse(event.data);
            switch (data.type) {
                case 'buttonOrderSaved':
                    this.appendToTerminal('Button order saved successfully.', 'info');
                    break;
                case 'buttonConfigUpdated':
                    this.appendToTerminal('Button configuration updated.', 'info');
                    break;
                    // Handle other message types as needed
                default:
                    console.warn(`Unknown message type received: ${data.type}`);
                    break;
            }
        });
    }

    /**
     * Updates a button's configuration in the toolbar.
     * @param {Object} updatedButton - The updated button configuration object.
     */
    updateButton(updatedButton) {
        const buttonIndex = this.buttons.findIndex(btn => btn.id === updatedButton.id);
        if (buttonIndex !== -1) {
            this.buttons[buttonIndex] = updatedButton;
            const existingButtonElement = this.toolbarElement.querySelector(`[data-id='${updatedButton.id}']`);
            if (existingButtonElement) {
                existingButtonElement.textContent = updatedButton.label;
                existingButtonElement.onclick = () => this.executeButton(updatedButton);
                existingButtonElement.ondblclick = () => this.openConfigDialog(updatedButton);
                // Update tooltip if exists
                if (updatedButton.tooltip) {
                    let tooltip = existingButtonElement.querySelector('.tooltip');
                    if (!tooltip) {
                        tooltip = document.createElement('span');
                        tooltip.className = 'tooltip';
                        existingButtonElement.appendChild(tooltip);
                    }
                    tooltip.textContent = updatedButton.tooltip;
                }
            }
            this.appendToTerminal(`Button '${updatedButton.label}' updated successfully.`, 'info');
        }
    }

    /**
     * Removes a button from the toolbar.
     * @param {string} buttonId - The ID of the button to remove.
     */
    removeButton(buttonId) {
        const buttonIndex = this.buttons.findIndex(btn => btn.id === buttonId);
        if (buttonIndex !== -1) {
            this.buttons.splice(buttonIndex, 1);
            const buttonElement = this.toolbarElement.querySelector(`[data-id='${buttonId}']`);
            if (buttonElement) {
                this.toolbarElement.removeChild(buttonElement);
                this.appendToTerminal(`Button with ID '${buttonId}' removed successfully.`, 'info');
            }
        }
    }
}

// Initialize the toolbar when the DOM is fully loaded
document.addEventListener('DOMContentLoaded', () => {
    const toolbar = new Toolbar('toolbar');
    toolbar.initMessageHandler();

    // Toolbar buttons are loaded and rendered from the backend via LoadAndRenderToolbarAsync in MainWindow.xaml.cs
});