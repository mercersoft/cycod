# React Integration Guide: Cycode Chat Functionality

This guide explains how to integrate the Blazor chat functionality into your React application.

## Overview

The chat integration consists of:
- **Blazor Backend**: Chat API with JSInterop methods
- **TypeScript API Layer**: Type-safe interface for React
- **React Hooks**: State management and chat operations
- **React Components**: UI components for chat interface

## Integration Steps

### 1. Copy TypeScript Integration Files

Copy these files from the Blazor project to your React app:

```bash
# From: src/cycodblazor/wwwroot/js/react-integration.ts
# To: web/src/lib/cycodChat.ts

# From: src/cycodblazor/wwwroot/js/react-hooks-template.ts  
# To: web/src/hooks/useCycodChat.ts

# From: src/cycodblazor/wwwroot/js/react-components-template.tsx
# To: web/src/components/chat/ (multiple component files)
```

### 2. Install Dependencies

Ensure your React app has the necessary dependencies:

```json
{
  "dependencies": {
    "react": "^18.0.0",
    "react-dom": "^18.0.0"
  },
  "devDependencies": {
    "typescript": "^5.0.0",
    "@types/react": "^18.0.0",
    "@types/react-dom": "^18.0.0"
  }
}
```

### 3. Update Your Build Process

Ensure the Blazor _framework files are available in your React app's public directory:

```bash
# Copy Blazor output to React public directory
cp -r src/cycodblazor/bin/Debug/net9.0/wwwroot/_framework web/public/
```

### 4. Configure TypeScript

Update your `tsconfig.json` to include the chat types:

```json
{
  "compilerOptions": {
    "strict": true,
    "skipLibCheck": true,
    "esModuleInterop": true,
    "allowSyntheticDefaultImports": true
  },
  "include": [
    "src/**/*",
    "src/lib/cycodChat.ts"
  ]
}
```

## Usage Examples

### Basic Chat Integration

```tsx
// App.tsx
import React from 'react';
import { ChatInterface } from './components/chat/ChatInterface';
import { ChatConfig } from './lib/cycodChat';

function App() {
  const chatConfig: ChatConfig = {
    systemPrompt: "You are a helpful AI assistant.",
    maxTokens: 2000,
    enableFunctionCalling: true,
    enableStreaming: true
  };

  return (
    <div className="App">
      <h1>My App with AI Chat</h1>
      <ChatInterface 
        config={chatConfig}
        onError={(error) => console.error('Chat error:', error)}
      />
    </div>
  );
}

export default App;
```

### Using the Chat Hook

```tsx
// components/CustomChat.tsx
import React from 'react';
import { useCycodChat } from '../hooks/useCycodChat';

export function CustomChat() {
  const {
    messages,
    isInitialized,
    isLoading,
    sendStreamingMessage,
    initializeChat
  } = useCycodChat({
    config: {
      systemPrompt: "You are a helpful assistant.",
      maxTokens: 1000
    },
    autoInit: true
  });

  const handleSendMessage = async () => {
    await sendStreamingMessage("Hello, how can you help me?");
  };

  if (!isInitialized) {
    return <div>Initializing chat...</div>;
  }

  return (
    <div>
      <div className="messages">
        {messages.map(msg => (
          <div key={msg.id} className={`message ${msg.role}`}>
            {msg.content}
          </div>
        ))}
      </div>
      <button onClick={handleSendMessage} disabled={isLoading}>
        Send Test Message
      </button>
    </div>
  );
}
```

### Advanced Integration with Context

```tsx
// context/ChatContext.tsx
import React, { createContext, useContext } from 'react';
import { ChatProvider as BaseChatProvider } from '../hooks/useCycodChat';

// Wrap the base provider with app-specific logic
export function AppChatProvider({ children }: { children: React.ReactNode }) {
  const config = {
    systemPrompt: "You are an AI assistant for our application.",
    maxTokens: 2000,
    enableFunctionCalling: true,
    autoSave: true,
    saveKey: 'app-chat-history'
  };

  const handleError = (error: Error) => {
    // Integrate with your app's error reporting
    console.error('Chat error:', error);
    // Could show toast notification, send to error service, etc.
  };

  return (
    <BaseChatProvider config={config} onError={handleError}>
      {children}
    </BaseChatProvider>
  );
}
```

## API Reference

### CycodChatAPI Class

```typescript
class CycodChatAPI {
  // Initialize chat with configuration
  initializeChat(config: ChatConfig): Promise<ChatResponse>
  
  // Send simple message (non-streaming)
  sendMessage(message: string): Promise<ChatResponse>
  
  // Send streaming message with callbacks
  sendMessageStreaming(message: string, callbacks: StreamingCallbacks): Promise<void>
  
  // Get chat status and capabilities
  getChatStatus(): Promise<ChatStatus>
  
  // History management
  clearChatHistory(): Promise<ChatResponse>
  saveChatHistory(key: string): Promise<ChatResponse>
  loadChatHistory(key: string): Promise<ChatResponse>
  
  // Utility
  isReady(): boolean
}
```

### useCycodChat Hook

```typescript
interface UseCycodChatReturn {
  // State
  messages: ChatMessage[];
  isInitialized: boolean;
  isLoading: boolean;
  error: ChatError | null;
  isStreaming: boolean;
  
  // Actions
  initializeChat: (config?: ChatConfig) => Promise<void>;
  sendMessage: (content: string) => Promise<void>;
  sendStreamingMessage: (content: string) => Promise<void>;
  clearHistory: () => Promise<void>;
  saveHistory: (key: string) => Promise<void>;
  loadHistory: (key: string) => Promise<void>;
  
  // Utility
  retry: () => Promise<void>;
  reset: () => void;
}
```

### Type Definitions

```typescript
interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  isStreaming?: boolean;
}

interface ChatConfig {
  systemPrompt: string;
  maxTokens?: number;
  enableFunctionCalling?: boolean;
  enableStreaming?: boolean;
  autoSave?: boolean;
  saveKey?: string;
}

interface StreamingCallbacks {
  onChunk?: (chunk: string) => void;
  onComplete?: () => void;
  onError?: (error: string) => void;
  onFunctionApproval?: (functionName: string, args: string) => Promise<boolean>;
  onFunctionCall?: (functionName: string, args: string, result: string) => void;
}
```

## Best Practices

### 1. Error Handling

Always handle chat errors gracefully:

```tsx
const { error, retry } = useCycodChat();

if (error) {
  return (
    <div className="chat-error">
      <p>Chat error: {error.message}</p>
      <button onClick={retry}>Retry</button>
    </div>
  );
}
```

### 2. Loading States

Show appropriate loading states:

```tsx
const { isLoading, isStreaming } = useCycodChat();

return (
  <div>
    {isLoading && <div>Loading...</div>}
    {isStreaming && <div>AI is typing...</div>}
    {/* Chat UI */}
  </div>
);
```

### 3. Function Call Approval

Implement user-friendly approval dialogs:

```tsx
const handleFunctionApproval = async (functionName: string, args: string): Promise<boolean> => {
  const approved = await showCustomApprovalDialog({
    title: `Allow ${functionName}?`,
    description: `The AI wants to execute: ${functionName}`,
    args: JSON.parse(args)
  });
  
  return approved;
};
```

### 4. Memory Management

Clean up resources when components unmount:

```tsx
useEffect(() => {
  return () => {
    // Chat API handles cleanup automatically
    // But you might want to save history, etc.
  };
}, []);
```

## Styling

The components include basic CSS classes. Customize them to match your app's design:

```css
/* Override default chat styles */
.chat-interface {
  /* Your custom styles */
}

.chat-message-user {
  background: var(--user-message-bg);
}

.chat-message-assistant {
  background: var(--ai-message-bg);
}
```

## Troubleshooting

### Common Issues

1. **Blazor not loading**: Ensure _framework files are in public directory
2. **TypeScript errors**: Check that all types are properly imported
3. **Chat not initializing**: Verify system prompt and configuration
4. **Streaming not working**: Check browser console for JavaScript errors

### Debug Mode

Enable debug logging:

```tsx
const chatApi = CycodChatAPI.getInstance();
// Add debug flag if available
```

## Next Steps

After basic integration:

1. Add custom styling to match your app
2. Implement advanced function calling features
3. Add chat history persistence
4. Create specialized chat components for your use cases
5. Add telemetry and analytics

## Support

For issues with the integration:
1. Check browser console for errors
2. Verify Blazor backend is running
3. Test chat API methods directly
4. Review this integration guide