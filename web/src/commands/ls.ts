import type { CommandDefinition } from './types'

export const lsCommand: CommandDefinition = {
  metadata: {
    name: 'ls',
    description: 'List directory contents',
    usage: 'ls [-la] [path]',
    examples: ['ls', 'ls -la', 'ls /home/user']
  },
  handler: (_words, addOutput, endOutput) => {
    addOutput('total 64')
    setTimeout(() => addOutput('drwxr-xr-x  12 user  staff   384 Jan 15 10:23 .'), 50)
    setTimeout(() => addOutput('drwxr-xr-x   7 user  staff   224 Jan 15 09:15 ..'), 100)
    setTimeout(() => addOutput('-rw-r--r--   1 user  staff  1432 Jan 15 10:23 README.md'), 150)
    setTimeout(() => addOutput('-rw-r--r--   1 user  staff   856 Jan 15 10:20 package.json'), 200)
    setTimeout(() => addOutput('drwxr-xr-x  10 user  staff   320 Jan 15 10:15 src'), 250)
    setTimeout(() => addOutput('drwxr-xr-x   5 user  staff   160 Jan 15 09:30 public'), 300)
    setTimeout(() => addOutput('drwxr-xr-x 425 user  staff 13600 Jan 15 10:10 node_modules'), 350)
    setTimeout(() => endOutput(), 400)
  }
}