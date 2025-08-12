import React, { useState, useEffect } from 'react';
import { Card, CardContent } from '@/components/ui/card';

interface Command {
  command: string;
  output: string[];
}

interface TerminalCardPSProps {
  title: string;
  commands: Command[];
  delay?: number;
  commandDelay?: number;
  currentPath?: string;
}

const TerminalCardPS: React.FC<TerminalCardPSProps> = ({ 
  title, 
  commands,
  delay = 200,
  commandDelay = 500,
  currentPath = "C:\\Users\\Developer\\Projects"
}) => {
  const [currentCommandIndex, setCurrentCommandIndex] = useState<number>(0);
  const [currentLineIndex, setCurrentLineIndex] = useState<number>(0);
  const [isTypingCommand, setIsTypingCommand] = useState<boolean>(true);
  const [typedCommand, setTypedCommand] = useState<string>('');
  
  const currentCommand = commands[currentCommandIndex];
  const isComplete = currentCommandIndex >= commands.length;
  
  useEffect(() => {
    if (isComplete) return;
    
    if (isTypingCommand) {
      // Type out the command character by character
      const commandText = currentCommand.command;
      if (typedCommand.length < commandText.length) {
        const timer = setTimeout(() => {
          setTypedCommand(commandText.slice(0, typedCommand.length + 1));
        }, 30);
        return () => clearTimeout(timer);
      } else {
        // Command fully typed, start showing output after a delay
        const timer = setTimeout(() => {
          setIsTypingCommand(false);
        }, commandDelay);
        return () => clearTimeout(timer);
      }
    } else {
      // Show output lines
      if (currentLineIndex < currentCommand.output.length) {
        const timer = setTimeout(() => {
          setCurrentLineIndex(prev => prev + 1);
        }, delay);
        return () => clearTimeout(timer);
      } else {
        // Move to next command after a pause
        const timer = setTimeout(() => {
          if (currentCommandIndex < commands.length - 1) {
            setCurrentCommandIndex(prev => prev + 1);
            setCurrentLineIndex(0);
            setIsTypingCommand(true);
            setTypedCommand('');
          } else {
            setCurrentCommandIndex(commands.length);
          }
        }, commandDelay);
        return () => clearTimeout(timer);
      }
    }
  }, [currentCommandIndex, currentLineIndex, isTypingCommand, typedCommand, commands, delay, commandDelay, isComplete]);
  
  return (
    <Card className="bg-black border-gray-800 overflow-hidden shadow-2xl">
      {/* PowerShell Header Bar - Dark Theme */}
      <div className="bg-gray-900 px-3 py-1.5 flex items-center justify-between border-b border-gray-800">
        <div className="flex items-center space-x-2">
          <div className="w-4 h-4">
            <svg viewBox="0 0 16 16" className="w-full h-full">
              <path fill="#0078d4" d="M0 0h7v7H0zM9 0h7v7H9zM0 9h7v7H0zM9 9h7v7H9z"/>
            </svg>
          </div>
          <span className="text-sm text-gray-300 font-['Segoe UI',system-ui,sans-serif]">
            {title}
          </span>
        </div>
        <div className="flex items-center space-x-1">
          <button className="w-7 h-7 hover:bg-gray-700 flex items-center justify-center rounded">
            <span className="text-gray-400">─</span>
          </button>
          <button className="w-7 h-7 hover:bg-gray-700 flex items-center justify-center rounded">
            <span className="text-gray-400">□</span>
          </button>
          <button className="w-7 h-7 hover:bg-red-600 hover:text-white flex items-center justify-center rounded">
            <span className="text-gray-400">✕</span>
          </button>
        </div>
      </div>
      
      {/* PowerShell Content - Dark Theme */}
      <CardContent className="p-4 font-['Consolas','Courier_New',monospace] text-sm max-h-96 overflow-y-auto bg-black">
        {/* PowerShell Header */}
        <div className="text-gray-400 mb-3">
          <div>Windows PowerShell</div>
          <div className="text-xs">Copyright (C) Microsoft Corporation. All rights reserved.</div>
          <div className="text-xs mb-2">Try the new cross-platform PowerShell https://aka.ms/pscore6</div>
        </div>
        
        <div className="space-y-2">
          {commands.slice(0, currentCommandIndex + 1).map((cmd, cmdIndex) => (
            <div key={cmdIndex}>
              {/* PowerShell Prompt - Dark Theme */}
              <div className="flex items-start">
                <span className="text-yellow-400">PS {currentPath}&gt;</span>
                <span className="text-gray-100 ml-2">
                  {cmdIndex === currentCommandIndex 
                    ? typedCommand 
                    : cmd.command
                  }
                  {cmdIndex === currentCommandIndex && isTypingCommand && typedCommand.length < cmd.command.length && (
                    <span className="inline-block w-2 h-4 bg-gray-100 animate-pulse ml-0.5"></span>
                  )}
                </span>
              </div>
              
              {/* Output Lines */}
              {(cmdIndex < currentCommandIndex || (cmdIndex === currentCommandIndex && !isTypingCommand)) && (
                <div className="space-y-0.5 mt-1 ml-0">
                  {cmd.output.slice(0, cmdIndex === currentCommandIndex ? currentLineIndex : cmd.output.length).map((line, lineIndex) => (
                    <div key={lineIndex} className={`leading-relaxed ${
                      // PowerShell specific coloring - Dark Theme
                      line.startsWith('SUCCESS:') || line.includes('successfully') || line.includes('✓')
                        ? 'text-green-400' 
                        : line.startsWith('WARNING:') || line.startsWith('VERBOSE:')
                        ? 'text-yellow-400'
                        : line.startsWith('ERROR:') || line.includes('cannot') || line.includes('failed')
                        ? 'text-red-400'
                        : line.startsWith('DEBUG:') || line.startsWith('#')
                        ? 'text-cyan-400'
                        : line.startsWith('    ') || line.startsWith('  ')
                        ? 'text-gray-500'
                        : line.includes('Name') || line.includes('----') || line.includes('Mode')
                        ? 'text-gray-600'
                        : 'text-gray-300'
                    }`}>
                      {line}
                    </div>
                  ))}
                  
                  {/* Cursor for output */}
                  {cmdIndex === currentCommandIndex && !isTypingCommand && currentLineIndex < cmd.output.length && (
                    <span className="inline-block w-2 h-4 bg-gray-100 animate-pulse"></span>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
};

export default TerminalCardPS;