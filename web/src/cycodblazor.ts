// src/cycodHeadless.ts
declare global {
  interface Window {
    Blazor?: { start: (options?: unknown) => Promise<void> };
    DotNet?: {
      invokeMethodAsync: (assembly: string, method: string, ...args: any[]) => Promise<any>;
      createJSObjectReference?: (obj: any) => any;
    };
  }
}

let started: Promise<void> | null = null;
let starting = false;

async function ensureStarted() {
  if (!started) {
    if (starting) {
      // Another caller is initiating startup; wait for the shared promise
      await started;
      return;
    }
    starting = true;
    // If blazor.web*.js already loaded, start. Otherwise, try to load it on-demand
    if (window.Blazor?.start) {
      // Guard against double-start errors
      try {
        started = window.Blazor.start({
          loadBootResource: (_type: string, _name: string, defaultUri: string) => {
            // Force root-level _framework
            if (defaultUri.startsWith('/_framework/')) return defaultUri;
            if (defaultUri.startsWith('_framework/')) return '/' + defaultUri;
            return defaultUri;
          },
        });
      } catch (e: any) {
        const msg = typeof e?.message === 'string' ? e.message : ''
        if (msg.includes('already started')) {
          started = Promise.resolve();
        } else {
          starting = false;
          throw e;
        }
      }
    } else {
      started = new Promise<void>((resolve, reject) => {
        const candidates = [
          // Serve from root-level _framework only
          '/_framework/blazor.webassembly.js',
          '/_framework/blazor.web.js',
        ];

        let index = 0;
        const tryNext = () => {
          if (index >= candidates.length) {
            reject(new Error('Failed to load Blazor loader script (tried blazor.web(.assembly).js variants)'));
            return;
          }
          const src = candidates[index++];
          const script = document.createElement('script');
          script.src = src;
          script.async = true;
          script.onload = async () => {
            try {
              if (window.Blazor?.start) {
                await window.Blazor.start({
                  loadBootResource: (_type: string, _name: string, defaultUri: string) => {
                    if (defaultUri.startsWith('/_framework/')) return defaultUri;
                    if (defaultUri.startsWith('_framework/')) return '/' + defaultUri;
                    return defaultUri;
                  },
                });
              }
              resolve();
            } catch (e: any) {
              const msg = typeof e?.message === 'string' ? e.message : ''
              if (msg.includes('already started')) {
                resolve();
              } else {
                reject(e);
              }
            }
          };
          script.onerror = () => {
            // Try the next candidate
            script.remove();
            tryNext();
          };
          document.head.appendChild(script);
        };
        tryNext();
      });
    }
  }
  await started;
  starting = false;
  
  // Additional wait for call dispatcher to be ready
  await waitForCallDispatcher();
}

async function waitForCallDispatcher() {
  // Wait for the call dispatcher to be available
  for (let i = 0; i < 50; i++) { // Try for up to 5 seconds (50 * 100ms)
    try {
      if (window.DotNet && typeof window.DotNet.invokeMethodAsync === 'function') {
        // Test if call dispatcher is ready by calling a simple method
        await window.DotNet.invokeMethodAsync("cycodblazor", "Version");
        return; // Success!
      }
    } catch (e: unknown) {
      const msg = typeof (e as { message?: string })?.message === 'string' ? (e as { message: string }).message : ''
      if (!msg.includes('No call dispatcher has been set')) {
        // Different error, call dispatcher is ready but method might not exist yet
        return;
      }
    }
    await delay(100); // Wait 100ms before next attempt
  }
  throw new Error('Call dispatcher not ready after 5 seconds');
}

function delay(ms: number) { return new Promise((r) => setTimeout(r, ms)); }

export async function version(): Promise<string> {
  await ensureStarted();
  // Retry a few times to allow the call dispatcher to initialize after start
  let lastErr: unknown = undefined
  for (let i = 0; i < 10; i++) {
    try {
      if (!window.DotNet || typeof window.DotNet.invokeMethodAsync !== 'function') {
        throw new Error('DotNet.invokeMethodAsync not available yet');
      }
      return await window.DotNet.invokeMethodAsync("cycodblazor", "Version");
    } catch (e) {
      lastErr = e
      await delay(50)
    }
  }
  throw lastErr ?? new Error('Unknown error calling version()')
}

export async function testChat(): Promise<{ Success: boolean; Message?: string; Error?: string }> {
  await ensureStarted();
  let lastErr: unknown = undefined
  for (let i = 0; i < 10; i++) {
    try {
      if (!window.DotNet || typeof window.DotNet.invokeMethodAsync !== 'function') {
        throw new Error('DotNet.invokeMethodAsync not available yet');
      }
      const result = await window.DotNet.invokeMethodAsync("cycodblazor", "TestChat");
      return JSON.parse(result);
    } catch (e) {
      lastErr = e
      await delay(50)
    }
  }
  throw lastErr ?? new Error('Unknown error calling testChat()')
}

export async function initializeChat(systemPrompt: string, maxTokens?: number): Promise<{ Success: boolean; Message?: string; Error?: string }> {
  await ensureStarted();
  let lastErr: unknown = undefined
  for (let i = 0; i < 10; i++) {
    try {
      if (!window.DotNet || typeof window.DotNet.invokeMethodAsync !== 'function') {
        throw new Error('DotNet.invokeMethodAsync not available yet');
      }
      const result = await window.DotNet.invokeMethodAsync("cycodblazor", "InitializeChat", systemPrompt, maxTokens);
      return JSON.parse(result);
    } catch (e) {
      lastErr = e
      await delay(50)
    }
  }
  throw lastErr ?? new Error('Unknown error calling initializeChat()')
}

export async function sendMessage(message: string): Promise<{ Success: boolean; Response?: string; Error?: string }> {
  await ensureStarted();
  let lastErr: unknown = undefined
  for (let i = 0; i < 10; i++) {
    try {
      if (!window.DotNet || typeof window.DotNet.invokeMethodAsync !== 'function') {
        throw new Error('DotNet.invokeMethodAsync not available yet');
      }
      const result = await window.DotNet.invokeMethodAsync("cycodblazor", "SendMessage", message);
      return JSON.parse(result);
    } catch (e) {
      lastErr = e
      await delay(50)
    }
  }
  throw lastErr ?? new Error('Unknown error calling sendMessage()')
}

export async function clearChatHistory(): Promise<{ Success: boolean; Message?: string; Error?: string }> {
  await ensureStarted();
  let lastErr: unknown = undefined
  for (let i = 0; i < 10; i++) {
    try {
      if (!window.DotNet || typeof window.DotNet.invokeMethodAsync !== 'function') {
        throw new Error('DotNet.invokeMethodAsync not available yet');
      }
      const result = await window.DotNet.invokeMethodAsync("cycodblazor", "ClearChatHistory");
      return JSON.parse(result);
    } catch (e) {
      lastErr = e
      await delay(50)
    }
  }
  throw lastErr ?? new Error('Unknown error calling clearChatHistory()')
}

export async function getChatStatus(): Promise<{ 
  Success: boolean; 
  IsInitialized?: boolean; 
  Capabilities?: {
    Streaming: boolean;
    FunctionCalling: boolean;
    HistoryPersistence: boolean;
    ApprovalWorkflow: boolean;
  }; 
  Version?: string; 
  Error?: string 
}> {
  await ensureStarted();
  let lastErr: unknown = undefined
  for (let i = 0; i < 10; i++) {
    try {
      if (!window.DotNet || typeof window.DotNet.invokeMethodAsync !== 'function') {
        throw new Error('DotNet.invokeMethodAsync not available yet');
      }
      const result = await window.DotNet.invokeMethodAsync("cycodblazor", "GetChatStatus");
      const parsed = JSON.parse(result);
      return parsed;
    } catch (e) {
      lastErr = e
      await delay(50)
    }
  }
  const errorMessage = lastErr instanceof Error ? lastErr.message : String(lastErr);
  throw new Error(`getChatStatus failed after 10 attempts. Last error: ${errorMessage}`)
}

export async function saveChatHistory(key: string): Promise<{ Success: boolean; Message?: string; Error?: string }> {
  await ensureStarted();
  let lastErr: unknown = undefined
  for (let i = 0; i < 10; i++) {
    try {
      if (!window.DotNet || typeof window.DotNet.invokeMethodAsync !== 'function') {
        throw new Error('DotNet.invokeMethodAsync not available yet');
      }
      const result = await window.DotNet.invokeMethodAsync("cycodblazor", "SaveChatHistory", key);
      return JSON.parse(result);
    } catch (e) {
      lastErr = e
      await delay(50)
    }
  }
  throw lastErr ?? new Error('Unknown error calling saveChatHistory()')
}

export async function loadChatHistory(key: string): Promise<{ Success: boolean; Message?: string; Error?: string }> {
  await ensureStarted();
  let lastErr: unknown = undefined
  for (let i = 0; i < 10; i++) {
    try {
      if (!window.DotNet || typeof window.DotNet.invokeMethodAsync !== 'function') {
        throw new Error('DotNet.invokeMethodAsync not available yet');
      }
      const result = await window.DotNet.invokeMethodAsync("cycodblazor", "LoadChatHistory", key);
      return JSON.parse(result);
    } catch (e) {
      lastErr = e
      await delay(50)
    }
  }
  throw lastErr ?? new Error('Unknown error calling loadChatHistory()')
}

interface StreamingCallbacks {
  onChunk: (chunk: string) => void;
  onComplete: () => void;
  onError: (error: string) => void;
  onFunctionCall?: (functionName: string, functionArgs: string | null, functionResult: string | null) => void;
}

export class StreamingChatCallback {
  dotNetRef: unknown;
  callbacks: StreamingCallbacks;
  
  constructor(callbacks: StreamingCallbacks) {
    this.callbacks = callbacks;
    // Create a .NET reference to this callback instance
    if (window.DotNet && typeof window.DotNet.createJSObjectReference === 'function') {
      this.dotNetRef = window.DotNet.createJSObjectReference(this);
    }
  }
  
  // These methods will be called from .NET
  OnChunk(chunk: string): void {
    this.callbacks.onChunk(chunk);
  }
  
  OnComplete(): void {
    this.callbacks.onComplete();
  }
  
  OnError(error: string): void {
    this.callbacks.onError(error);
  }
  
  OnFunctionCall(functionName: string, functionArgs: string | null, functionResult: string | null): void {
    if (this.callbacks.onFunctionCall) {
      this.callbacks.onFunctionCall(functionName, functionArgs, functionResult);
    }
  }
  
  async OnFunctionCallApproval(_functionName: string, _functionArgs: string | null): Promise<boolean> {
    // For now, auto-approve all function calls
    // In a real implementation, you'd show a dialog to the user
    return true;
  }
  
  getDotNetReference(): unknown {
    return this.dotNetRef;
  }
  
  dispose(): void {
    const ref = this.dotNetRef as { dispose?: () => void };
    if (ref && typeof ref.dispose === 'function') {
      ref.dispose();
    }
  }
}

export async function sendMessageStreaming(
  message: string,
  onChunk: (chunk: string) => void,
  onComplete: () => void,
  onError: (error: string) => void,
  onFunctionCall?: (functionName: string, functionArgs: string | null, functionResult: string | null) => void
): Promise<{ Success: boolean; Message?: string; Error?: string }> {
  await ensureStarted();
  
  const callback = new StreamingChatCallback({
    onChunk,
    onComplete,
    onError,
    onFunctionCall
  });
  
  let lastErr: unknown = undefined
  for (let i = 0; i < 10; i++) {
    try {
      if (!window.DotNet || typeof window.DotNet.invokeMethodAsync !== 'function') {
        throw new Error('DotNet.invokeMethodAsync not available yet');
      }
      const result = await window.DotNet.invokeMethodAsync(
        "cycodblazor", 
        "SendMessageStreaming", 
        message, 
        callback.getDotNetReference()
      );
      return JSON.parse(result);
    } catch (e) {
      lastErr = e
      await delay(50)
    }
  }
  
  callback.dispose();
  throw lastErr ?? new Error('Unknown error calling sendMessageStreaming()')
}
