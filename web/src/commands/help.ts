import type { CommandDefinition } from './types'

export const helpCommand: CommandDefinition = {
  metadata: {
    name: 'help',
    description: 'Show this help message',
    usage: 'help [command]',
    examples: ['help', 'help ls', 'help echo']
  },
  handler: (_words, addOutput, endOutput) => {
    // This will be updated in index.ts to use the actual command registry
    addOutput('Available commands:')
    setTimeout(() => addOutput('  help     - Show this help message'), 100)
    setTimeout(() => addOutput('  ls       - List directory contents'), 200)
    setTimeout(() => addOutput('  pwd      - Print working directory'), 300)
    setTimeout(() => addOutput('  echo     - Echo a message'), 400)
    setTimeout(() => addOutput('  date     - Show current date'), 500)
    setTimeout(() => addOutput('  clear    - Clear the terminal (or Ctrl+L)'), 600)
    setTimeout(() => addOutput('  cycod    - Run CycoAI CLI commands'), 700)
    setTimeout(() => endOutput(), 800)
  }
}