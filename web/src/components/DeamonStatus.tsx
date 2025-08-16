import { useEffect, useState } from "react"

interface StatusState {
  status: 'checking' | 'connected' | 'error'
  message: string
  version?: string
}

export default function DeamonStatus() {
  const [state, setState] = useState<StatusState>({
    status: 'checking',
    message: 'checking ....'
  })

  useEffect(() => {
    let ws: WebSocket | null = null
    let timeoutId: NodeJS.Timeout | null = null

    const connect = () => {
      try {
        // Set a timeout for the connection attempt
        timeoutId = setTimeout(() => {
          if (ws && ws.readyState === WebSocket.CONNECTING) {
            ws.close()
            setState({
              status: 'error',
              message: 'Connection timeout - daemon may not be running'
            })
          }
        }, 5000)

        ws = new WebSocket('ws://localhost:6464/ws')

        ws.onopen = () => {
          console.log('WebSocket connected')
        }

        ws.onmessage = (event) => {
          try {
            const data = JSON.parse(event.data)
            console.log('Received:', data)

            switch (data.type) {
              case 'challenge':
                // Authenticate with empty token (no auth required by default)
                ws?.send(JSON.stringify({
                  type: 'authenticate',
                  token: ''
                }))
                break

              case 'authenticated':
                // Authentication successful, request version
                ws?.send(JSON.stringify({
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
                  version: data.version
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

        ws.onerror = (error) => {
          console.error('WebSocket error:', error)
          setState({
            status: 'error',
            message: 'Connection failed - daemon may not be running'
          })
        }

        ws.onclose = (event) => {
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
              message: errorMessage
            })
          }
        }

      } catch (error) {
        console.error('Failed to create WebSocket:', error)
        setState({
          status: 'error',
          message: 'Failed to connect to daemon'
        })
      }
    }

    connect()

    // Cleanup function
    return () => {
      if (timeoutId) {
        clearTimeout(timeoutId)
      }
      if (ws) {
        ws.close()
      }
    }
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

  return (
    <div className="flex items-center gap-2 text-white">
      <span className="font-medium">Status:</span>
      <span className={getStatusColor()}>
        {state.message}
      </span>
    </div>
  )
}
