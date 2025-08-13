export type CommandHandler = (
  words: string[],
  addOutput: (text: string) => void,
  endOutput: () => void
) => void

export interface CommandMetadata {
  name: string
  description: string
  usage?: string
  examples?: string[]
  aliases?: string[]
}

export interface CommandDefinition {
  handler: CommandHandler
  metadata: CommandMetadata
}

export type CommandRegistry = Record<string, CommandDefinition>