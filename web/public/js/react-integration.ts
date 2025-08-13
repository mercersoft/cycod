// react-integration.ts - TypeScript definitions and helpers for React integration

declare global {
  interface Window {
    Blazor?: { start: (options?: unknown) => Promise<void> };
    DotNet?: {
      invokeMethodAsync: (assembly: string, method: string, ...args: any[]) => Promise<any>;
      createJSObjectReference: (jsObject: any) => any;
    };
  }
}

// Core chat response types
export interface ChatResponse {
  success: boolean;
  response?: string;
  message?: string;
  error?: string;
}

export interface ChatStatus {
  success: boolean;
  isInitialized: boolean;
  capabilities: {
    streaming: boolean;
    functionCalling: boolean;
    historyPersistence: boolean;
    approvalWorkflow: boolean;
  };
  version: string;
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  isStreaming?: boolean;
}

export interface FunctionCall {
  functionName: string;
  arguments: string;
  approved?: boolean;
  result?: string;
  timestamp: Date;
}

// Streaming callback interfaces
export interface StreamingCallbacks {
  onChunk?: (chunk: string) => void;
  onComplete?: () => void;
  onError?: (error: string) => void;
  onFunctionApproval?: (functionName: string, args: string) => Promise<boolean>;
  onFunctionCall?: (functionName: string, args: string, result: string) => void;
}

// Chat configuration options
export interface ChatConfig {
  systemPrompt: string;
  maxTokens?: number;
  enableFunctionCalling?: boolean;
  enableStreaming?: boolean;
  autoSave?: boolean;
  saveKey?: string;
}

// Error types for better error handling
export class ChatError extends Error {
  constructor(
    message: string,
    public readonly type: 'initialization' | 'network' | 'function_call' | 'streaming' | 'unknown' = 'unknown',
    public readonly originalError?: Error
  ) {
    super(message);
    this.name = 'ChatError';
  }
}

// Main chat API class for React integration
export class CycodChatAPI {
  private static instance: CycodChatAPI | null = null;
  private isInitialized = false;
  private blazorStarted = false;

  private constructor() {}

  static getInstance(): CycodChatAPI {
    if (!CycodChatAPI.instance) {
      CycodChatAPI.instance = new CycodChatAPI();
    }
    return CycodChatAPI.instance;
  }

  // Ensure Blazor is started before any chat operations
  async ensureBlazorStarted(): Promise<void> {
    if (this.blazorStarted) return;

    if (!window.Blazor?.start) {
      throw new ChatError('Blazor runtime not available', 'initialization');
    }

    try {
      await window.Blazor.start({
        loadBootResource: (_type: string, _name: string, defaultUri: string) => {
          if (defaultUri.startsWith('/_framework/')) return defaultUri;
          if (defaultUri.startsWith('_framework/')) return '/' + defaultUri;
          return defaultUri;
        },
      });
      this.blazorStarted = true;
    } catch (error) {
      throw new ChatError(
        'Failed to start Blazor runtime',
        'initialization',
        error instanceof Error ? error : undefined
      );
    }
  }

  // Initialize chat with configuration
  async initializeChat(config: ChatConfig): Promise<ChatResponse> {
    await this.ensureBlazorStarted();

    try {
      const result = await window.DotNet!.invokeMethodAsync(
        'cycodblazor',
        'InitializeChat',
        config.systemPrompt,
        config.maxTokens
      );

      const response: ChatResponse = JSON.parse(result);
      if (response.success) {
        this.isInitialized = true;
      }
      return response;
    } catch (error) {
      throw new ChatError(
        'Failed to initialize chat',
        'initialization',
        error instanceof Error ? error : undefined
      );
    }
  }

  // Send a simple message (non-streaming)
  async sendMessage(message: string): Promise<ChatResponse> {
    if (!this.isInitialized) {
      throw new ChatError('Chat not initialized. Call initializeChat first.', 'initialization');
    }

    try {
      const result = await window.DotNet!.invokeMethodAsync(
        'cycodblazor',
        'SendMessage',
        message
      );

      return JSON.parse(result) as ChatResponse;
    } catch (error) {
      throw new ChatError(
        'Failed to send message',
        'network',
        error instanceof Error ? error : undefined
      );
    }
  }

  // Send a streaming message
  async sendMessageStreaming(
    message: string,
    callbacks: StreamingCallbacks
  ): Promise<void> {
    if (!this.isInitialized) {
      throw new ChatError('Chat not initialized. Call initializeChat first.', 'initialization');
    }

    try {
      // Create streaming callback wrapper
      const callbackWrapper = {
        OnChunk: (chunk: string) => callbacks.onChunk?.(chunk),
        OnComplete: () => callbacks.onComplete?.(),
        OnError: (error: string) => callbacks.onError?.(error),
        OnFunctionCallApproval: async (functionName: string, args: string): Promise<boolean> => {
          if (callbacks.onFunctionApproval) {
            return await callbacks.onFunctionApproval(functionName, args);
          }
          return false; // Default to deny if no approval callback
        },
        OnFunctionCall: (functionName: string, args: string, result: string) => {
          callbacks.onFunctionCall?.(functionName, args, result);
        }
      };

      // Create JS object reference for Blazor
      const callbackRef = window.DotNet!.createJSObjectReference(callbackWrapper);

      const result = await window.DotNet!.invokeMethodAsync(
        'cycodblazor',
        'SendMessageStreaming',
        message,
        callbackRef
      );

      const response: ChatResponse = JSON.parse(result);
      if (!response.success) {
        throw new ChatError(response.error || 'Streaming failed', 'streaming');
      }
    } catch (error) {
      throw new ChatError(
        'Failed to start streaming',
        'streaming',
        error instanceof Error ? error : undefined
      );
    }
  }

  // Get chat status and capabilities
  async getChatStatus(): Promise<ChatStatus> {
    await this.ensureBlazorStarted();

    try {
      const result = await window.DotNet!.invokeMethodAsync(
        'cycodblazor',
        'GetChatStatus'
      );

      return JSON.parse(result) as ChatStatus;
    } catch (error) {
      throw new ChatError(
        'Failed to get chat status',
        'network',
        error instanceof Error ? error : undefined
      );
    }
  }

  // Clear chat history
  async clearChatHistory(): Promise<ChatResponse> {
    if (!this.isInitialized) {
      throw new ChatError('Chat not initialized. Call initializeChat first.', 'initialization');
    }

    try {
      const result = await window.DotNet!.invokeMethodAsync(
        'cycodblazor',
        'ClearChatHistory'
      );

      return JSON.parse(result) as ChatResponse;
    } catch (error) {
      throw new ChatError(
        'Failed to clear chat history',
        'network',
        error instanceof Error ? error : undefined
      );
    }
  }

  // Save chat history
  async saveChatHistory(key: string): Promise<ChatResponse> {
    if (!this.isInitialized) {
      throw new ChatError('Chat not initialized. Call initializeChat first.', 'initialization');
    }

    try {
      const result = await window.DotNet!.invokeMethodAsync(
        'cycodblazor',
        'SaveChatHistory',
        key
      );

      return JSON.parse(result) as ChatResponse;
    } catch (error) {
      throw new ChatError(
        'Failed to save chat history',
        'network',
        error instanceof Error ? error : undefined
      );
    }
  }

  // Load chat history
  async loadChatHistory(key: string): Promise<ChatResponse> {
    if (!this.isInitialized) {
      throw new ChatError('Chat not initialized. Call initializeChat first.', 'initialization');
    }

    try {
      const result = await window.DotNet!.invokeMethodAsync(
        'cycodblazor',
        'LoadChatHistory',
        key
      );

      return JSON.parse(result) as ChatResponse;
    } catch (error) {
      throw new ChatError(
        'Failed to load chat history',
        'network',
        error instanceof Error ? error : undefined
      );
    }
  }

  // Check if chat is initialized
  isReady(): boolean {
    return this.isInitialized && this.blazorStarted;
  }
}

// Utility functions for React components
export const chatUtils = {
  // Format message content with basic markdown
  formatMessageContent: (content: string): string => {
    return content
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      .replace(/`(.*?)`/g, '<code>$1</code>')
      .replace(/\n/g, '<br>');
  },

  // Generate unique message ID
  generateMessageId: (): string => {
    return `msg_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  },

  // Create chat message object
  createMessage: (
    role: 'user' | 'assistant' | 'system',
    content: string,
    isStreaming = false
  ): ChatMessage => ({
    id: chatUtils.generateMessageId(),
    role,
    content,
    timestamp: new Date(),
    isStreaming
  }),

  // Validate chat configuration
  validateConfig: (config: ChatConfig): string[] => {
    const errors: string[] = [];

    if (!config.systemPrompt || config.systemPrompt.trim().length === 0) {
      errors.push('System prompt is required');
    }

    if (config.maxTokens !== undefined && (config.maxTokens < 1 || config.maxTokens > 100000)) {
      errors.push('Max tokens must be between 1 and 100000');
    }

    return errors;
  }
};

// Export singleton instance
export const cycodChat = CycodChatAPI.getInstance();

// Default export for easy importing
export default cycodChat;