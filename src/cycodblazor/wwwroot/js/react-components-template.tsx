// react-components-template.tsx - React component templates for chat integration
// These are templates to be adapted when integrating with the actual React app

/*
// TEMPLATE: Basic Chat Input Component
// Copy this to your React app's components directory and adapt as needed

import React, { useState, KeyboardEvent } from 'react';
import { useChatContext } from '../hooks/useCycodChat'; // Adjust import path

export interface ChatInputProps {
  placeholder?: string;
  disabled?: boolean;
  onSend?: (message: string) => void;
  className?: string;
}

export function ChatInput({ 
  placeholder = "Type your message...", 
  disabled, 
  onSend,
  className 
}: ChatInputProps) {
  const [message, setMessage] = useState('');
  const { sendStreamingMessage, isLoading, isStreaming } = useChatContext();

  const handleSend = async () => {
    if (!message.trim() || isLoading || isStreaming) return;

    const messageToSend = message.trim();
    setMessage('');

    if (onSend) {
      onSend(messageToSend);
    } else {
      await sendStreamingMessage(messageToSend);
    }
  };

  const handleKeyPress = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const isDisabled = disabled || isLoading || isStreaming;

  return (
    <div className={`chat-input-container ${className || ''}`}>
      <input
        type="text"
        value={message}
        onChange={(e) => setMessage(e.target.value)}
        onKeyPress={handleKeyPress}
        placeholder={placeholder}
        disabled={isDisabled}
        className="chat-input"
      />
      <button 
        onClick={handleSend}
        disabled={isDisabled || !message.trim()}
        className="chat-send-button"
      >
        {isStreaming ? 'Sending...' : 'Send'}
      </button>
    </div>
  );
}

// TEMPLATE: Chat Message Component
import React from 'react';
import { ChatMessage } from '../types/chat'; // Adjust import path

export interface ChatMessageProps {
  message: ChatMessage;
  showTimestamp?: boolean;
  className?: string;
}

export function ChatMessageComponent({ 
  message, 
  showTimestamp = true,
  className 
}: ChatMessageProps) {
  const formatContent = (content: string) => {
    return content
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      .replace(/`(.*?)`/g, '<code>$1</code>')
      .replace(/\n/g, '<br>');
  };

  return (
    <div className={`chat-message chat-message-${message.role} ${className || ''}`}>
      <div className="message-header">
        <span className="message-role">
          {message.role === 'user' ? 'You' : message.role === 'assistant' ? 'AI' : 'System'}
        </span>
        {showTimestamp && (
          <span className="message-timestamp">
            {message.timestamp.toLocaleTimeString()}
          </span>
        )}
        {message.isStreaming && (
          <span className="message-streaming-indicator">●</span>
        )}
      </div>
      <div 
        className="message-content"
        dangerouslySetInnerHTML={{ __html: formatContent(message.content) }}
      />
    </div>
  );
}

// TEMPLATE: Chat History Component
import React, { useEffect, useRef } from 'react';
import { useChatContext } from '../hooks/useCycodChat';
import { ChatMessageComponent } from './ChatMessage';

export interface ChatHistoryProps {
  autoScroll?: boolean;
  showTimestamps?: boolean;
  className?: string;
  maxHeight?: string;
}

export function ChatHistory({ 
  autoScroll = true,
  showTimestamps = true,
  className,
  maxHeight = '400px'
}: ChatHistoryProps) {
  const { messages } = useChatContext();
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (autoScroll && scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messages, autoScroll]);

  return (
    <div 
      ref={scrollRef}
      className={`chat-history ${className || ''}`}
      style={{ maxHeight, overflowY: 'auto' }}
    >
      {messages.map((message) => (
        <ChatMessageComponent
          key={message.id}
          message={message}
          showTimestamp={showTimestamps}
        />
      ))}
      {messages.length === 0 && (
        <div className="chat-history-empty">
          No messages yet. Start a conversation!
        </div>
      )}
    </div>
  );
}

// TEMPLATE: Function Approval Modal
import React from 'react';

export interface FunctionApprovalModalProps {
  isOpen: boolean;
  functionName: string;
  functionArgs: string;
  onApprove: () => void;
  onDeny: () => void;
  className?: string;
}

export function FunctionApprovalModal({
  isOpen,
  functionName,
  functionArgs,
  onApprove,
  onDeny,
  className
}: FunctionApprovalModalProps) {
  if (!isOpen) return null;

  let parsedArgs;
  try {
    parsedArgs = JSON.parse(functionArgs || '{}');
  } catch {
    parsedArgs = functionArgs;
  }

  return (
    <div className={`function-approval-modal ${className || ''}`}>
      <div className="modal-overlay" onClick={onDeny}>
        <div className="modal-content" onClick={(e) => e.stopPropagation()}>
          <h3>Function Call Approval</h3>
          <p>The AI wants to call function: <strong>{functionName}</strong></p>
          
          <div className="function-args">
            <h4>Arguments:</h4>
            <pre>{JSON.stringify(parsedArgs, null, 2)}</pre>
          </div>

          <div className="modal-actions">
            <button onClick={onApprove} className="btn-approve">
              Approve
            </button>
            <button onClick={onDeny} className="btn-deny">
              Deny
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

// TEMPLATE: Chat Status Component
import React from 'react';
import { useChatContext } from '../hooks/useCycodChat';

export interface ChatStatusProps {
  showCapabilities?: boolean;
  className?: string;
}

export function ChatStatus({ 
  showCapabilities = false,
  className 
}: ChatStatusProps) {
  const { isInitialized, isLoading, error, isStreaming } = useChatContext();

  const getStatusText = () => {
    if (error) return `Error: ${error.message}`;
    if (isStreaming) return 'AI is responding...';
    if (isLoading) return 'Loading...';
    if (isInitialized) return 'Ready';
    return 'Not connected';
  };

  const getStatusClass = () => {
    if (error) return 'status-error';
    if (isStreaming || isLoading) return 'status-loading';
    if (isInitialized) return 'status-ready';
    return 'status-disconnected';
  };

  return (
    <div className={`chat-status ${getStatusClass()} ${className || ''}`}>
      <span className="status-indicator">●</span>
      <span className="status-text">{getStatusText()}</span>
      
      {showCapabilities && isInitialized && (
        <div className="status-capabilities">
          <span>Streaming ✓</span>
          <span>Functions ✓</span>
          <span>History ✓</span>
        </div>
      )}
    </div>
  );
}

// TEMPLATE: Complete Chat Interface
import React, { useState } from 'react';
import { ChatProvider, ChatConfig } from '../hooks/useCycodChat';
import { ChatHistory } from './ChatHistory';
import { ChatInput } from './ChatInput';
import { ChatStatus } from './ChatStatus';
import { FunctionApprovalModal } from './FunctionApprovalModal';

export interface ChatInterfaceProps {
  config: ChatConfig;
  className?: string;
  onError?: (error: Error) => void;
}

export function ChatInterface({ 
  config, 
  className,
  onError 
}: ChatInterfaceProps) {
  const [showApprovalModal, setShowApprovalModal] = useState(false);
  const [pendingFunction, setPendingFunction] = useState<{
    name: string;
    args: string;
    resolve: (approved: boolean) => void;
  } | null>(null);

  const handleFunctionApproval = (functionName: string, args: string): Promise<boolean> => {
    return new Promise((resolve) => {
      setPendingFunction({ name: functionName, args, resolve });
      setShowApprovalModal(true);
    });
  };

  const handleApprove = () => {
    pendingFunction?.resolve(true);
    setShowApprovalModal(false);
    setPendingFunction(null);
  };

  const handleDeny = () => {
    pendingFunction?.resolve(false);
    setShowApprovalModal(false);
    setPendingFunction(null);
  };

  return (
    <ChatProvider 
      config={{
        ...config,
        // Override function approval with our modal
      }} 
      onError={onError}
    >
      <div className={`chat-interface ${className || ''}`}>
        <ChatStatus showCapabilities={true} />
        <ChatHistory autoScroll={true} showTimestamps={true} />
        <ChatInput />
        
        <FunctionApprovalModal
          isOpen={showApprovalModal}
          functionName={pendingFunction?.name || ''}
          functionArgs={pendingFunction?.args || '{}'}
          onApprove={handleApprove}
          onDeny={handleDeny}
        />
      </div>
    </ChatProvider>
  );
}

// TEMPLATE: CSS Styles
// Add this to your CSS file or styled-components

.chat-interface {
  display: flex;
  flex-direction: column;
  height: 100%;
  max-width: 800px;
  margin: 0 auto;
  border: 1px solid #ddd;
  border-radius: 8px;
  overflow: hidden;
}

.chat-status {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: #f5f5f5;
  font-size: 14px;
}

.status-indicator {
  font-size: 12px;
}

.status-ready .status-indicator { color: #28a745; }
.status-loading .status-indicator { color: #ffc107; }
.status-error .status-indicator { color: #dc3545; }
.status-disconnected .status-indicator { color: #6c757d; }

.chat-history {
  flex: 1;
  padding: 16px;
  background: white;
}

.chat-message {
  margin-bottom: 16px;
  padding: 12px;
  border-radius: 8px;
}

.chat-message-user {
  background: #e3f2fd;
  margin-left: 20%;
}

.chat-message-assistant {
  background: #f5f5f5;
  margin-right: 20%;
}

.chat-message-system {
  background: #fff3e0;
  font-style: italic;
}

.message-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  font-size: 12px;
  color: #666;
}

.message-streaming-indicator {
  color: #28a745;
  animation: pulse 1s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

.chat-input-container {
  display: flex;
  padding: 16px;
  background: #f9f9f9;
  border-top: 1px solid #ddd;
}

.chat-input {
  flex: 1;
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  margin-right: 8px;
}

.chat-send-button {
  padding: 8px 16px;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.chat-send-button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.function-approval-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  z-index: 1000;
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
  padding: 24px;
  max-width: 500px;
  max-height: 80vh;
  overflow-y: auto;
}

.function-args {
  margin: 16px 0;
}

.function-args pre {
  background: #f5f5f5;
  padding: 12px;
  border-radius: 4px;
  font-size: 12px;
  overflow-x: auto;
}

.modal-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
  margin-top: 16px;
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

*/