import Header from "@/components/Header"
import StarfieldBackground from "@/components/StarfieldBackground"
import { getAuth, onAuthStateChanged, type User } from "firebase/auth"
import { useEffect, useState } from "react"
import { getFirebaseApp } from "@/lib/firebase"
import ErrorPage from "./ErrorPage"
import SignupTable from "@/components/SignupTable"

export default function Admin() {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const [activeTab, setActiveTab] = useState<"signup" | "logins">("signup")

  useEffect(() => {
    const auth = getAuth(getFirebaseApp())
    const unsub = onAuthStateChanged(auth, (u) => {
      setUser(u)
      setLoading(false)
    })
    return () => unsub()
  }, [])

  if (loading) {
    return (
      <StarfieldBackground>
        <Header />
        <main className="min-h-screen pt-24 text-white">
          <div className="max-w-4xl mx-auto px-4 py-20">
            <p className="text-gray-300">Loading…</p>
          </div>
        </main>
      </StarfieldBackground>
    )
  }

  const allowed = user?.email?.toLowerCase() === "philipp.h.schmid@gmail.com"
  if (!allowed) return <ErrorPage />

  return (
    <StarfieldBackground>
      <Header />
      <main className="min-h-screen pt-24 text-white">
        <div className="max-w-4xl mx-auto px-4 py-20">
          <h1 className="text-3xl md:text-4xl font-mono">theMovement Admin</h1>

          <div className="bg-gray-900/80 rounded-lg p-8 border border-gray-700 mt-8">
            <p className="text-lg md:text-xl leading-relaxed mb-6">
              <span className="text-white-400 font-mono">Admin functionality to manage theMovement</span>
            </p>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-center select-none">
              {[
                { key: "signup" as const, label: "Signups", color: "green" },
                { key: "logins" as const, label: "Logins", color: "blue" }
              ].map((t) => {
                const isActive = activeTab === t.key
                const colorText =
                  t.color === "green" ? "text-green-400" :
                  t.color === "blue" ? "text-blue-400" :
                  t.color === "purple" ? "text-purple-400" :
                  "text-yellow-400"
                const colorBar =
                  t.color === "green" ? "bg-green-400" :
                  t.color === "blue" ? "bg-blue-400" :
                  t.color === "purple" ? "bg-purple-400" :
                  "bg-yellow-400"
                return (
                  <button
                    key={t.key}
                    type="button"
                    onClick={() => setActiveTab(t.key)}
                    className="group space-y-2 focus:outline-none"
                    aria-pressed={isActive}
                  >
                    <div className={`font-mono text-sm ${colorText} ${isActive ? "brightness-125" : "opacity-90"}`}>
                      {t.label}
                    </div>
                    <div className={`w-full h-1 rounded transition-all ${colorBar} ${isActive ? "scale-100" : "scale-95 opacity-70"}`} />
                  </button>
                )
              })}
            </div>
            <div className="mt-8">
              {activeTab === "signup" && (
                <SignupTable />
              )}
            </div>
          </div>
        </div>
      </main>
    </StarfieldBackground>
  )
}


