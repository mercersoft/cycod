import type { CommandRegistry, CommandHandler } from './types'
import { helpCommand } from './help'
import { lsCommand } from './ls'
import { pwdCommand } from './pwd'
import { echoCommand } from './echo'
import { dateCommand } from './date'
import { clearCommand } from './clear'
import { cycodCommand } from './cycod'

// Build the command registry
export const commands: CommandRegistry = {
  help: helpCommand,
  ls: lsCommand,
  pwd: pwdCommand,
  echo: echoCommand,
  date: dateCommand,
  clear: clearCommand,
  cycod: cycodCommand,
  cyco: cycodCommand // alias for cycod
}

// Generate help text dynamically from command metadata
export function generateHelpText(addOutput: (text: string) => void, endOutput: () => void) {
  addOutput('Available commands:')
  
  const commandList = Object.values(commands)
    // Remove duplicates (aliases point to same command object)
    .filter((cmd, index, self) => self.indexOf(cmd) === index)
    .map(cmd => cmd.metadata)
    .sort((a, b) => a.name.localeCompare(b.name))
  
  commandList.forEach((metadata, index) => {
    const paddedName = metadata.name.padEnd(8)
    setTimeout(() => {
      addOutput(`  ${paddedName} - ${metadata.description}`)
    }, (index + 1) * 100)
  })
  
  setTimeout(() => endOutput(), (commandList.length + 1) * 100)
}

// Update help command to use the dynamic help generator
helpCommand.handler = (words, addOutput, endOutput) => {
  if (words[1]) {
    // Show help for specific command
    const cmd = commands[words[1]]
    if (cmd) {
      const meta = cmd.metadata
      addOutput(`${meta.name} - ${meta.description}`)
      if (meta.usage) {
        addOutput(`Usage: ${meta.usage}`)
      }
      if (meta.examples && meta.examples.length > 0) {
        addOutput('Examples:')
        meta.examples.forEach(example => {
          addOutput(`  ${example}`)
        })
      }
      if (meta.aliases && meta.aliases.length > 0) {
        addOutput(`Aliases: ${meta.aliases.join(', ')}`)
      }
      endOutput()
    } else {
      addOutput(`No help available for: ${words[1]}`)
      endOutput()
    }
  } else {
    // Show general help
    generateHelpText(addOutput, endOutput)
  }
}

// Main command handler that delegates to specific commands
export function handleCommand(
  command: string,
  addOutput: (text: string) => void,
  endOutput: () => void
): void {
  const words = command.toLowerCase().trim().split(' ')
  const cmd = words[0]
  
  const commandDef = commands[cmd]
  if (commandDef) {
    commandDef.handler(words, addOutput, endOutput)
  } else {
    // Handle unknown command
    addOutput(`Error: Command not found: ${cmd}`)
    setTimeout(() => addOutput(`Try 'help' for a list of available commands`), 100)
    setTimeout(() => endOutput(), 200)
  }
}