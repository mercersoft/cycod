import { createContext, useContext, useEffect, useState, useRef, useCallback } from 'react'
import type { ReactNode } from 'react'

interface CommandRequest {
  type: 'command'
  command: string
  args: string[]
  requestId: string
}

interface CommandResult {
  type: 'command-result'
  requestId: string
  stdout: string
  stderr: string
  exitCode: number
}

interface StatusState {
  status: 'checking' | 'connected' | 'started' | 'error'
  message: string
  version?: string
  hasServer?: boolean
  daemonState?: 'stopped' | 'running' | 'connected' | 'started'
}

interface WebSocketContextType {
  // Status management
  status: StatusState
  handleButtonClick: () => void
  connectToServer: () => void
  
  // Command execution
  executeCommand: (
    command: string,
    addOutput: (text: string) => void,
    endOutput: () => void
  ) => void
  
  // Connection state
  isConnected: boolean
}

const WebSocketContext = createContext<WebSocketContextType | undefined>(undefined)

// eslint-disable-next-line react-refresh/only-export-components
export function useWebSocket() {
  const context = useContext(WebSocketContext)
  if (!context) {
    throw new Error('useWebSocket must be used within a WebSocketProvider')
  }
  return context
}

interface WebSocketProviderProps {
  children: ReactNode
}

export function WebSocketProvider({ children }: WebSocketProviderProps) {
  const [status, setStatus] = useState<StatusState>({
    status: 'checking',
    message: 'checking ....'
  })
  const [ws, setWs] = useState<WebSocket | null>(null)
  const [isConnected, setIsConnected] = useState(false)
  
  // Use refs to avoid stale closure issues
  const wsRef = useRef<WebSocket | null>(null)
  const isConnectedRef = useRef(false)
  const statusRef = useRef(status)
  
  // Update refs whenever state changes
  wsRef.current = ws
  isConnectedRef.current = isConnected
  statusRef.current = status
  
  
  const pendingRequests = useRef<Map<string, {
    addOutput: (text: string) => void
    endOutput: () => void
  }>>(new Map())

  const handleButtonClick = () => {
    if (status.status === 'started' && ws) {
      // Send stop command to daemon
      ws.send(JSON.stringify({
        type: 'stop'
      }))
    } else if (status.status === 'connected' && ws) {
      // Send start command to daemon
      ws.send(JSON.stringify({
        type: 'start'
      }))
    } else {
      // Refresh - reconnect to check for server
      connectToServer()
    }
  }

  const connectToServer = () => {
    // Reset state to checking and clear server detection
    setStatus({
      status: 'checking',
      message: 'checking ....',
      hasServer: undefined // Reset server detection
    })
    setIsConnected(false)
    
    // Close existing connection if any
    if (ws) {
      ws.close()
      setWs(null)
    }
    
    // Small delay to ensure cleanup, then reconnect
    setTimeout(() => {
      initializeConnection()
    }, 100)
  }

  const initializeConnection = useCallback(() => {
    let localWs: WebSocket | null = null
    let timeoutId: NodeJS.Timeout | null = null

    const connect = () => {
      try {
        // Set a timeout for the connection attempt
        timeoutId = setTimeout(() => {
          if (localWs && localWs.readyState === WebSocket.CONNECTING) {
            localWs.close()
            setStatus({
              status: 'error',
              message: 'Connection timeout - daemon may not be running',
              hasServer: false
            })
            setIsConnected(false)
          }
        }, 5000)

        localWs = new WebSocket('ws://localhost:6464/ws')
        setWs(localWs)

        localWs.onopen = () => {
          console.log('WebSocket connected')
        }

        localWs.onmessage = (event) => {
          try {
            const data = JSON.parse(event.data)
            console.log('Received:', data)

            switch (data.type) {
              case 'challenge':
                // Authenticate with empty token (no auth required by default)
                localWs?.send(JSON.stringify({
                  type: 'authenticate',
                  token: ''
                }))
                break

              case 'authenticated':
                // Authentication successful, request version
                localWs?.send(JSON.stringify({
                  type: 'version'
                }))
                setIsConnected(true)
                break

              case 'version':
                if (timeoutId) {
                  clearTimeout(timeoutId)
                  timeoutId = null
                }
                setStatus({
                  status: 'connected',
                  message: data.version,
                  version: data.version,
                  hasServer: true,
                  daemonState: 'connected'
                })
                break

              case 'started':
                setStatus(prevState => ({
                  ...prevState,
                  status: 'started',
                  message: 'Daemon started and ready for operations',
                  daemonState: 'started'
                }))
                break

              case 'stopped':
                setStatus(prevState => ({
                  ...prevState,
                  status: 'connected',
                  message: 'Daemon stopped - ready to start',
                  daemonState: 'connected'
                }))
                break

              case 'command-result':
                handleCommandResult(data as CommandResult)
                break

              case 'error':
                setStatus({
                  status: 'error',
                  message: `Server error: ${data.message}`,
                  hasServer: true
                })
                setIsConnected(false)
                break

              default:
                console.log('Unknown message type:', data.type)
            }
          } catch (error) {
            console.error('Failed to parse message:', error)
            setStatus({
              status: 'error',
              message: 'Invalid response from daemon'
            })
            setIsConnected(false)
          }
        }

        localWs.onerror = (error) => {
          console.error('WebSocket error:', error)
          setStatus({
            status: 'error',
            message: 'Connection failed - daemon may not be running',
            hasServer: false
          })
          setIsConnected(false)
        }

        localWs.onclose = (event) => {
          console.log('WebSocket closed:', event.code, event.reason)
          setIsConnected(false)
          
          // Only update state if it was checking or connected, not if already in error
          setStatus(prevState => {
            if (prevState.status === 'checking' || prevState.status === 'connected' || prevState.status === 'started') {
              let errorMessage = 'Connection closed - daemon may not be running'
              
              // Provide more specific error messages based on close codes
              if (event.code === 1006) {
                errorMessage = 'Connection failed - daemon may not be running or origin not allowed'
              } else if (event.code === 1002) {
                errorMessage = 'Protocol error - check daemon configuration'
              } else if (event.code === 1003) {
                errorMessage = 'Invalid data received from daemon'
              }
              
              return {
                ...prevState,
                status: 'error',
                message: errorMessage,
                hasServer: false,
                daemonState: 'stopped'
              }
            }
            return prevState
          })

          // Reject any pending command requests
          pendingRequests.current.forEach(({ addOutput, endOutput }) => {
            addOutput('Error: Connection lost')
            endOutput()
          })
          pendingRequests.current.clear()
        }

      } catch (error) {
        console.error('Failed to create WebSocket:', error)
        setStatus({
          status: 'error',
          message: 'Failed to connect to daemon',
          hasServer: false
        })
        setIsConnected(false)
      }
    }

    connect()
  }, [])

  const handleCommandResult = (result: CommandResult) => {
    const request = pendingRequests.current.get(result.requestId)
    if (!request) {
      console.warn('Received result for unknown request:', result.requestId)
      return
    }

    const { addOutput, endOutput } = request
    pendingRequests.current.delete(result.requestId)

    // Output stderr first if present
    if (result.stderr) {
      const stderrLines = result.stderr.split('\n').filter(line => line.trim())
      stderrLines.forEach(line => addOutput(`Error: ${line}`))
    }

    // Output stdout
    if (result.stdout) {
      const stdoutLines = result.stdout.split('\n').filter(line => line.trim())
      stdoutLines.forEach(line => addOutput(line))
    }

    // Show exit code if non-zero
    if (result.exitCode !== 0) {
      addOutput(`Command exited with code: ${result.exitCode}`)
    }

    endOutput()
  }

  const executeCommand = useCallback((
    command: string,
    addOutput: (text: string) => void,
    endOutput: () => void
  ) => {
    // Use refs to get current values (not stale closure values)
    const currentIsConnected = isConnectedRef.current
    const currentWs = wsRef.current
    
    if (!currentIsConnected || !currentWs) {
      addOutput('Error: Not connected to daemon')
      addOutput('Please ensure the daemon is running and try again')
      setTimeout(endOutput, 100)
      return
    }

    const words = command.trim().split(' ')
    if (words.length === 0) {
      addOutput('Error: Empty command')
      setTimeout(endOutput, 100)
      return
    }

    // Check if this is a cycod command
    if (words[0] !== 'cycod') {
      addOutput(`Error: Only 'cycod' commands are supported`)
      addOutput(`Try: cycod config list`)
      setTimeout(endOutput, 100)
      return
    }

    // Validate config commands
    if (words.length < 2 || words[1] !== 'config') {
      addOutput(`Error: Only 'cycod config' commands are supported currently`)
      addOutput(`Try: cycod config list`)
      setTimeout(endOutput, 100)
      return
    }

    // Generate unique request ID
    const requestId = Date.now().toString() + Math.random().toString(36).substring(2, 11)
    
    // Store the request callbacks
    pendingRequests.current.set(requestId, { addOutput, endOutput })

    // Send command to server
    const commandRequest: CommandRequest = {
      type: 'command',
      command: words[0], // 'cycod'
      args: words.slice(1), // ['config', 'list', ...]
      requestId
    }

    addOutput(`Executing: ${command}`)
    
    try {
      currentWs.send(JSON.stringify(commandRequest))
    } catch (error) {
      console.error('Failed to send command:', error)
      pendingRequests.current.delete(requestId)
      addOutput('Error: Failed to send command to daemon')
      setTimeout(endOutput, 100)
    }
  }, []) // Use refs for state to avoid stale closures

  // Auto-connect on mount
  useEffect(() => {
    initializeConnection()
  }, [initializeConnection])

  const contextValue: WebSocketContextType = {
    status,
    handleButtonClick,
    connectToServer,
    executeCommand,
    isConnected
  }

  return (
    <WebSocketContext.Provider value={contextValue}>
      {children}
    </WebSocketContext.Provider>
  )
}