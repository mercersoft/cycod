// chat.js - JavaScript helpers for chat streaming functionality

window.chatHelpers = {
    // Create a streaming callback wrapper that can be passed to Blazor
    createStreamingCallback: function (onChunk, onComplete, onError) {
        return {
            OnChunk: function (content) {
                if (onChunk && typeof onChunk === 'function') {
                    onChunk(content);
                }
            },
            OnComplete: function () {
                if (onComplete && typeof onComplete === 'function') {
                    onComplete();
                }
            },
            OnError: function (error) {
                if (onError && typeof onError === 'function') {
                    onError(error);
                }
            }
        };
    },

    // Enhanced streaming with promise-based API
    streamMessage: async function (message, onChunk, onComplete, onError) {
        return new Promise(async (resolve, reject) => {
            try {
                // Create callback wrapper
                const callback = DotNet.createJSObjectReference(
                    this.createStreamingCallback(
                        onChunk,
                        () => {
                            if (onComplete) onComplete();
                            resolve();
                        },
                        (error) => {
                            if (onError) onError(error);
                            reject(new Error(error));
                        }
                    )
                );

                // Call Blazor streaming method
                const result = await DotNet.invokeMethodAsync(
                    "cycodblazor",
                    "SendMessageStreaming",
                    message,
                    callback
                );

                const response = JSON.parse(result);
                if (!response.success) {
                    throw new Error(response.error);
                }
            } catch (error) {
                if (onError) onError(error.message);
                reject(error);
            }
        });
    },

    // Function calling approval dialog
    showFunctionCallApproval: function (functionName, functionArgs) {
        return new Promise((resolve) => {
            const modal = document.createElement('div');
            modal.className = 'function-approval-modal';
            modal.innerHTML = `
                <div class="modal-overlay">
                    <div class="modal-content">
                        <h3>Function Call Approval</h3>
                        <p>The AI wants to call function: <strong>${functionName}</strong></p>
                        <pre>${JSON.stringify(JSON.parse(functionArgs || '{}'), null, 2)}</pre>
                        <div class="modal-actions">
                            <button id="approve-btn" class="btn-approve">Approve</button>
                            <button id="deny-btn" class="btn-deny">Deny</button>
                        </div>
                    </div>
                </div>
            `;
            
            // Add to document
            document.body.appendChild(modal);
            
            // Handle approval/denial
            modal.querySelector('#approve-btn').addEventListener('click', () => {
                document.body.removeChild(modal);
                resolve(true);
            });
            
            modal.querySelector('#deny-btn').addEventListener('click', () => {
                document.body.removeChild(modal);
                resolve(false);
            });
        });
    },

    // Format streaming content for display
    formatStreamingContent: function (content, targetElement) {
        if (targetElement && content) {
            // Simple markdown-like formatting
            let formatted = content
                .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
                .replace(/\*(.*?)\*/g, '<em>$1</em>')
                .replace(/`(.*?)`/g, '<code>$1</code>')
                .replace(/\n/g, '<br>');
            
            targetElement.innerHTML = formatted;
        }
    },

    // Auto-scroll to bottom during streaming
    autoScrollToBottom: function (element) {
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    }
};

// CSS styles for function approval modal
if (!document.getElementById('chat-styles')) {
    const styles = document.createElement('style');
    styles.id = 'chat-styles';
    styles.textContent = `
        .function-approval-modal {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            z-index: 10000;
        }
        
        .modal-overlay {
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.5);
            display: flex;
            align-items: center;
            justify-content: center;
        }
        
        .modal-content {
            background: white;
            border-radius: 8px;
            padding: 20px;
            max-width: 500px;
            max-height: 80vh;
            overflow-y: auto;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
        }
        
        .modal-content h3 {
            margin: 0 0 15px 0;
            color: #333;
        }
        
        .modal-content pre {
            background: #f5f5f5;
            padding: 10px;
            border-radius: 4px;
            font-size: 12px;
            overflow-x: auto;
        }
        
        .modal-actions {
            display: flex;
            gap: 10px;
            margin-top: 15px;
        }
        
        .btn-approve {
            background: #28a745;
            color: white;
            border: none;
            padding: 8px 16px;
            border-radius: 4px;
            cursor: pointer;
        }
        
        .btn-deny {
            background: #dc3545;
            color: white;
            border: none;
            padding: 8px 16px;
            border-radius: 4px;
            cursor: pointer;
        }
        
        .btn-approve:hover {
            background: #218838;
        }
        
        .btn-deny:hover {
            background: #c82333;
        }
    `;
    document.head.appendChild(styles);
}