import React, { useState, useEffect, useRef } from 'react';
import type { KeyboardEvent } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import type { TerminalAPI } from '@/types/terminal';

interface CommandOutput {
  command: string;
  output: string[];
  isStreaming?: boolean;
}

interface InteractiveTerminalCardMacProps {
  title?: string;
  username?: string;
  hostname?: string;
  currentPath?: string;
  onCommand: (command: string, addOutput: (text: string) => void, endOutput: () => void) => void;
  placeholder?: string;
}

const InteractiveTerminalCardMac: React.FC<InteractiveTerminalCardMacProps> = ({
  title = "Terminal",
  username = "user",
  hostname = "MacBook-Pro",
  currentPath = "~/projects",
  onCommand,
  placeholder = "Type a command and press Enter..."
}) => {
  const [history, setHistory] = useState<CommandOutput[]>([]);
  const [currentCommand, setCurrentCommand] = useState<string>('');
  const [isProcessing, setIsProcessing] = useState<boolean>(false);
  const isProcessingRef = useRef<boolean>(false);
  const [commandHistory, setCommandHistory] = useState<string[]>([]);
  const [historyIndex, setHistoryIndex] = useState<number>(-1);
  const [prompt, setPrompt] = useState<string>('$ ');
  const commandHandlerRef = useRef<(input: string, addOutput: (text: string) => void, endOutput: () => void) => void>(onCommand);
  const terminalRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const isResizingRef = useRef<boolean>(false);
  const startYRef = useRef<number>(0);
  const startHeightRef = useRef<number>(0);
  const [containerHeightPx, setContainerHeightPx] = useState<number>(384); // ~ h-96

  // Auto-scroll to bottom when new content is added
  useEffect(() => {
    if (terminalRef.current) {
      terminalRef.current.scrollTop = terminalRef.current.scrollHeight;
    }
  }, [history]);

  // Expose terminal API to window
  useEffect(() => {
    const terminalAPI: TerminalAPI = {
      setPrompt: (newPrompt: string) => setPrompt(newPrompt),
      getPrompt: () => prompt,
      commandHandler: async (input: string) => {
        // Create a promise-based wrapper for the command execution
        return new Promise<void>((resolve) => {
          const addOutput = (text: string) => {
            setHistory(prev => {
              const updated = [...prev];
              const lastEntry = updated[updated.length - 1];
              if (lastEntry && lastEntry.isStreaming) {
                lastEntry.output = [...lastEntry.output, text];
              }
              return updated;
            });
          };

          const endOutput = () => {
            setHistory(prev => {
              const updated = [...prev];
              const lastEntry = updated[updated.length - 1];
              if (lastEntry) {
                lastEntry.isStreaming = false;
              }
              return updated;
            });
            resolve();
          };

          // Use the ref to get the current handler
          commandHandlerRef.current(input, addOutput, endOutput);
        });
      }
    };

    // Override the commandHandler property to allow dynamic updates
    Object.defineProperty(terminalAPI, 'commandHandler', {
      get() {
        return async (input: string) => {
          // Create new command entry
          const newEntry: CommandOutput = {
            command: '',
            output: [],
            isStreaming: true
          };
          
          setHistory(prev => [...prev, newEntry]);

          return new Promise<void>((resolve) => {
            const addOutput = (text: string) => {
              setHistory(prev => {
                const updated = [...prev];
                const lastEntry = updated[updated.length - 1];
                if (lastEntry && lastEntry.isStreaming) {
                  lastEntry.output = [...lastEntry.output, text];
                }
                return updated;
              });
            };

            const endOutput = () => {
              setHistory(prev => {
                const updated = [...prev];
                const lastEntry = updated[updated.length - 1];
                if (lastEntry) {
                  lastEntry.isStreaming = false;
                }
                return updated;
              });
              setIsProcessing(false);
              isProcessingRef.current = false;
              resolve();
            };

            commandHandlerRef.current(input, addOutput, endOutput);
          });
        };
      },
      set(handler: (input: string, addOutput: (text: string) => void, endOutput: () => void) => void) {
        commandHandlerRef.current = handler;
      },
      configurable: true
    });

    window.terminal = terminalAPI;

    return () => {
      // Clean up on unmount
      if (window.terminal === terminalAPI) {
        delete window.terminal;
      }
    };
  }, [prompt]);

  // Focus input when clicking anywhere in the terminal
  const handleTerminalClick = () => {
    if (!isProcessing && inputRef.current) {
      inputRef.current.focus();
    }
  };

  // Resize handlers (vertical only)
  const onResizerMouseDown = (e: React.MouseEvent<HTMLDivElement>) => {
    isResizingRef.current = true;
    startYRef.current = e.clientY;
    startHeightRef.current = containerHeightPx;
    document.addEventListener('mousemove', onResizerMouseMove);
    document.addEventListener('mouseup', onResizerMouseUp);
    e.preventDefault();
  };

  const onResizerMouseMove = (e: MouseEvent) => {
    if (!isResizingRef.current) return;
    const deltaY = e.clientY - startYRef.current;
    const next = Math.max(240, Math.min(900, startHeightRef.current + deltaY));
    setContainerHeightPx(next);
  };

  const onResizerMouseUp = () => {
    if (!isResizingRef.current) return;
    isResizingRef.current = false;
    document.removeEventListener('mousemove', onResizerMouseMove);
    document.removeEventListener('mouseup', onResizerMouseUp);
  };

  // Cleanup in case component unmounts during resize
  useEffect(() => {
    return () => {
      document.removeEventListener('mousemove', onResizerMouseMove);
      document.removeEventListener('mouseup', onResizerMouseUp);
    };
  }, []);

  // Handle command submission
  const handleSubmit = () => {
    if (!currentCommand.trim()) return;
    if (isProcessingRef.current) return;
    // Synchronous reentrancy guard
    isProcessingRef.current = true;
    if (!isProcessing) {
      const cmd = currentCommand.trim();
      
      // Add to command history
      setCommandHistory(prev => [...prev, cmd]);
      setHistoryIndex(-1);
      
      // Create new command entry
      const newEntry: CommandOutput = {
        command: cmd,
        output: [],
        isStreaming: true
      };
      
      setHistory(prev => [...prev, newEntry]);
      setCurrentCommand('');
      setIsProcessing(true);

      // Callback functions for streaming output
      const addOutput = (text: string) => {
        setHistory(prev => {
          const updated = [...prev];
          const lastEntry = updated[updated.length - 1];
          if (lastEntry && lastEntry.isStreaming) {
            lastEntry.output = [...lastEntry.output, text];
          }
          return updated;
        });
      };

      const endOutput = () => {
        setHistory(prev => {
          const updated = [...prev];
          const lastEntry = updated[updated.length - 1];
          if (lastEntry) {
            lastEntry.isStreaming = false;
          }
          return updated;
        });
        setIsProcessing(false);
        isProcessingRef.current = false;
        
        // Focus input for next command
        setTimeout(() => {
          if (inputRef.current) {
            inputRef.current.focus();
          }
        }, 0);
      };

      // Call the current command handler
      commandHandlerRef.current(cmd, addOutput, endOutput);
    }
  };

  // Handle keyboard shortcuts
  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.repeat) return; // prevent auto-repeat duplicates
    if (e.key === 'Enter') {
      handleSubmit();
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      if (commandHistory.length > 0) {
        const newIndex = historyIndex === -1 
          ? commandHistory.length - 1 
          : Math.max(0, historyIndex - 1);
        setHistoryIndex(newIndex);
        setCurrentCommand(commandHistory[newIndex]);
      }
    } else if (e.key === 'ArrowDown') {
      e.preventDefault();
      if (historyIndex !== -1) {
        const newIndex = historyIndex + 1;
        if (newIndex >= commandHistory.length) {
          setHistoryIndex(-1);
          setCurrentCommand('');
        } else {
          setHistoryIndex(newIndex);
          setCurrentCommand(commandHistory[newIndex]);
        }
      }
    } else if (e.key === 'c' && e.ctrlKey) {
      // Ctrl+C to cancel current command
      if (isProcessing) {
        setHistory(prev => {
          const updated = [...prev];
          const lastEntry = updated[updated.length - 1];
          if (lastEntry && lastEntry.isStreaming) {
            lastEntry.output = [...lastEntry.output, '^C'];
            lastEntry.isStreaming = false;
          }
          return updated;
        });
        setIsProcessing(false);
      } else {
        setCurrentCommand('');
      }
    } else if (e.key === 'l' && e.ctrlKey) {
      // Ctrl+L to clear terminal
      e.preventDefault();
      setHistory([]);
    }
  };

  return (
    <Card className="bg-gray-900 border-gray-700 overflow-hidden shadow-2xl">
      {/* macOS Terminal Header */}
      <div className="bg-gradient-to-b from-gray-700 to-gray-800 px-3 py-2 flex items-center justify-between">
        <div className="flex items-center space-x-2">
          <div className="flex space-x-2">
            <button className="w-3 h-3 rounded-full bg-red-500 hover:bg-red-600 transition-colors group">
              <span className="hidden group-hover:block text-xs text-red-900 -mt-0.5 ml-0.5">×</span>
            </button>
            <button className="w-3 h-3 rounded-full bg-yellow-500 hover:bg-yellow-600 transition-colors group">
              <span className="hidden group-hover:block text-xs text-yellow-900 -mt-1 ml-0.5">−</span>
            </button>
            <button className="w-3 h-3 rounded-full bg-green-500 hover:bg-green-600 transition-colors group">
              <span className="hidden group-hover:block text-xs text-green-900 -mt-0.5 ml-0.5">⤢</span>
            </button>
          </div>
          <span className="text-xs text-gray-300 ml-4 font-['SF_Mono','Monaco','Courier_New',monospace]">
            {username}@{hostname} — {title} — {history.length + (isProcessing ? 1 : 0)} lines
          </span>
        </div>
      </div>

      {/* Terminal Content */}
      <CardContent 
        ref={terminalRef}
        onClick={handleTerminalClick}
        className="p-4 font-['SF_Mono','Monaco','Courier_New',monospace] text-sm overflow-y-auto bg-black cursor-text"
        style={{ height: `${containerHeightPx}px` }}
      >
        {/* Welcome message */}
        {history.length === 0 && !isProcessing && (
          <div className="text-gray-500 mb-4">
            <div>Last login: {new Date().toLocaleString()} on ttys001</div>
            <div className="mt-2 text-gray-600">{placeholder}</div>
            <div className="text-gray-600">• Use ↑↓ to navigate command history</div>
            <div className="text-gray-600">• Press Ctrl+C to cancel a command</div>
            <div className="text-gray-600">• Press Ctrl+L to clear the terminal</div>
          </div>
        )}

        {/* Command History */}
        {history.map((entry, index) => (
          <div key={index} className="mb-2">
            {/* Command prompt and input */}
            <div className="flex items-start">
              <span className="text-green-400 mr-2">{username}@{hostname}</span>
              <span className="text-cyan-400 mr-2">{currentPath}</span>
              <span className="text-yellow-400 mr-2">{entry.command ? '$ ' : ''}</span>
              <span className="text-white">{entry.command}</span>
            </div>
            
            {/* Command output */}
            {entry.output.map((line, lineIndex) => (
              <div 
                key={lineIndex} 
                className={`mt-1 ${
                  line.startsWith('Error:') || line.startsWith('ERROR') || line === '^C'
                    ? 'text-red-400'
                    : line.startsWith('Warning:') || line.startsWith('WARN')
                    ? 'text-yellow-400'
                    : line.startsWith('Success:') || line.startsWith('✓')
                    ? 'text-green-400'
                    : line.startsWith('#') || line.startsWith('//')
                    ? 'text-gray-500'
                    : 'text-gray-300'
                }`}
              >
                {line}
              </div>
            ))}
            
            {/* Streaming indicator */}
            {entry.isStreaming && (
              <span className="inline-block w-2 h-4 bg-white animate-pulse mt-1"></span>
            )}
          </div>
        ))}

        {/* Current Input Line */}
        {!isProcessing && (
          <div className="flex items-start">
            <span className="text-green-400 mr-2">{username}@{hostname}</span>
            <span className="text-cyan-400 mr-2">{currentPath}</span>
            <span className="text-yellow-400 mr-2">{prompt}</span>
            <div className="flex-1 relative">
              <input
                ref={inputRef}
                type="text"
                value={currentCommand}
                onChange={(e) => setCurrentCommand(e.target.value)}
                onKeyDown={handleKeyDown}
                className="bg-transparent text-white outline-none w-full"
                autoFocus
                spellCheck={false}
                autoComplete="off"
                autoCorrect="off"
                autoCapitalize="off"
              />
              <span className="inline-block w-2 h-4 bg-white animate-pulse absolute top-0 -right-3"></span>
            </div>
          </div>
        )}

        {/* Processing indicator */}
        {isProcessing && (
          <div className="flex items-start text-gray-500">
            <span className="mr-2">Processing</span>
            <span className="animate-pulse">...</span>
          </div>
        )}
      </CardContent>
      {/* Vertical resize handle */}
      <div
        role="separator"
        aria-orientation="horizontal"
        onMouseDown={onResizerMouseDown}
        className="h-3 bg-gray-800/60 hover:bg-gray-700/70 border-t border-gray-700 cursor-ns-resize select-none flex items-center justify-center"
        title="Drag to resize"
      >
        <div className="w-10 h-0.5 bg-gray-500 rounded-full" />
      </div>
    </Card>
  );
};

export default InteractiveTerminalCardMac;