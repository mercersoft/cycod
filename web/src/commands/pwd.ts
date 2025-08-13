import type { CommandDefinition } from './types'

export const pwdCommand: CommandDefinition = {
  metadata: {
    name: 'pwd',
    description: 'Print working directory',
    usage: 'pwd',
    examples: ['pwd']
  },
  handler: (words, addOutput, endOutput) => {
    addOutput('/Users/' + 'user' + '/projects')
    endOutput()
  }
}