import { useEffect, useMemo, useState } from "react"
import { ChevronDown, Terminal, Smartphone, Mic, GitBranch, Users, Zap } from "lucide-react"
import StarfieldBackground from "./components/StarfieldBackground"
import Header from "./components/Header"
import { getFirebaseApp } from "@/lib/firebase"
import { getFirestore, collection, addDoc, serverTimestamp } from "firebase/firestore"

function App() {
  const [email, setEmail] = useState("")
  const [showThanks, setShowThanks] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [typed, setTyped] = useState("")
  const headline = "It's a toolset, a mindset, a culture, and a movement."

  useEffect(() => {
    let i = 0
    const id = setInterval(() => {
      if (i < headline.length) {
        setTyped(headline.slice(0, i + 1))
        i += 1
      } else {
        clearInterval(id)
      }
    }, 50)
    return () => clearInterval(id)
  }, [])

  function detectOsEnv(): string {
    // userAgentData is non-standardly typed, so cast only this property
    const navAny = navigator as unknown as { userAgentData?: { platform?: string } }
    const uaData = navAny.userAgentData
    if (uaData && uaData.platform) return String(uaData.platform)
    const p = navigator.platform || ""
    if (p) return p
    const ua = navigator.userAgent || ""
    if (/Windows/i.test(ua)) return "Windows"
    if (/Mac OS X/i.test(ua)) return "Mac"
    if (/Android/i.test(ua)) return "Android"
    if (/(iPhone|iPad|iPod)/i.test(ua)) return "iOS"
    if (/Linux/i.test(ua)) return "Linux"
    return "Unknown"
  }

  const isValidEmail = useMemo(() => {
    if (!email) return false
    // Simple, robust email regex for client-side gating
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
  }, [email])

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!isValidEmail || submitting) return
    try {
      setSubmitting(true)
      const app = getFirebaseApp()
      const db = getFirestore(app)
      const env = detectOsEnv()
      await addDoc(collection(db, "theMovement"), {
        signup: serverTimestamp(),
        email,
        env,
      })
      setShowThanks(true)
    } catch (err) {
      console.error("Failed to submit join:", err)
      alert("Sorry, something went wrong. Please try again.")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <StarfieldBackground>
      <Header />

      {/* Hero */}
      <section className="min-h-[64vh] flex flex-col justify-center items-center px-4 relative text-white">
        <div className="max-w-4xl mx-auto text-center space-y-8">
          <div className="flex items-center justify-center space-x-3 mb-8">
            <Terminal className="w-12 h-12 text-green-400" />
            <h1 className="text-4xl md:text-6xl font-mono font-bold tracking-wider">
              Cyco<span className="text-green-400">Dev</span>
            </h1>
          </div>
          <div className="h-20 flex items-center justify-center">
            <h2 className="text-2xl md:text-4xl font-light leading-tight max-w-3xl">
              {typed}
              <span className="animate-pulse text-green-400">|</span>
            </h2>
          </div>
          <p className="text-xl md:text-2xl text-gray-300 font-mono">
            Freedom → Control → Collaboration → <span className="text-green-400">Flow</span>
          </p>
          <div className="pt-8">
            <a href="#join" className="inline-flex items-center px-8 py-4 bg-green-400 text-black font-semibold rounded-lg hover:bg-green-300 transition-all duration-300 transform hover:scale-105">
              Join the Movement
              <ChevronDown className="ml-2 w-5 h-5" />
            </a>
          </div>
        </div>
        <div className="absolute bottom-8 left-1/2 -translate-x-1/2 animate-bounce">
          <ChevronDown className="w-6 h-6 text-gray-400" />
        </div>
      </section>

      {/* $ what_is_cycodev */}
      <section className="py-12 px-4 border-t border-gray-800 text-white">
        <div className="max-w-4xl mx-auto">
          <h3 className="text-3xl md:text-4xl font-mono mb-8 text-center">
            <span className="text-green-400">$</span> what_is_cycodev
          </h3>
          <div className="bg-gray-900/80 rounded-lg p-8 border border-gray-700">
            <p className="text-lg md:text-xl leading-relaxed mb-6">
              CycoDev is a <span className="text-green-400 font-mono">CLI-first</span>,
              <span className="text-blue-400 font-mono"> Git-native</span>,
              <span className="text-purple-400 font-mono"> AI-augmented</span> developer
              toolset that starts where you already live—the terminal.
            </p>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
              <div className="space-y-2">
                <div className="text-green-400 font-mono text-sm">Open Source</div>
                <div className="w-full h-1 bg-green-400 rounded" />
              </div>
              <div className="space-y-2">
                <div className="text-blue-400 font-mono text-sm">BYOK</div>
                <div className="w-full h-1 bg-blue-400 rounded" />
              </div>
              <div className="space-y-2">
                <div className="text-purple-400 font-mono text-sm">IDE Extensions</div>
                <div className="w-full h-1 bg-purple-400 rounded" />
              </div>
              <div className="space-y-2">
                <div className="text-yellow-400 font-mono text-sm">Mobile + Voice</div>
                <div className="w-full h-1 bg-yellow-400 rounded" />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* # Coming Features */}
      <section className="py-20 px-4 border-t border-gray-800 text-white">
        <div className="max-w-6xl mx-auto">
          <h3 className="text-3xl md:text-4xl font-mono mb-12 text-center">
            <span className="text-green-400">#</span> Coming Features
          </h3>
          <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-8">
            <div className="bg-gray-900/80 rounded-lg p-6 border border-gray-700 hover:border-green-400 transition-all duration-300 group">
              <Terminal className="w-8 h-8 text-green-400 mb-4 group-hover:scale-110 transition-transform" />
              <h4 className="font-mono text-lg mb-2">CLI + IDE</h4>
              <p className="text-gray-400 text-sm">Native terminal integration with your favorite editor</p>
            </div>
            <div className="bg-gray-900/80 rounded-lg p-6 border border-gray-700 hover:border-blue-400 transition-all duration-300 group">
              <Smartphone className="w-8 h-8 text-blue-400 mb-4 group-hover:scale-110 transition-transform" />
              <h4 className="font-mono text-lg mb-2">Mobile</h4>
              <p className="text-gray-400 text-sm">Code review and management on the go</p>
            </div>
            <div className="bg-gray-900/80 rounded-lg p-6 border border-gray-700 hover:border-purple-400 transition-all duration-300 group">
              <Mic className="w-8 h-8 text-purple-400 mb-4 group-hover:scale-110 transition-transform" />
              <h4 className="font-mono text-lg mb-2">Voice</h4>
              <p className="text-gray-400 text-sm">Natural language interface for complex workflows</p>
            </div>
            <div className="bg-gray-900/80 rounded-lg p-6 border border-gray-700 hover:border-yellow-400 transition-all duration-300 group">
              <GitBranch className="w-8 h-8 text-yellow-400 mb-4 group-hover:scale-110 transition-transform" />
              <h4 className="font-mono text-lg mb-2">Agentic</h4>
              <p className="text-gray-400 text-sm">Branching, time-travel, multi-verse workflows</p>
            </div>
          </div>
          <div className="mt-12 text-center">
            <div className="inline-flex items-center space-x-4 px-6 py-4 bg-gray-900/80 rounded-lg border border-gray-700">
              <Zap className="w-5 h-5 text-green-400" />
              <span className="font-mono">Developer-first: No vendor lock-in, full transparency</span>
            </div>
          </div>
        </div>
      </section>

      {/* ? Why Now */}
      <section className="py-20 px-4 border-t border-gray-800 text-white">
        <div className="max-w-4xl mx-auto text-center">
          <h3 className="text-3xl md:text-4xl font-mono mb-8">
            <span className="text-green-400">?</span> Why Now
          </h3>
          <div className="space-y-6 text-lg md:text-xl leading-relaxed">
            <p>
              The intersection of <span className="text-blue-400 font-mono">AI</span> and
              <span className="text-green-400 font-mono"> DevTools</span> is red-hot, but ripe for disruption.
            </p>
            <p className="text-gray-300">While others build yet another IDE or web-based tool, we start where developers already live—</p>
            <p className="font-mono text-2xl text-green-400">the terminal.</p>
          </div>
        </div>
      </section>

      {/* # Join */}
      <section id="join" className="py-20 px-4 border-t border-gray-800 text-white">
        <div className="max-w-2xl mx-auto text-center">
          <Users className="w-16 h-16 text-green-400 mx-auto mb-8" />
          <h3 className="text-3xl md:text-4xl font-mono mb-6">Join the Movement</h3>
          <p className="text-lg text-gray-300 mb-8">Community-first, open source roots. Be among the first to shape the future of development tools.</p>
          <form onSubmit={onSubmit} className="space-y-4">
            <div className="flex flex-col sm:flex-row gap-4 max-w-md mx-auto">
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="developer@example.com"
                required
                className="flex-1 px-4 py-3 bg-gray-900/80 border border-gray-700 rounded-lg focus:border-green-400 focus:outline-none font-mono"
              />
              <button
                type="submit"
                disabled={!isValidEmail || submitting}
                className="px-6 py-3 bg-green-400 text-black font-semibold rounded-lg transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-green-300 transform hover:scale-105"
              >
                {submitting ? "Joining..." : "Join"}
              </button>
            </div>
          </form>
        </div>
      </section>

      {/* Thank-you modal */}
      {showThanks && (
        <div className="fixed inset-0 z-[60] grid place-items-center bg-black/60 p-4">
          <div className="max-w-md w-full rounded-xl border border-white/15 bg-gray-900/95 p-6 text-white shadow-xl">
            <h4 className="font-mono text-lg mb-2 text-green-400">Thank you!</h4>
            <p className="text-gray-200 mb-6">Thank you for your interest in CycoDev. We will email you with future updates!</p>
            <div className="flex justify-end">
              <button
                type="button"
                onClick={() => {
                  setShowThanks(false)
                  setEmail("")
                }}
                className="rounded-lg border border-white/25 bg-white/5 px-4 py-2 text-sm text-white/90 shadow-sm backdrop-blur-md transition hover:bg-white/10 hover:text-white"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      <footer className="py-16 px-4 border-t border-gray-800 text-white">
        <div className="max-w-6xl mx-auto">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8">
            <div>
              <h4 className="text-lg font-mono mb-4">Product</h4>
              <ul className="space-y-3 text-gray-300">
                <li><a href="#" className="hover:text-white transition-colors">Agent</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Chat</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Next Edit</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Completions</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Slack</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Pricing</a></li>
              </ul>
            </div>
            <div>
              <h4 className="text-lg font-mono mb-4">Resources</h4>
              <ul className="space-y-3 text-gray-300">
                <li><a href="#" className="hover:text-white transition-colors">Docs</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Blog</a></li>
                <li><a href="#" className="hover:text-white transition-colors">MCP Servers</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Changelog</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Privacy & Security</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Trust Center</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Status Page</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Guides</a></li>
              </ul>
            </div>
            <div>
              <h4 className="text-lg font-mono mb-4">Company</h4>
              <ul className="space-y-3 text-gray-300">
                <li><a href="#" className="hover:text-white transition-colors">Careers</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Press Inquiries</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Contact Sales</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Contact Support</a></li>
              </ul>
            </div>
            <div>
              <h4 className="text-lg font-mono mb-4">Legal</h4>
              <ul className="space-y-3 text-gray-300">
                <li><a href="#" className="hover:text-white transition-colors">Cookie Policy</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Privacy Policy</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Terms of Service</a></li>
              </ul>
            </div>
          </div>
          <div className="mt-12 pt-6 border-t border-gray-800 text-center">
            <div className="flex items-center justify-center gap-3 text-gray-500 text-sm font-mono">
              <span className="text-gray-400">&gt;_</span>
              <span className="text-gray-300">Cyco<span className="text-green-400">Dev</span></span>
              <span>©</span>
              <span>2025</span>
            </div>
          </div>
        </div>
      </footer>

    </StarfieldBackground>
  )
}

export default App
