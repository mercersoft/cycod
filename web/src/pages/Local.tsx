import Header from "@/components/Header"
import StarfieldBackground from "@/components/StarfieldBackground"
import DeamonStatus from "@/components/DeamonStatus"
import InteractiveTerminalCardMac from "@/components/InteractiveTerminalCardMac"
import { WebSocketProvider, useWebSocket } from "@/context/WebSocketContext"

function LocalContent() {
  const context = useWebSocket()
  const { executeCommand, isConnected, status } = context
  
  console.log('🐛 LocalContent render:', { 
    isConnected, 
    status: status.status,
    contextInstance: context 
  })

  return (
    <>
      <Header />
      <main className="min-h-screen pt-24 text-white">
        <div className="max-w-4xl mx-auto px-4 py-20">
          <h1 className="text-3xl md:text-4xl font-mono text-green-400">Local CycoDev</h1>
          <p className="text-gray-300 mt-4">
            Execute commands locally through the daemon. 
            {isConnected ? (
              <span className="text-green-400"> Connected to daemon.</span>
            ) : (
              <span className="text-red-400"> Not connected to daemon.</span>
            )}
          </p>

          <div className="mt-8 mb-8">
            <DeamonStatus />
          </div>

          <div className="mt-8">
            <InteractiveTerminalCardMac
              title="CycoDev Local Terminal"
              username="you"
              hostname="local"
              currentPath="~/projects"
              onCommand={executeCommand}
              placeholder="Type 'cycod config list' or 'help' and press Enter…"
            />
          </div>
        </div>

        {/* Instructions */}
        <div className="max-w-4xl mx-auto px-4 pb-16 text-gray-400 text-sm space-y-2 font-mono">
          <h3 className="text-white font-semibold mb-2">Local Terminal Features:</h3>
          <ul className="space-y-1">
            <li>• Commands are executed through the local daemon</li>
            <li>• Use ↑/↓ arrow keys to navigate command history</li>
            <li>• Press Ctrl+C to cancel a running command</li>
            <li>• Press Ctrl+L to clear the terminal</li>
            <li>• Click anywhere in the terminal to focus input</li>
          </ul>
          <h3 className="text-white font-semibold mt-4 mb-2">Supported Commands:</h3>
          <ul className="space-y-1">
            <li>• <span className="text-cyan-400">cycod config list</span> - List all configuration settings</li>
            <li>• <span className="text-cyan-400">cycod config get &lt;key&gt;</span> - Get a configuration value</li>
            <li>• <span className="text-cyan-400">cycod config set &lt;key&gt; &lt;value&gt;</span> - Set a configuration value</li>
            <li>• <span className="text-cyan-400">cycod config clear &lt;key&gt;</span> - Clear a configuration value</li>
            <li>• <span className="text-cyan-400">cycod config add &lt;key&gt; &lt;value&gt;</span> - Add to a configuration list</li>
            <li>• <span className="text-cyan-400">cycod config remove &lt;key&gt; &lt;value&gt;</span> - Remove from a configuration list</li>
          </ul>
        </div>
      </main>
    </>
  )
}

export default function Local() {
  return (
    <StarfieldBackground>
      <WebSocketProvider>
        <LocalContent />
      </WebSocketProvider>
    </StarfieldBackground>
  )
}
