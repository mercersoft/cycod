import type { CommandDefinition } from './types'

export const dateCommand: CommandDefinition = {
  metadata: {
    name: 'date',
    description: 'Show current date',
    usage: 'date',
    examples: ['date']
  },
  handler: (words, addOutput, endOutput) => {
    addOutput(new Date().toString())
    endOutput()
  }
}