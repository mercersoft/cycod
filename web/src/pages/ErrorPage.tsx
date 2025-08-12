import Header from "@/components/Header"
import StarfieldBackground from "@/components/StarfieldBackground"

export default function ErrorPage() {
  return (
    <StarfieldBackground>
      <Header />
      <main className="min-h-screen pt-24 text-white">
        <div className="max-w-4xl mx-auto px-4 py-20">
          <h1 className="text-3xl md:text-4xl font-mono text-red-400">Access denied</h1>
          <p className="text-gray-300 mt-4">You do not have permission to view this page.</p>
        </div>
      </main>
    </StarfieldBackground>
  )
}


