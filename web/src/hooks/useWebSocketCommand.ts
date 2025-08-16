import { useEffect, useState, useCallback, useRef } from 'react'

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

export function useWebSocketCommand() {
  const [ws, setWs] = useState<WebSocket | null>(null)
  const [isConnected, setIsConnected] = useState(false)
  const pendingRequests = useRef<Map<string, {
    addOutput: (text: string) => void
    endOutput: () => void
  }>>(new Map())

  const connect = useCallback(() => {
    if (ws?.readyState === WebSocket.OPEN) {
      return // Already connected
    }

    try {
      const newWs = new WebSocket('ws://localhost:6464/ws')
      
      newWs.onopen = () => {
        console.log('WebSocket connected for commands')
      }

      newWs.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data)
          console.log('Command WebSocket received:', data)

          switch (data.type) {
            case 'challenge':
              // Authenticate with empty token
              newWs.send(JSON.stringify({
                type: 'authenticate',
                token: ''
              }))
              break

            case 'authenticated':
              setIsConnected(true)
              console.log('Command WebSocket authenticated')
              break

            case 'command-result':
              handleCommandResult(data as CommandResult)
              break

            case 'error':
              console.error('WebSocket command error:', data.message)
              break
          }
        } catch (error) {
          console.error('Failed to parse WebSocket message:', error)
        }
      }

      newWs.onerror = (error) => {
        console.error('WebSocket command error:', error)
        setIsConnected(false)
      }

      newWs.onclose = () => {
        console.log('WebSocket command connection closed')
        setIsConnected(false)
        setWs(null)
        
        // Reject any pending requests
        pendingRequests.current.forEach(({ addOutput, endOutput }) => {
          addOutput('Error: Connection lost')
          endOutput()
        })
        pendingRequests.current.clear()
      }

      setWs(newWs)
    } catch (error) {
      console.error('Failed to create WebSocket connection:', error)
      setIsConnected(false)
    }
  }, [ws])

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

  const handleCommand = useCallback((
    command: string,
    addOutput: (text: string) => void,
    endOutput: () => void
  ) => {
    if (!isConnected || !ws) {
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
    const requestId = Date.now().toString() + Math.random().toString(36).substr(2, 9)
    
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
      ws.send(JSON.stringify(commandRequest))
    } catch (error) {
      console.error('Failed to send command:', error)
      pendingRequests.current.delete(requestId)
      addOutput('Error: Failed to send command to daemon')
      setTimeout(endOutput, 100)
    }
  }, [isConnected, ws])

  // Auto-connect on mount
  useEffect(() => {
    connect()
    
    // Cleanup on unmount
    return () => {
      if (ws) {
        ws.close()
      }
    }
  }, [])

  // Reconnect when connection is lost
  useEffect(() => {
    if (!isConnected && (!ws || ws.readyState === WebSocket.CLOSED)) {
      const timer = setTimeout(connect, 2000)
      return () => clearTimeout(timer)
    }
  }, [isConnected, ws, connect])

  return {
    handleCommand,
    isConnected,
    connect
  }
}