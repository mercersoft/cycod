export interface TerminalAPI {
  setPrompt: (prompt: string) => void;
  getPrompt: () => string;
  commandHandler: (input: string) => void | Promise<void>;
}

declare global {
  interface Window {
    terminal?: TerminalAPI;
  }
}