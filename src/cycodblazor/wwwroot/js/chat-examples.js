// chat-examples.js - Example usage of the chat streaming functionality

// Example 1: Basic streaming chat
async function basicStreamingChat() {
    try {
        // Initialize chat
        const initResult = await DotNet.invokeMethodAsync("cycodblazor", "InitializeChat", 
            "You are a helpful assistant that responds concisely.", 1000);
        
        console.log('Init result:', initResult);

        // Send streaming message
        let fullResponse = '';
        await chatHelpers.streamMessage(
            "Explain quantum computing in simple terms",
            // onChunk
            (chunk) => {
                fullResponse += chunk;
                console.log('Chunk received:', chunk);
                // Update UI here
                const chatOutput = document.getElementById('chat-output');
                if (chatOutput) {
                    chatOutput.textContent = fullResponse;
                    chatHelpers.autoScrollToBottom(chatOutput);
                }
            },
            // onComplete
            () => {
                console.log('Streaming complete:', fullResponse);
            },
            // onError
            (error) => {
                console.error('Streaming error:', error);
            }
        );
    } catch (error) {
        console.error('Chat error:', error);
    }
}

// Example 2: Chat with function calling
async function functionCallingChat() {
    try {
        // Initialize chat with function calling capabilities
        const initResult = await DotNet.invokeMethodAsync("cycodblazor", "InitializeChat", 
            "You are a helpful assistant with access to various functions. Use them when appropriate.", 2000);
        
        console.log('Init result:', initResult);

        let fullResponse = '';
        await chatHelpers.streamMessage(
            "What's the current time and date?",
            // onChunk
            (chunk) => {
                fullResponse += chunk;
                const chatOutput = document.getElementById('chat-output');
                if (chatOutput) {
                    chatOutput.innerHTML = chatHelpers.formatStreamingContent(fullResponse);
                    chatHelpers.autoScrollToBottom(chatOutput);
                }
            },
            // onComplete
            () => {
                console.log('Function calling chat complete:', fullResponse);
            },
            // onError
            (error) => {
                console.error('Function calling error:', error);
            }
        );
    } catch (error) {
        console.error('Function calling chat error:', error);
    }
}

// Example 3: Chat with history persistence
async function chatWithHistory() {
    try {
        // Initialize chat
        await DotNet.invokeMethodAsync("cycodblazor", "InitializeChat", 
            "You are a helpful assistant that remembers our conversation.", 1000);

        // Try to load previous conversation
        const loadResult = await DotNet.invokeMethodAsync("cycodblazor", "LoadChatHistory", "my-conversation");
        console.log('Load history result:', loadResult);

        // Send message
        let fullResponse = '';
        await chatHelpers.streamMessage(
            "Continue our previous conversation",
            (chunk) => {
                fullResponse += chunk;
                console.log('Chunk:', chunk);
            },
            async () => {
                // Save conversation after completion
                const saveResult = await DotNet.invokeMethodAsync("cycodblazor", "SaveChatHistory", "my-conversation");
                console.log('Save history result:', saveResult);
            },
            (error) => console.error('Error:', error)
        );
    } catch (error) {
        console.error('History chat error:', error);
    }
}

// Example 4: Advanced streaming with UI updates
class ChatUI {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
        this.setupUI();
    }

    setupUI() {
        if (!this.container) return;

        this.container.innerHTML = `
            <div class="chat-container">
                <div id="chat-messages" class="chat-messages"></div>
                <div class="chat-input-container">
                    <input type="text" id="chat-input" placeholder="Type your message..." />
                    <button id="send-btn">Send</button>
                    <button id="clear-btn">Clear History</button>
                </div>
                <div id="chat-status" class="chat-status"></div>
            </div>
        `;

        // Add event listeners
        const sendBtn = this.container.querySelector('#send-btn');
        const clearBtn = this.container.querySelector('#clear-btn');
        const input = this.container.querySelector('#chat-input');

        sendBtn?.addEventListener('click', () => this.sendMessage());
        clearBtn?.addEventListener('click', () => this.clearHistory());
        input?.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.sendMessage();
        });

        this.messagesDiv = this.container.querySelector('#chat-messages');
        this.inputElement = input;
        this.statusDiv = this.container.querySelector('#chat-status');

        // Initialize chat
        this.initializeChat();
    }

    async initializeChat() {
        try {
            this.updateStatus('Initializing chat...');
            
            const result = await DotNet.invokeMethodAsync("cycodblazor", "InitializeChat", 
                "You are a helpful AI assistant. Respond clearly and concisely.", 2000);
            
            const response = JSON.parse(result);
            if (response.success) {
                this.updateStatus('Chat ready');
            } else {
                this.updateStatus(`Error: ${response.error}`);
            }
        } catch (error) {
            this.updateStatus(`Initialization error: ${error.message}`);
        }
    }

    async sendMessage() {
        const message = this.inputElement?.value?.trim();
        if (!message) return;

        // Clear input
        if (this.inputElement) this.inputElement.value = '';

        // Add user message to UI
        this.addMessage('user', message);
        this.updateStatus('AI is thinking...');

        try {
            let aiResponse = '';
            const aiMessageElement = this.addMessage('assistant', '');

            await chatHelpers.streamMessage(
                message,
                // onChunk
                (chunk) => {
                    aiResponse += chunk;
                    if (aiMessageElement) {
                        aiMessageElement.innerHTML = chatHelpers.formatStreamingContent(aiResponse);
                    }
                    this.scrollToBottom();
                },
                // onComplete
                () => {
                    this.updateStatus('Ready');
                },
                // onError
                (error) => {
                    this.updateStatus(`Error: ${error}`);
                    if (aiMessageElement) {
                        aiMessageElement.innerHTML = `<em>Error: ${error}</em>`;
                    }
                }
            );
        } catch (error) {
            this.updateStatus(`Error: ${error.message}`);
        }
    }

    addMessage(role, content) {
        if (!this.messagesDiv) return null;

        const messageDiv = document.createElement('div');
        messageDiv.className = `message message-${role}`;
        messageDiv.innerHTML = `
            <div class="message-role">${role === 'user' ? 'You' : 'AI'}</div>
            <div class="message-content">${content}</div>
        `;

        this.messagesDiv.appendChild(messageDiv);
        this.scrollToBottom();
        
        return messageDiv.querySelector('.message-content');
    }

    async clearHistory() {
        try {
            const result = await DotNet.invokeMethodAsync("cycodblazor", "ClearChatHistory");
            const response = JSON.parse(result);
            
            if (response.success) {
                if (this.messagesDiv) this.messagesDiv.innerHTML = '';
                this.updateStatus('History cleared');
            } else {
                this.updateStatus(`Clear error: ${response.error}`);
            }
        } catch (error) {
            this.updateStatus(`Clear error: ${error.message}`);
        }
    }

    updateStatus(message) {
        if (this.statusDiv) {
            this.statusDiv.textContent = message;
        }
    }

    scrollToBottom() {
        if (this.messagesDiv) {
            chatHelpers.autoScrollToBottom(this.messagesDiv);
        }
    }
}

// Initialize chat UI when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Example usage: new ChatUI('chat-container');
});

// Export for use in other scripts
window.ChatUI = ChatUI;
window.chatExamples = {
    basicStreamingChat,
    functionCallingChat,
    chatWithHistory
};