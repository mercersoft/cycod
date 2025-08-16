import { useEffect, useState } from "react"

interface StatusState {
  status: 'checking' | 'connected' | 'error'
  message: string
  version?: string
  hasServer?: boolean
}

export default function DeamonStatus() {
  const [state, setState] = useState<StatusState>({
    status: 'checking',
    message: 'checking ....'
  })
  const [ws, setWs] = useState<WebSocket | null>(null)

  const handleButtonClick = () => {
    if (state.status === 'connected' && ws) {
      // Disconnect
      ws.close()
      setWs(null)
      setState({
        status: 'error',
        message: 'Disconnected by user',
        hasServer: true
      })
    } else if (state.hasServer || state.status === 'checking') {
      // Connect/Refresh
      connectToServer()
    }
  }

  const connectToServer = () => {
    setState({
      status: 'checking',
      message: 'checking ....'
    })
    
    // Trigger a reconnection by calling the connect logic
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
                  hasServer: true
                })
                break

              case 'error':
                setState({
                  status: 'error',
                  message: `Server error: ${data.message}`
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
          if (state.status === 'checking') {
            let errorMessage = 'Connection closed - daemon may not be running'
            
            // Provide more specific error messages based on close codes
            if (event.code === 1006) {
              errorMessage = 'Connection failed - daemon may not be running or origin not allowed'
            } else if (event.code === 1002) {
              errorMessage = 'Protocol error - check daemon configuration'
            } else if (event.code === 1003) {
              errorMessage = 'Invalid data received from daemon'
            }
            
            setState({
              status: 'error',
              message: errorMessage,
              hasServer: false
            })
          }
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
        return 'text-green-400'
      case 'error':
        return 'text-red-400'
      default:
        return 'text-gray-300'
    }
  }

  const getButtonText = () => {
    if (state.status === 'connected') {
      return 'Disconnect'
    } else if (state.hasServer || state.status === 'checking') {
      return state.status === 'checking' ? 'Connecting...' : 'Connect'
    } else {
      return 'Refresh'
    }
  }

  const getButtonStyle = () => {
    const baseStyle = "px-3 py-1 text-sm rounded border transition-colors duration-200"
    
    if (state.status === 'connected') {
      return `${baseStyle} border-red-500 text-red-400 hover:bg-red-500 hover:text-white`
    } else if (state.hasServer || state.status === 'checking') {
      return `${baseStyle} border-green-500 text-green-400 hover:bg-green-500 hover:text-white ${state.status === 'checking' ? 'opacity-50 cursor-not-allowed' : ''}`
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
