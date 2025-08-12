import React, { useState, useEffect } from 'react';
import { Card, CardContent } from '@/components/ui/card';

interface Command {
  command: string;
  output: string[];
}

interface TerminalCardProps {
  title: string;
  commands: Command[];
  delay?: number;
  commandDelay?: number;
}

const TerminalCardMac: React.FC<TerminalCardProps> = ({ 
  title, 
  commands,
  delay = 200,
  commandDelay = 500
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
    <Card className="bg-gray-950 border-gray-800 overflow-hidden">
      {/* Terminal Header Bar */}
      <div className="bg-gray-900 border-b border-gray-800 px-3 py-2 flex items-center space-x-2">
        <div className="w-3 h-3 rounded-full bg-red-500"></div>
        <div className="w-3 h-3 rounded-full bg-yellow-500"></div>
        <div className="w-3 h-3 rounded-full bg-green-500"></div>
        <span className="text-xs text-gray-500 ml-3 font-mono">{title}</span>
      </div>
      
      {/* Terminal Content */}
      <CardContent className="p-4 font-mono text-sm max-h-96 overflow-y-auto">
        <div className="space-y-2">
          {commands.slice(0, currentCommandIndex + 1).map((cmd, cmdIndex) => (
            <div key={cmdIndex}>
              {/* Command Line */}
              <div className="text-cyan-400">
                <span className="text-green-400">$</span> {
                  cmdIndex === currentCommandIndex 
                    ? typedCommand 
                    : cmd.command
                }
                {cmdIndex === currentCommandIndex && isTypingCommand && typedCommand.length < cmd.command.length && (
                  <span className="inline-block w-2 h-4 bg-cyan-400 animate-pulse ml-0.5"></span>
                )}
              </div>
              
              {/* Output Lines */}
              {(cmdIndex < currentCommandIndex || (cmdIndex === currentCommandIndex && !isTypingCommand)) && (
                <div className="space-y-0.5 mt-1">
                  {cmd.output.slice(0, cmdIndex === currentCommandIndex ? currentLineIndex : cmd.output.length).map((line, lineIndex) => (
                    <div key={lineIndex} className={`leading-relaxed ${
                      line.startsWith('✓') || line.startsWith('🚀') || line.startsWith('🌍') || line.startsWith('✅')
                        ? 'text-green-400' 
                        : line.startsWith('⚠') || line.startsWith('npm WARN')
                        ? 'text-yellow-400'
                        : line.startsWith('✗') || line.startsWith('ERROR')
                        ? 'text-red-400'
                        : line.startsWith('#')
                        ? 'text-gray-500'
                        : 'text-gray-300'
                    }`}>
                      {line}
                    </div>
                  ))}
                  
                  {/* Cursor for output */}
                  {cmdIndex === currentCommandIndex && !isTypingCommand && currentLineIndex < cmd.output.length && (
                    <span className="inline-block w-2 h-4 bg-cyan-400 animate-pulse"></span>
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

export default TerminalCardMac;