# Chat Integration Plan: Exposing Chat Functionality to React via Blazor/JSInterop

## Current Architecture Analysis

### cycodlib
- Contains `FunctionCallingChat` class with full AI chat capabilities
- Features include:
  - Function calling with approval workflow
  - Streaming responses
  - Chat history management (save/load)
  - Token limit management
  - Integration with Microsoft.Extensions.AI

### cycodblazor
- Basic JSInterop setup in `Api.cs` with simple `Version()` method
- References cycodlib project
- Blazor WebAssembly application

### web (React App)
- Existing JSInterop integration via `cycodblazor.ts`
- Current integration only calls `Version()` method
- TypeScript setup with proper DotNet interop declarations

## Implementation Plan

### 1. Create Chat JSInterop API in cycodblazor
**File: `src/cycodblazor/Api.cs`**
- Extend existing API class to include chat-related methods:
  - `InitializeChat(systemPrompt, maxTokens?)` - Initialize a new chat session
  - `SendMessage(message)` - Send message and get complete response
  - `SendMessageStreaming(message)` - Send message with streaming response
  - `ClearChatHistory()` - Clear current chat session
  - `SaveChatHistory(key)` - Persist chat history to storage
  - `LoadChatHistory(key)` - Restore chat history from storage

### 2. Add Chat Service to cycodblazor
**File: `src/cycodblazor/Services/ChatService.cs`**
- Manage `FunctionCallingChat` instances
- Handle dependency injection for required services:
  - ILogger implementation
  - IStorageProvider implementation  
  - FunctionFactory setup
- Manage chat session state and lifecycle
- Handle chat configuration and options

### 3. Implement Streaming Support
- Create mechanism to stream chat responses back to React
- Use JavaScript callbacks or events for real-time updates
- Handle function calling approval workflow
- Implement proper error handling for streaming scenarios

### 4. Extend React Integration
**File: `web/src/cycodblazor.ts`**
- Add chat methods to existing TypeScript integration:
  - `initializeChat(systemPrompt, maxTokens?)`
  - `sendMessage(message)`
  - `sendMessageStreaming(message, onChunk?)`
  - `clearChatHistory()`
  - `saveChatHistory(key)`
  - `loadChatHistory(key)`
- Create TypeScript interfaces for:
  - Chat messages and responses
  - Streaming response chunks
  - Chat configuration options

### 5. Dependencies & Configuration
- Ensure all required cycodlib dependencies are available in Blazor context
- Configure AI chat client (likely OpenAI or similar)
- Set up function factory with available functions
- Implement storage provider for chat persistence
- Handle dependency injection setup in `Program.cs`

### 6. Error Handling & Safety
- Implement proper error boundaries and fallbacks
- Add validation for chat inputs and configuration
- Handle function calling security and approval mechanisms
- Provide graceful degradation when services are unavailable

## Key Benefits

- **Leverage Existing Functionality**: Use robust chat implementation from cycodlib
- **Separation of Concerns**: Keep chat logic in C# while UI remains in React
- **Advanced Features**: Enable function calling, streaming, and chat persistence
- **Flexible Interface**: Provide both simple and streaming chat options
- **Type Safety**: Full TypeScript support for chat interactions

## Technical Considerations

- **Performance**: Streaming responses for better user experience
- **State Management**: Proper chat session lifecycle management
- **Security**: Function calling approval and validation
- **Persistence**: Chat history storage and retrieval
- **Error Recovery**: Robust error handling and fallback mechanisms

## Implementation Order

1. ✅ Document plan (this file)
2. ✅ Extend `Api.cs` with chat JSInterop methods
3. ✅ Create `ChatService.cs` for chat management
4. ✅ Implement streaming support and callbacks
5. ⏳ Extend React TypeScript integration
6. ⏳ Configure dependencies and DI setup
7. ⏳ Add comprehensive error handling

## Step 3 Completed: Enhanced Streaming Support

### JavaScript Helpers (`/wwwroot/js/chat.js`)
- ✅ `createStreamingCallback()` - Wrapper for Blazor callbacks
- ✅ `streamMessage()` - Promise-based streaming API
- ✅ `showFunctionCallApproval()` - Interactive approval dialog
- ✅ `formatStreamingContent()` - Markdown-like content formatting
- ✅ `autoScrollToBottom()` - UI scroll management

### Enhanced StreamingCallback Class
- ✅ `OnChunk()` - Real-time content streaming
- ✅ `OnComplete()` - Streaming completion notification
- ✅ `OnError()` - Error handling
- ✅ `OnFunctionCallApproval()` - Interactive function approval
- ✅ `OnFunctionCall()` - Function execution notification

### ChatService Enhancements
- ✅ Full function calling workflow integration
- ✅ Approval callback handling
- ✅ Function execution callbacks
- ✅ Error handling for all callback scenarios

### Additional Features
- ✅ `GetChatStatus()` API method for capability discovery
- ✅ Comprehensive example usage (`chat-examples.js`)
- ✅ Complete ChatUI class for demonstration
- ✅ CSS styling for approval dialogs