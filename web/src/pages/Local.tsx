import Header from "@/components/Header"
import StarfieldBackground from "@/components/StarfieldBackground"
import DeamonStatus from "@/components/DeamonStatus"

export default function Local() {
  return (
    <StarfieldBackground>
      <Header />
      <main className="min-h-screen pt-24 text-white">
        <div className="max-w-4xl mx-auto px-4 py-20">
          <DeamonStatus />
        </div>
      </main>
    </StarfieldBackground>
  )
}
