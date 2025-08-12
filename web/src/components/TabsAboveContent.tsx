// no explicit React import required with modern JSX

export type TabKey = "windows" | "mac" | "linux"

export function TabsAboveContent({ tab, onTabChange }: { tab: TabKey; onTabChange: (k: TabKey) => void }) {
  return (
    <div className="mb-10">
      <div className="grid grid-cols-3 gap-4 text-center mb-4" role="tablist" aria-label="Platforms">
        {[
          { label: "Windows", key: "windows" as TabKey, text: "text-green-400", bar: "bg-green-400" },
          { label: "MacOS", key: "mac" as TabKey, text: "text-blue-400", bar: "bg-blue-400" },
          { label: "Linux", key: "linux" as TabKey, text: "text-purple-400", bar: "bg-purple-400" },
        ].map(({ label, key, text, bar }) => (
          <button
            key={key}
            role="tab"
            aria-selected={tab === key}
            onClick={() => onTabChange(key)}
            className={`space-y-2 px-2 py-1 transition ${tab === key ? "text-white" : "text-white/80 hover:text-white"}`}
          >
            <div className={`${text} font-mono text-sm`}>{label}</div>
            <div className={`mx-auto h-1 w-full rounded ${bar} ${tab === key ? "opacity-100" : "opacity-50"}`} />
          </button>
        ))}
      </div>

      {/* Empty tab contents for now */}
      <div role="region" aria-live="polite" className="min-h-10 mb-6" />
    </div>
  )
}


