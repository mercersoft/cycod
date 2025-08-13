import type { CommandDefinition } from './types'

export const clearCommand: CommandDefinition = {
  metadata: {
    name: 'clear',
    description: 'Clear the terminal (or Ctrl+L)',
    usage: 'clear',
    examples: ['clear']
  },
  handler: (_words, addOutput, endOutput) => {
    addOutput('Use Ctrl+L to clear the terminal')
    endOutput()
  }
}