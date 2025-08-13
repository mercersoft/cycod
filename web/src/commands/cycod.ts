import type { CommandDefinition } from './types'
import { version, testChat, initializeChat, sendMessage, sendMessageStreaming, clearChatHistory, getChatStatus, saveChatHistory, loadChatHistory } from '@/cycodblazor'

let chatActive = false
let chatInitialized = false

async function handleChatCommand(args: string[], addOutput: (text: string) => void, endOutput: () => void) {
  // Basic debug output to verify function is called
  addOutput(`DEBUG: handleChatCommand called with args: ${JSON.stringify(args)}`)
  console.log('handleChatCommand called with args:', args)
  
  try {
    // Parse chat subcommands
    const subCommand = args[0]
    addOutput(`DEBUG: subCommand = "${subCommand}"`)
    
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
      addOutput('DEBUG: Starting --test command')
      console.log('Starting --test command')
      try {
        addOutput('DEBUG: About to call testChat()')
        const result = await testChat()
        addOutput(`DEBUG: testChat() returned: ${JSON.stringify(result)}`)
        if (result.Success) {
          addOutput('✓ Chat connectivity test passed')
          addOutput(`Message: ${result.Message}`)
        } else {
          addOutput('✗ Chat connectivity test failed')
          addOutput(`Error: ${result.Error}`)
        }
      } catch (error) {
        addOutput('✗ Chat connectivity test failed')
        addOutput(`DEBUG: Exception in --test: ${error}`)
        console.error('Test command error:', error)
      }
      endOutput()
      return
    }
    
    if (subCommand === '--status') {
      try {
        addOutput('Calling getChatStatus...')
        const status = await getChatStatus()
        addOutput(`Status response: ${JSON.stringify(status)}`)
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
        addOutput(`Exception in --status: ${error}`)
        console.error('Status command error:', error)
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
        addOutput('Calling initializeChat...')
        const initResult = await initializeChat(
          'You are a helpful AI assistant. Provide clear, concise, and accurate responses.'
        )
        addOutput(`InitializeChat result: ${JSON.stringify(initResult)}`)
        if (!initResult.Success) {
          addOutput(`Error initializing chat: ${initResult.Error || 'Unknown error'}`)
          endOutput()
          return
        }
        chatInitialized = true
        addOutput('Chat initialized successfully!')
      } catch (error) {
        addOutput(`Exception during initialization: ${error}`)
        console.error('Chat initialization error:', error)
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
    addOutput(`Outer catch - Error: ${error}`)
    console.error('handleChatCommand outer error:', error)
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
    } else {
      addOutput('CycoAI CLI v2.0.1')
      setTimeout(() => addOutput('Usage: cyco [command] [options]'), 100)
      setTimeout(() => addOutput(''), 200)
      setTimeout(() => addOutput('Commands:'), 300)
      setTimeout(() => addOutput('  chat     Start chat'), 400)
      setTimeout(() => addOutput('  --version  Show version'), 500)
      setTimeout(() => endOutput(), 700)
    }
  }
}