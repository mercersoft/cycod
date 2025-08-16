import { useEffect, useState } from "react"

interface StatusState {
  status: 'checking' | 'connected' | 'started' | 'error'
  message: string
  version?: string
  hasServer?: boolean
  daemonState?: 'stopped' | 'running' | 'connected' | 'started' // Track daemon operational state
}

export default function DeamonStatus() {
  const [state, setState] = useState<StatusState>({
    status: 'checking',
    message: 'checking ....'
  })
  const [ws, setWs] = useState<WebSocket | null>(null)

  const handleButtonClick = () => {
    if (state.status === 'started' && ws) {
      // Send stop command to daemon
      ws.send(JSON.stringify({
        type: 'stop'
      }))
    } else if (state.status === 'connected' && ws) {
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
    setState({
      status: 'checking',
      message: 'checking ....',
      hasServer: undefined // Reset server detection
    })
    
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

  const initializeConnection = () => {
    let localWs: WebSocket | null = null
    let timeoutId: NodeJS.Timeout | null = null

    const connect = () => {
      try {
        // Set a timeout for the connection attempt
        timeoutId = setTimeout(() => {
          if (localWs && localWs.readyState === WebSocket.CONNECTING) {
            localWs.close()
            setState({
              status: 'error',
              message: 'Connection timeout - daemon may not be running',
              hasServer: false
            })
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
                break

              case 'version':
                if (timeoutId) {
                  clearTimeout(timeoutId)
                  timeoutId = null
                }
                setState({
                  status: 'connected',
                  message: data.version,
                  version: data.version,
                  hasServer: true,
                  daemonState: 'connected'
                })
                break

              case 'started':
                setState(prevState => ({
                  ...prevState,
                  status: 'started',
                  message: 'Daemon started and ready for operations',
                  daemonState: 'started'
                }))
                break

              case 'stopped':
                setState(prevState => ({
                  ...prevState,
                  status: 'connected',
                  message: 'Daemon stopped - ready to start',
                  daemonState: 'connected'
                }))
                break

              case 'error':
                setState({
                  status: 'error',
                  message: `Server error: ${data.message}`,
                  hasServer: true
                })
                break

              default:
                console.log('Unknown message type:', data.type)
            }
          } catch (error) {
            console.error('Failed to parse message:', error)
            setState({
              status: 'error',
              message: 'Invalid response from daemon'
            })
          }
        }

        localWs.onerror = (error) => {
          console.error('WebSocket error:', error)
          setState({
            status: 'error',
            message: 'Connection failed - daemon may not be running',
            hasServer: false
          })
        }

        localWs.onclose = (event) => {
          console.log('WebSocket closed:', event.code, event.reason)
          // Only update state if it was checking or connected, not if already in error
          setState(prevState => {
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
        }

      } catch (error) {
        console.error('Failed to create WebSocket:', error)
        setState({
          status: 'error',
          message: 'Failed to connect to daemon',
          hasServer: false
        })
      }
    }

    connect()

    // Cleanup function
    return () => {
      if (timeoutId) {
        clearTimeout(timeoutId)
      }
      if (localWs) {
        localWs.close()
      }
    }
  }

  useEffect(() => {
    initializeConnection()
  }, [])

  const getStatusColor = () => {
    switch (state.status) {
      case 'connected':
        return 'text-yellow-400'
      case 'started':
        return 'text-green-400'
      case 'error':
        return 'text-red-400'
      default:
        return 'text-gray-300'
    }
  }

  const getButtonText = () => {
    if (state.status === 'started') {
      return 'Stop'
    } else if (state.status === 'connected') {
      return 'Start'
    } else if (state.status === 'checking') {
      return 'Connecting...'
    } else {
      return 'Refresh'
    }
  }

  const getButtonStyle = () => {
    const baseStyle = "px-3 py-1 text-sm rounded border transition-colors duration-200"
    
    if (state.status === 'started') {
      return `${baseStyle} border-red-500 text-red-400 hover:bg-red-500 hover:text-white`
    } else if (state.status === 'connected') {
      return `${baseStyle} border-green-500 text-green-400 hover:bg-green-500 hover:text-white`
    } else if (state.status === 'checking') {
      return `${baseStyle} border-gray-500 text-gray-400 opacity-50 cursor-not-allowed`
    } else {
      return `${baseStyle} border-blue-500 text-blue-400 hover:bg-blue-500 hover:text-white`
    }
  }

  return (
    <div className="flex items-center gap-3 text-white">
      <button
        onClick={handleButtonClick}
        disabled={state.status === 'checking'}
        className={getButtonStyle()}
      >
        {getButtonText()}
      </button>
      <span className="font-medium">Status:</span>
      <span className={getStatusColor()}>
        {state.message}
      </span>
    </div>
  )
}
