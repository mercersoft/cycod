// src/cycodHeadless.ts
declare global {
  interface Window {
    Blazor?: { start: (options?: unknown) => Promise<void> };
    DotNet?: {
      invokeMethodAsync: (assembly: string, method: string, ...args: any[]) => Promise<any>;
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
