import { useState } from "react"
import Header from "@/components/Header"
import StarfieldBackground from "@/components/StarfieldBackground"
import GettingStartedMac from "@/components/GettingStartedMac"
import GettingStartedWindows from "@/components/GettingStartedWindows"
import { TabsAboveContent, type TabKey } from "@/components/TabsAboveContent"

// TabKey now imported from TabsAboveContent

export default function GettingStarted() {
  const [tab, setTab] = useState<TabKey>("mac")
  return (
    <StarfieldBackground>
      <Header />
      <main className="min-h-screen pt-24 text-white">
        <div className="max-w-4xl mx-auto px-4 py-20">
          {/* Tabs (Windows, MacOS, Linux) */}
          <TabsAboveContent tab={tab} onTabChange={setTab} />

          <ContentByTab tab={tab} />
        </div>
      </main>
    </StarfieldBackground>
  )
}
function ContentByTab({ tab }: { tab: TabKey }) {
  if (tab === "windows") return <GettingStartedWindows />
  if (tab === "linux") return null
  return <GettingStartedMac />
}


