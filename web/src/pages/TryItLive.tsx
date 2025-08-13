import Header from "@/components/Header"
import StarfieldBackground from "@/components/StarfieldBackground"
import InteractiveTerminalCardMac from "@/components/InteractiveTerminalCardMac"
import { handleCommand } from "@/commands"

export default function TryItLive() {

  return (
    <StarfieldBackground>
      <Header />
      <main className="min-h-screen pt-24 text-white">
        <div className="max-w-4xl mx-auto px-4 py-20">
          <h1 className="text-3xl md:text-4xl font-mono text-green-400">Try It Live</h1>
          <p className="text-gray-300 mt-4">You are using the same CLI code as you would <a href="/start" className="text-cyan-400">install on your own machine</a>!</p>
          <p className="text-gray-300 mt-4">Try simple commands like <code className="font-mono text-white">cycod --version</code> or <code className="font-mono text-white">cycod config list</code>.</p>

          <div className="mt-8">
            <InteractiveTerminalCardMac
              title="Interactive Terminal"
              username="you"
              hostname="MacBook-Pro"
              currentPath="~/cycod"
              onCommand={handleCommand}
              placeholder="Type 'help' or 'echo hello' and press Enter…"
            />
          </div>
        </div>
        {/* Instructions */}
        <div className="max-w-4xl mx-auto px-4 pb-16 text-gray-400 text-sm space-y-2 font-mono">
          <h3 className="text-white font-semibold mb-2">Interactive Terminal Features:</h3>
          <ul className="space-y-1">
            <li>• Type commands and press Enter to execute</li>
            <li>• Use ↑/↓ arrow keys to navigate command history</li>
            <li>• Press Ctrl+C to cancel a running command</li>
            <li>• Press Ctrl+L to clear the terminal</li>
            <li>• Click anywhere in the terminal to focus input</li>
            <li>• Commands stream output asynchronously</li>
          </ul>
          <h3 className="text-white font-semibold mt-4 mb-2">Example Commands:</h3>
          <ul className="space-y-1">
            <li>• <span className="text-cyan-400">help</span> - Show available commands</li>
            <li>• <span className="text-cyan-400">ls</span> - List files</li>
            <li>• <span className="text-cyan-400">pwd</span> - Show current directory</li>
            <li>• <span className="text-cyan-400">echo hello world</span> - Echo a message</li>
            <li>• <span className="text-cyan-400">cycod init</span> - Initialize a CycoAI project</li>
            <li>• <span className="text-cyan-400">cycod deploy</span> - Deploy to production</li>
            <li>• <span className="text-cyan-400">cycod --version</span> - calls the Blazor WebAssembly runtime!</li>
          </ul>
        </div>
      </main>
    </StarfieldBackground>
  )
}


