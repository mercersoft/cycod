import Header from "@/components/Header"
import StarfieldBackground from "@/components/StarfieldBackground"
import InteractiveTerminalCardMac from "@/components/InteractiveTerminalCardMac"
import { version } from "@/cycodblazor"

export default function TryItLive() {
  function handleCommand(command: string, addOutput: (text: string) => void, endOutput: () => void) {
    const words = command.toLowerCase().trim().split(' ')
    const cmd = words[0]

    switch (cmd) {
      case 'help': {
        addOutput('Available commands:')
        setTimeout(() => addOutput('  help     - Show this help message'), 100)
        setTimeout(() => addOutput('  ls       - List directory contents'), 200)
        setTimeout(() => addOutput('  pwd      - Print working directory'), 300)
        setTimeout(() => addOutput('  echo     - Echo a message'), 400)
        setTimeout(() => addOutput('  date     - Show current date'), 500)
        setTimeout(() => addOutput('  clear    - Clear the terminal (or Ctrl+L)'), 600)
        setTimeout(() => addOutput('  cyco     - Run CycoAI CLI commands'), 700)
        setTimeout(() => endOutput(), 800)
        break
      }
      case 'ls': {
        addOutput('total 64')
        setTimeout(() => addOutput('drwxr-xr-x  12 user  staff   384 Jan 15 10:23 .'), 50)
        setTimeout(() => addOutput('drwxr-xr-x   7 user  staff   224 Jan 15 09:15 ..'), 100)
        setTimeout(() => addOutput('-rw-r--r--   1 user  staff  1432 Jan 15 10:23 README.md'), 150)
        setTimeout(() => addOutput('-rw-r--r--   1 user  staff   856 Jan 15 10:20 package.json'), 200)
        setTimeout(() => addOutput('drwxr-xr-x  10 user  staff   320 Jan 15 10:15 src'), 250)
        setTimeout(() => addOutput('drwxr-xr-x   5 user  staff   160 Jan 15 09:30 public'), 300)
        setTimeout(() => addOutput('drwxr-xr-x 425 user  staff 13600 Jan 15 10:10 node_modules'), 350)
        setTimeout(() => endOutput(), 400)
        break
      }
      case 'pwd': {
        addOutput('/Users/' + 'user' + '/projects')
        endOutput()
        break
      }
      case 'echo': {
        const message = words.slice(1).join(' ')
        addOutput(message)
        endOutput()
        break
      }
      case 'date': {
        addOutput(new Date().toString())
        endOutput()
        break
      }
      case 'clear': {
        addOutput('Use Ctrl+L to clear the terminal')
        endOutput()
        break
      }
      case 'cycod': {
        if (words[1] === 'init') {
          addOutput('✓ Creating project structure...')
          setTimeout(() => addOutput('✓ Installing dependencies...'), 500)
          setTimeout(() => addOutput('✓ Configuring AI models...'), 1000)
          setTimeout(() => addOutput('✓ Setting up development environment...'), 1500)
          setTimeout(() => addOutput(''), 2000)
          setTimeout(() => addOutput('🚀 Project ready! Run "cyco dev" to start'), 2100)
          setTimeout(() => endOutput(), 2200)
        } else if (words[1] === '--version' || words[1] === '-v' || words[1] === 'version') {
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
        } else if (words[1] === 'deploy') {
          addOutput('Building application...')
          setTimeout(() => addOutput('Optimizing bundles...'), 300)
          setTimeout(() => addOutput('Deploying to edge network...'), 800)
          setTimeout(() => addOutput('✓ Deployed to 32 global locations'), 1500)
          setTimeout(() => addOutput('✓ SSL certificates configured'), 1700)
          setTimeout(() => addOutput(''), 1900)
          setTimeout(() => addOutput('🌍 Live at: https://app.cyco.dev'), 2000)
          setTimeout(() => endOutput(), 2100)
        } else {
          addOutput('CycoAI CLI v2.0.1')
          setTimeout(() => addOutput('Usage: cyco [command] [options]'), 100)
          setTimeout(() => addOutput(''), 200)
          setTimeout(() => addOutput('Commands:'), 300)
          setTimeout(() => addOutput('  init     Initialize a new project'), 400)
          setTimeout(() => addOutput('  deploy   Deploy to production'), 500)
          setTimeout(() => addOutput('  dev      Start development server'), 600)
          setTimeout(() => endOutput(), 700)
        }
        break
      }
      default: {
        addOutput(`Error: Command not found: ${cmd}`)
        setTimeout(() => addOutput(`Try 'help' for a list of available commands`), 100)
        setTimeout(() => endOutput(), 200)
      }
    }
  }

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


