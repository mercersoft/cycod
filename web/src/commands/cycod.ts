import type { CommandDefinition } from './types'
import { version, testChat, initializeChat, sendMessage, sendMessageStreaming, clearChatHistory, getChatStatus, saveChatHistory, loadChatHistory, setConfig, getConfig, listConfig, clearConfig } from '@/cycodblazor'

let chatActive = false
let chatInitialized = false

// Function to mask API keys for security display
function maskApiKey(key: string, value: string): string {
  // Check if this looks like an API key (contains "key", "token", "secret", or "password")
  // Use more flexible matching for dotted notation like "openai.apikey"
  const isApiKey = /(key|token|secret|password)/i.test(key)
  
  if (isApiKey && value && value.length > 4) {
    // Show first 2 and last 2 characters, mask the rest
    const first2 = value.substring(0, 2)
    const last2 = value.substring(value.length - 2)
    const maskLength = Math.max(value.length - 4, 4) // At least 4 stars
    const masked = '*'.repeat(maskLength)
    return `${first2}${masked}${last2}`
  }
  
  // Return original value for non-sensitive keys
  return value
}

async function handleConfigCommand(args: string[], addOutput: (text: string) => void, endOutput: () => void) {
  try {
    const subCommand = args[0]
    
    if (subCommand === '--help' || subCommand === '-h') {
      addOutput('Config commands:')
      addOutput('  cycod config set KEY VALUE     Set configuration value')
      addOutput('  cycod config get KEY           Get configuration value')
      addOutput('  cycod config list              List all configuration')
      addOutput('  cycod config clear [KEY]       Clear specific key or all config')
      addOutput('')
      addOutput('Examples:')
      addOutput('  cycod config set ANTHROPIC_API_KEY sk-ant-1234...')
      addOutput('  cycod config set OPENAI_API_KEY sk-1234...')
      addOutput('  cycod config get ANTHROPIC_API_KEY')
      addOutput('  cycod config list')
      addOutput('  cycod config clear ANTHROPIC_API_KEY')
      endOutput()
      return
    }
    
    if (subCommand === 'set') {
      const key = args[1]
      const value = args[2]
      if (!key || !value) {
        addOutput('Error: Both key and value are required')
        addOutput('Usage: cycod config set KEY VALUE')
        endOutput()
        return
      }
      
      // Call the Blazor API to set config
      const result = await setConfig(key, value)
      if (result.Success) {
        addOutput(`Configuration set: ${key}`)
      } else {
        addOutput(`Error setting config: ${result.Error || 'Unknown error'}`)
      }
      endOutput()
      return
    }
    
    if (subCommand === 'get') {
      const key = args[1]
      if (!key) {
        addOutput('Error: Key is required')
        addOutput('Usage: cycod config get KEY')
        endOutput()
        return
      }
      
      // Call the Blazor API to get config
      const result = await getConfig(key)
      if (result.Success && result.Value) {
        addOutput(`${key}=${result.Value}`)
      } else {
        addOutput(`Configuration not found: ${key}`)
      }
      endOutput()
      return
    }
    
    if (subCommand === 'list') {
      // Call the Blazor API to list all config
      const result = await listConfig()
      if (result.Success && result.Items) {
        if (result.Items.length === 0) {
          addOutput('No configuration found')
        } else {
          addOutput('Configuration:')
          result.Items.forEach((item: any) => {
            const displayValue = maskApiKey(item.Key, item.Value)
            addOutput(`  ${item.Key}=${displayValue}`)
          })
        }
      } else {
        addOutput(`Error listing config: ${result.Error || 'Unknown error'}`)
      }
      endOutput()
      return
    }
    
    if (subCommand === 'clear') {
      const key = args[1]
      
      // Call the Blazor API to clear config
      const result = await clearConfig(key)
      if (result.Success) {
        if (key) {
          addOutput(`Configuration cleared: ${key}`)
        } else {
          addOutput('All configuration cleared')
        }
      } else {
        addOutput(`Error clearing config: ${result.Error || 'Unknown error'}`)
      }
      endOutput()
      return
    }
    
    // Unknown subcommand
    addOutput(`Unknown config command: ${subCommand}`)
    addOutput('Run "cycod config --help" for usage information')
    endOutput()
  } catch (error) {
    addOutput(`Error: ${error}`)
    endOutput()
  }
}

function showHelp(addOutput: (text: string) => void, versionString?: string) {
  addOutput(versionString ? `CycoAI CLI v${versionString}` : 'CycoAI CLI')
  addOutput('')
  addOutput('Usage: cycod [command] [options]')
  addOutput('')
  addOutput('Commands:')
  addOutput('  cycod --version, -v     Show version information')
  addOutput('  cycod chat              Start interactive chat mode')
  addOutput('  cycod chat --help       Show detailed chat help')
  addOutput('  cycod chat --test       Test chat connectivity')
  addOutput('  cycod chat --status     Show chat status and capabilities')
  addOutput('  cycod chat --clear      Clear chat history')
  addOutput('  cycod chat --save KEY   Save chat history with key')
  addOutput('  cycod chat --load KEY   Load chat history from key')
  addOutput('')
  addOutput('Examples:')
  addOutput('  cycod chat              # Start interactive chat')
  addOutput('  cycod chat --test       # Test if chat is working')
  addOutput('  cycod --version         # Show version')
}

async function handleChatCommand(args: string[], addOutput: (text: string) => void, endOutput: () => void) {
  try {
    // Parse chat subcommands
    const subCommand = args[0]
    
    if (subCommand === '--help' || subCommand === '-h') {
      addOutput('Chat commands:')
      addOutput('  cycod chat              Start interactive chat')
      addOutput('  cycod chat --test       Test chat connectivity')
      addOutput('  cycod chat --status     Check chat status')
      addOutput('  cycod chat --clear      Clear chat history')
      addOutput('  cycod chat --save KEY   Save chat history')
      addOutput('  cycod chat --load KEY   Load chat history')
      addOutput('')
      addOutput('In chat mode:')
      addOutput('  Type your message and press Enter to send')
      addOutput('  Type /exit or /quit to exit chat mode')
      addOutput('  Type /clear to clear history')
      addOutput('  Type /help for help')
      endOutput()
      return
    }
    
    if (subCommand === '--test') {
      try {
        const result = await testChat()
        if (result.Success) {
          addOutput('✓ Chat connectivity test passed')
          addOutput(`Message: ${result.Message}`)
        } else {
          addOutput('✗ Chat connectivity test failed')
          addOutput(`Error: ${result.Error}`)
        }
      } catch (error) {
        addOutput('✗ Chat connectivity test failed')
        addOutput(`Error: ${error}`)
      }
      endOutput()
      return
    }
    
    if (subCommand === '--status') {
      try {
        const status = await getChatStatus()
        if (status.Success) {
          addOutput(`Chat initialized: ${status.IsInitialized ? 'Yes' : 'No'}`)
          if (status.Capabilities) {
            addOutput('Capabilities:')
            addOutput(`  - Streaming: ${status.Capabilities.Streaming ? 'Yes' : 'No'}`)
            addOutput(`  - Function calling: ${status.Capabilities.FunctionCalling ? 'Yes' : 'No'}`)
            addOutput(`  - History persistence: ${status.Capabilities.HistoryPersistence ? 'Yes' : 'No'}`)
          }
          addOutput(`Version: ${status.Version}`)
        } else {
          addOutput(`Error: ${status.Error || 'Unknown error'}`)
        }
      } catch (error) {
        addOutput(`Error: ${error}`)
      }
      endOutput()
      return
    }
    
    if (subCommand === '--clear') {
      const result = await clearChatHistory()
      if (result.Success) {
        addOutput('Chat history cleared')
        chatInitialized = false
      } else {
        addOutput(`Error: ${result.Error || 'Failed to clear history'}`)
      }
      endOutput()
      return
    }
    
    if (subCommand === '--save') {
      const key = args[1]
      if (!key) {
        addOutput('Error: Please provide a key to save the chat history')
        addOutput('Usage: cycod chat --save KEY')
        endOutput()
        return
      }
      const result = await saveChatHistory(key)
      if (result.Success) {
        addOutput(`Chat history saved as "${key}"`)
      } else {
        addOutput(`Error: ${result.Error || 'Failed to save history'}`)
      }
      endOutput()
      return
    }
    
    if (subCommand === '--load') {
      const key = args[1]
      if (!key) {
        addOutput('Error: Please provide a key to load the chat history')
        addOutput('Usage: cycod chat --load KEY')
        endOutput()
        return
      }
      const result = await loadChatHistory(key)
      if (result.Success) {
        addOutput(`Chat history loaded from "${key}"`)
        chatInitialized = true
      } else {
        addOutput(`Error: ${result.Error || 'Failed to load history'}`)
      }
      endOutput()
      return
    }
    
    // Handle explicit 'start' subcommand or no subcommand (both start chat)
    if (subCommand === 'start' || !subCommand) {
      // Continue to chat initialization
    }
    
    // Start interactive chat mode
    if (!chatInitialized) {
      try {
        addOutput('Initializing chat...')
        const initResult = await initializeChat(
          'You are a helpful AI assistant. Provide clear, concise, and accurate responses.'
        )
        if (!initResult.Success) {
          const errorMessage = initResult.Error || 'Unknown error'
          // Split error message by newlines to preserve formatting
          const lines = errorMessage.split('\n')
          if (lines.length > 1) {
            // Multi-line error message, output each line separately
            lines.forEach(line => addOutput(line))
          } else {
            // Single line error message
            addOutput(`Error initializing chat: ${errorMessage}`)
          }
          endOutput()
          return
        }
        chatInitialized = true
        addOutput('Chat initialized successfully!')
      } catch (error) {
        addOutput(`Error during initialization: ${error}`)
        endOutput()
        return
      }
    }
    
    chatActive = true
    addOutput('Chat initialized. Type your message and press Enter.')
    addOutput('Commands: /exit, /quit (exit chat), /clear (clear history), /help')
    addOutput('')
    
    // Set up chat input handler
    const originalPrompt = (window as any).terminal?.getPrompt?.()
    if ((window as any).terminal) {
      (window as any).terminal.setPrompt('chat> ')
      
      // Override the command handler temporarily
      const originalHandler = (window as any).terminal.commandHandler
      ;(window as any).terminal.commandHandler = async (input: string) => {
        if (!chatActive) {
          // Restore original handler if chat is not active
          ;(window as any).terminal.commandHandler = originalHandler
          ;(window as any).terminal.setPrompt(originalPrompt || '$ ')
          originalHandler(input)
          return
        }
        
        input = input.trim()
        
        // Handle chat commands
        if (input === '/exit' || input === '/quit') {
          chatActive = false
          addOutput('Exiting chat mode.')
          ;(window as any).terminal.commandHandler = originalHandler
          ;(window as any).terminal.setPrompt(originalPrompt || '$ ')
          endOutput()
          return
        }
        
        if (input === '/clear') {
          const result = await clearChatHistory()
          if (result.Success) {
            addOutput('Chat history cleared')
            chatInitialized = false
            // Reinitialize chat
            const initResult = await initializeChat(
              'You are a helpful AI assistant. Provide clear, concise, and accurate responses.'
            )
            if (initResult.Success) {
              chatInitialized = true
              addOutput('Chat reinitialized')
            }
          } else {
            addOutput(`Error: ${result.Error || 'Failed to clear history'}`)
          }
          return
        }
        
        if (input === '/help') {
          addOutput('Chat commands:')
          addOutput('  /exit, /quit - Exit chat mode')
          addOutput('  /clear - Clear chat history')
          addOutput('  /help - Show this help')
          return
        }
        
        if (input === '') {
          return
        }
        
        // Send message to chat
        addOutput(`You: ${input}`)
        
        // Check if streaming is available
        const status = await getChatStatus()
        const useStreaming = status.Success && status.Capabilities?.Streaming
        
        if (useStreaming) {
          // Use streaming for real-time response
          let responseText = ''
          let isFirstChunk = true
          
          try {
            await sendMessageStreaming(
              input,
              (chunk: string) => {
                // Handle each chunk of the response
                if (isFirstChunk) {
                  addOutput('AI: ' + chunk)
                  isFirstChunk = false
                  responseText = chunk
                } else {
                  // For subsequent chunks, we'd ideally update the same line
                  // but since we can't do that easily, we'll just append
                  responseText += chunk
                  // Clear previous line and rewrite
                  addOutput('AI: ' + responseText)
                }
              },
              () => {
                // Streaming complete
                if (responseText === '') {
                  addOutput('AI: (No response)')
                }
              },
              (error: string) => {
                addOutput(`Error: ${error}`)
              },
              (functionName: string, _functionArgs: string | null, _functionResult: string | null) => {
                // Log function calls
                addOutput(`[Function called: ${functionName}]`)
              }
            )
          } catch (error) {
            addOutput(`Error sending message: ${error}`)
          }
        } else {
          // Fallback to non-streaming mode
          addOutput('AI: Thinking...')
          
          try {
            const response = await sendMessage(input)
            if (response.Success && response.Response) {
              // Clear the "Thinking..." message and show the response
              addOutput(`AI: ${response.Response}`)
            } else {
              addOutput(`Error: ${response.Error || 'No response received'}`)
            }
          } catch (error) {
            addOutput(`Error sending message: ${error}`)
          }
        }
      }
    } else {
      addOutput('Error: Terminal not available for interactive mode')
      endOutput()
    }
  } catch (error) {
    addOutput(`Error: ${error}`)
    endOutput()
  }
}

export const cycodCommand: CommandDefinition = {
  metadata: {
    name: 'cycod',
    description: 'Run CycoAI CLI commands',
    usage: 'cycod [command] [options]',
    examples: ['cycod --version', 'cycod chat', 'cycod init', 'cycod deploy'],
    aliases: ['cyco']
  },
  handler: (words, addOutput, endOutput) => {
    if (words[1] === '--version' || words[1] === '-v' || words[1] === 'version') {
      version()
        .then((v) => {
          addOutput(`CycoDev CLI v${v}`)
        })
        .catch((err) => {
          addOutput('Error: Unable to get version')
          const asString = (() => {
            try { return JSON.stringify(err) } catch { return String(err) }
          })()
          if (err && typeof err === 'object') {
            const anyErr = err as { message?: string; stack?: string }
            if (anyErr.message) addOutput(`message: ${anyErr.message}`)
            if (anyErr.stack) addOutput(`stack: ${anyErr.stack}`)
          }
          if (asString && asString !== '""') addOutput(`details: ${asString}`)
        })
        .finally(() => endOutput())
    } else if (words[1] === 'chat') {
      handleChatCommand(words.slice(2), addOutput, endOutput)
    } else if (words[1] === 'config') {
      handleConfigCommand(words.slice(2), addOutput, endOutput)
    } else if (words[1] === 'help' || words[1] === '--help' || words[1] === '-h') {
      // Show help with actual version and complete command list
      version()
        .then((v) => showHelp(addOutput, v))
        .catch(() => showHelp(addOutput))
        .finally(() => endOutput())
    } else {
      // Show help with actual version and complete command list
      version()
        .then((v) => showHelp(addOutput, v))
        .catch(() => showHelp(addOutput))
        .finally(() => endOutput())
    }
  }
}