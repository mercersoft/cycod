import type { CommandDefinition } from './types'
import { version } from '@/cycodblazor'

export const cycodCommand: CommandDefinition = {
  metadata: {
    name: 'cycod',
    description: 'Run CycoAI CLI commands',
    usage: 'cycod [command] [options]',
    examples: ['cycod --version', 'cycod chat', 'cycod init', 'cycod deploy'],
    aliases: ['cyco']
  },
  handler: (words, addOutput, endOutput) => {
    if (words[1] === '--version' || words[1] === '-v' || words[1] === 'version') {
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
    } else if (words[1] === 'chat') {
      addOutput('Starting chat...')
      setTimeout(() => endOutput(), 300)
    } else {
      addOutput('CycoAI CLI v2.0.1')
      setTimeout(() => addOutput('Usage: cyco [command] [options]'), 100)
      setTimeout(() => addOutput(''), 200)
      setTimeout(() => addOutput('Commands:'), 300)
      setTimeout(() => addOutput('  chat     Start chat'), 400)
      setTimeout(() => addOutput('  --version  Show version'), 500)
      setTimeout(() => endOutput(), 700)
    }
  }
}