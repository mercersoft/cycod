// react-hooks-template.ts - React hook templates for chat integration
// These are templates to be adapted when integrating with the actual React app

/*
// TEMPLATE: useCycodChat Hook
// Copy this to your React app's hooks directory and adapt as needed

import { useState, useCallback, useEffect, useRef } from 'react';
import { cycodChat, ChatMessage, ChatConfig, ChatError, StreamingCallbacks } from '../path/to/react-integration';

export interface UseCycodChatOptions {
  config?: ChatConfig;
  autoInit?: boolean;
  onError?: (error: ChatError) => void;
}

export interface UseCycodChatReturn {
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

export function useCycodChat(options: UseCycodChatOptions = {}): UseCycodChatReturn {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isInitialized, setIsInitialized] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<ChatError | null>(null);
  const [isStreaming, setIsStreaming] = useState(false);
  
  const currentStreamingMessage = useRef<ChatMessage | null>(null);
  const lastConfig = useRef<ChatConfig | null>(null);

  // Clear error when starting new operations
  const clearError = useCallback(() => setError(null), []);

  // Initialize chat
  const initializeChat = useCallback(async (config?: ChatConfig) => {
    const finalConfig = config || options.config;
    if (!finalConfig) {
      const error = new ChatError('Chat configuration is required', 'initialization');
      setError(error);
      options.onError?.(error);
      return;
    }

    setIsLoading(true);
    clearError();
    lastConfig.current = finalConfig;

    try {
      const response = await cycodChat.initializeChat(finalConfig);
      if (response.success) {
        setIsInitialized(true);
        
        // Add system message if provided
        if (finalConfig.systemPrompt) {
          const systemMessage: ChatMessage = {
            id: `system_${Date.now()}`,
            role: 'system',
            content: finalConfig.systemPrompt,
            timestamp: new Date()
          };
          setMessages([systemMessage]);
        }
      } else {
        const error = new ChatError(response.error || 'Failed to initialize chat', 'initialization');
        setError(error);
        options.onError?.(error);
      }
    } catch (err) {
      const error = err instanceof ChatError ? err : new ChatError('Initialization failed', 'initialization', err as Error);
      setError(error);
      options.onError?.(error);
    } finally {
      setIsLoading(false);
    }
  }, [options.config, options.onError, clearError]);

  // Send non-streaming message
  const sendMessage = useCallback(async (content: string) => {
    if (!isInitialized) {
      const error = new ChatError('Chat not initialized', 'initialization');
      setError(error);
      options.onError?.(error);
      return;
    }

    const userMessage: ChatMessage = {
      id: `user_${Date.now()}`,
      role: 'user',
      content,
      timestamp: new Date()
    };

    setMessages(prev => [...prev, userMessage]);
    setIsLoading(true);
    clearError();

    try {
      const response = await cycodChat.sendMessage(content);
      if (response.success && response.response) {
        const assistantMessage: ChatMessage = {
          id: `assistant_${Date.now()}`,
          role: 'assistant',
          content: response.response,
          timestamp: new Date()
        };
        setMessages(prev => [...prev, assistantMessage]);
      } else {
        const error = new ChatError(response.error || 'Failed to send message', 'network');
        setError(error);
        options.onError?.(error);
      }
    } catch (err) {
      const error = err instanceof ChatError ? err : new ChatError('Message sending failed', 'network', err as Error);
      setError(error);
      options.onError?.(error);
    } finally {
      setIsLoading(false);
    }
  }, [isInitialized, options.onError, clearError]);

  // Send streaming message
  const sendStreamingMessage = useCallback(async (content: string) => {
    if (!isInitialized) {
      const error = new ChatError('Chat not initialized', 'initialization');
      setError(error);
      options.onError?.(error);
      return;
    }

    const userMessage: ChatMessage = {
      id: `user_${Date.now()}`,
      role: 'user',
      content,
      timestamp: new Date()
    };

    // Create initial assistant message for streaming
    const assistantMessage: ChatMessage = {
      id: `assistant_${Date.now()}`,
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      isStreaming: true
    };

    setMessages(prev => [...prev, userMessage, assistantMessage]);
    setIsStreaming(true);
    clearError();
    currentStreamingMessage.current = assistantMessage;

    const callbacks: StreamingCallbacks = {
      onChunk: (chunk: string) => {
        if (currentStreamingMessage.current) {
          setMessages(prev => prev.map(msg => 
            msg.id === currentStreamingMessage.current!.id
              ? { ...msg, content: msg.content + chunk }
              : msg
          ));
        }
      },
      
      onComplete: () => {
        if (currentStreamingMessage.current) {
          setMessages(prev => prev.map(msg => 
            msg.id === currentStreamingMessage.current!.id
              ? { ...msg, isStreaming: false }
              : msg
          ));
        }
        setIsStreaming(false);
        currentStreamingMessage.current = null;
      },
      
      onError: (errorMessage: string) => {
        const error = new ChatError(errorMessage, 'streaming');
        setError(error);
        options.onError?.(error);
        setIsStreaming(false);
        currentStreamingMessage.current = null;
      },
      
      onFunctionApproval: async (functionName: string, args: string): Promise<boolean> => {
        // Default approval logic - can be overridden
        return window.confirm(`Allow function call: ${functionName}?`);
      },
      
      onFunctionCall: (functionName: string, args: string, result: string) => {
        console.log(`Function executed: ${functionName}`, { args, result });
        // Could add function call messages to the chat history
      }
    };

    try {
      await cycodChat.sendMessageStreaming(content, callbacks);
    } catch (err) {
      const error = err instanceof ChatError ? err : new ChatError('Streaming failed', 'streaming', err as Error);
      setError(error);
      options.onError?.(error);
      setIsStreaming(false);
    }
  }, [isInitialized, options.onError, clearError]);

  // Clear chat history
  const clearHistory = useCallback(async () => {
    setIsLoading(true);
    clearError();

    try {
      const response = await cycodChat.clearChatHistory();
      if (response.success) {
        setMessages([]);
      } else {
        const error = new ChatError(response.error || 'Failed to clear history', 'network');
        setError(error);
        options.onError?.(error);
      }
    } catch (err) {
      const error = err instanceof ChatError ? err : new ChatError('Clear history failed', 'network', err as Error);
      setError(error);
      options.onError?.(error);
    } finally {
      setIsLoading(false);
    }
  }, [options.onError, clearError]);

  // Save chat history
  const saveHistory = useCallback(async (key: string) => {
    setIsLoading(true);
    clearError();

    try {
      const response = await cycodChat.saveChatHistory(key);
      if (!response.success) {
        const error = new ChatError(response.error || 'Failed to save history', 'network');
        setError(error);
        options.onError?.(error);
      }
    } catch (err) {
      const error = err instanceof ChatError ? err : new ChatError('Save history failed', 'network', err as Error);
      setError(error);
      options.onError?.(error);
    } finally {
      setIsLoading(false);
    }
  }, [options.onError, clearError]);

  // Load chat history
  const loadHistory = useCallback(async (key: string) => {
    setIsLoading(true);
    clearError();

    try {
      const response = await cycodChat.loadChatHistory(key);
      if (!response.success) {
        const error = new ChatError(response.error || 'Failed to load history', 'network');
        setError(error);
        options.onError?.(error);
      }
      // Note: The actual messages would need to be retrieved separately
      // This depends on how the backend implements history loading
    } catch (err) {
      const error = err instanceof ChatError ? err : new ChatError('Load history failed', 'network', err as Error);
      setError(error);
      options.onError?.(error);
    } finally {
      setIsLoading(false);
    }
  }, [options.onError, clearError]);

  // Retry last operation
  const retry = useCallback(async () => {
    if (lastConfig.current && !isInitialized) {
      await initializeChat(lastConfig.current);
    }
  }, [initializeChat, isInitialized]);

  // Reset everything
  const reset = useCallback(() => {
    setMessages([]);
    setIsInitialized(false);
    setIsLoading(false);
    setError(null);
    setIsStreaming(false);
    currentStreamingMessage.current = null;
    lastConfig.current = null;
  }, []);

  // Auto-initialize if enabled
  useEffect(() => {
    if (options.autoInit && options.config && !isInitialized && !isLoading) {
      initializeChat(options.config);
    }
  }, [options.autoInit, options.config, isInitialized, isLoading, initializeChat]);

  return {
    // State
    messages,
    isInitialized,
    isLoading,
    error,
    isStreaming,
    
    // Actions
    initializeChat,
    sendMessage,
    sendStreamingMessage,
    clearHistory,
    saveHistory,
    loadHistory,
    
    // Utility
    retry,
    reset
  };
}

// TEMPLATE: Chat Context Provider
// Copy and adapt this for your React app

import React, { createContext, useContext, ReactNode } from 'react';

interface ChatContextValue extends UseCycodChatReturn {
  // Add any additional context-specific values
}

const ChatContext = createContext<ChatContextValue | null>(null);

export interface ChatProviderProps {
  children: ReactNode;
  config: ChatConfig;
  onError?: (error: ChatError) => void;
}

export function ChatProvider({ children, config, onError }: ChatProviderProps) {
  const chatHook = useCycodChat({
    config,
    autoInit: true,
    onError
  });

  return (
    <ChatContext.Provider value={chatHook}>
      {children}
    </ChatContext.Provider>
  );
}

export function useChatContext(): ChatContextValue {
  const context = useContext(ChatContext);
  if (!context) {
    throw new Error('useChatContext must be used within a ChatProvider');
  }
  return context;
}

*/