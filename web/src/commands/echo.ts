import type { CommandDefinition } from './types'

export const echoCommand: CommandDefinition = {
  metadata: {
    name: 'echo',
    description: 'Echo a message',
    usage: 'echo [message]',
    examples: ['echo hello world', 'echo "Hello, World!"']
  },
  handler: (words, addOutput, endOutput) => {
    const message = words.slice(1).join(' ')
    addOutput(message)
    endOutput()
  }
}