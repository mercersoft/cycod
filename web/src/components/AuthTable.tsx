import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { getFirebaseApp } from "@/lib/firebase"
import {
  collection,
  getCountFromServer,
  getDocs,
  getFirestore,
  limit,
  orderBy,
  query,
  startAfter,
  Timestamp,
  where,
  type DocumentData,
  type QueryDocumentSnapshot,
} from "firebase/firestore"

type AuthRow = {
  id: string
  email: string
  action: string
  createdAt: Date
  env?: string
}

export default function AuthTable() {
  const [rows, setRows] = useState<AuthRow[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [pageSize] = useState(20)
  const [pageIndex, setPageIndex] = useState(0)
  const pageCursorsRef = useRef<Array<QueryDocumentSnapshot<DocumentData> | null>>([null])
  const [hasNextPage, setHasNextPage] = useState(false)

  // Summary counts (over entire collection)
  const [summaryLoading, setSummaryLoading] = useState(false)
  const [summaryError, setSummaryError] = useState<string | null>(null)
  const [totalCount, setTotalCount] = useState<number>(0)
  const [last7Count, setLast7Count] = useState<number>(0)
  const [windowsCount, setWindowsCount] = useState<number>(0)
  const [macCount, setMacCount] = useState<number>(0)
  const othersCount = useMemo(() => Math.max(0, totalCount - windowsCount - macCount), [totalCount, windowsCount, macCount])

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / pageSize)), [totalCount, pageSize])

  const fetchPage = useCallback(async (requestedPageIndex: number) => {
    setLoading(true)
    setError(null)
    try {
      const app = getFirebaseApp()
      const db = getFirestore(app)
      const baseRef = collection(db, "auth")

      const constraints: any[] = []
      constraints.push(orderBy("createdAt", "desc"))

      const cursor = pageCursorsRef.current[requestedPageIndex]
      if (requestedPageIndex > 0 && cursor) {
        constraints.push(startAfter(cursor))
      }
      constraints.push(limit(pageSize))

      const q = query(baseRef, ...constraints)
      const snap = await getDocs(q)

      const docs = snap.docs
      const mapped: AuthRow[] = docs.map((d) => {
        const data = d.data() as { email?: string; action?: string; createdAt?: Timestamp; env?: string }
        return {
          id: d.id,
          email: data.email ?? "",
          action: data.action ?? "",
          createdAt: (data.createdAt instanceof Timestamp ? data.createdAt.toDate() : new Date(0)),
          env: data.env,
        }
      })

      setRows(mapped)
      setHasNextPage(docs.length === pageSize)

      // Store cursor for next page without re-rendering
      const lastDoc = docs[docs.length - 1]
      const existing = pageCursorsRef.current.slice()
      existing[requestedPageIndex + 1] = lastDoc ?? null
      pageCursorsRef.current = existing

      setPageIndex(requestedPageIndex)
    } catch (e: any) {
      setError(e?.message || "Failed to load auth events")
    } finally {
      setLoading(false)
    }
  }, [pageSize])

  // Initial load
  useEffect(() => {
    pageCursorsRef.current = [null]
    setPageIndex(0)
    fetchPage(0)
  }, [fetchPage])

  // Fetch summary once on mount
  useEffect(() => {
    const run = async () => {
      setSummaryLoading(true)
      setSummaryError(null)
      try {
        const app = getFirebaseApp()
        const db = getFirestore(app)
        const baseRef = collection(db, "auth")

        const sevenDaysAgo = new Date()
        sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7)

        const [totalSnap, last7Snap, winSnap, macSnap] = await Promise.all([
          getCountFromServer(query(baseRef, where("action", "==", "signin"))),
          getCountFromServer(query(baseRef, where("action", "==", "signin"), where("createdAt", ">=", Timestamp.fromDate(sevenDaysAgo)))),
          // Windows sign-ins only
          getCountFromServer(query(baseRef, where("action", "==", "signin"), where("env", "==", "Windows"))),
          // macOS sign-ins only
          getCountFromServer(query(baseRef, where("action", "==", "signin"), where("env", "==", "macOS"))),
        ])

        console.log("Stats for Logins:")
        console.log("totalSnap", totalSnap.data().count)
        console.log("last7Snap", last7Snap.data().count)
        console.log("winSnap", winSnap.data().count)
        console.log("macSnap", macSnap.data().count)

        setTotalCount(totalSnap.data().count)
        setLast7Count(last7Snap.data().count)
        setWindowsCount(winSnap.data().count)
        setMacCount(macSnap.data().count)
      } catch (e: any) {
        setSummaryError(e?.message || "Failed to load summary")
      } finally {
        setSummaryLoading(false)
      }
    }
    run()
  }, [])

  return (
    <div className="space-y-4">
      {/* Summary Row */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
        <div className="bg-gray-900/80 rounded border border-gray-800 p-3">
          <div className="text-xs text-gray-400 font-mono">Total</div>
          <div className="text-white text-lg font-mono">{summaryLoading ? "—" : totalCount}</div>
        </div>
        <div className="bg-gray-900/80 rounded border border-gray-800 p-3">
          <div className="text-xs text-gray-400 font-mono">Last 7 days</div>
          <div className="text-white text-lg font-mono">{summaryLoading ? "—" : last7Count}</div>
        </div>
        <div className="bg-gray-900/80 rounded border border-gray-800 p-3">
          <div className="text-xs text-gray-400 font-mono">Windows</div>
          <div className="text-white text-lg font-mono">{summaryLoading ? "—" : windowsCount}</div>
        </div>
        <div className="bg-gray-900/80 rounded border border-gray-800 p-3">
          <div className="text-xs text-gray-400 font-mono">MacOS</div>
          <div className="text-white text-lg font-mono">{summaryLoading ? "—" : macCount}</div>
        </div>
        <div className="bg-gray-900/80 rounded border border-gray-800 p-3">
          <div className="text-xs text-gray-400 font-mono">Linux/others</div>
          <div className="text-white text-lg font-mono">{summaryLoading ? "—" : othersCount}</div>
        </div>
      </div>

      {summaryError && (
        <div className="text-red-400 text-sm">{summaryError}</div>
      )}

      <div className="overflow-x-auto rounded border border-gray-800">
        <table className="min-w-full text-left text-sm text-gray-300">
          <thead className="bg-gray-800 text-gray-200 font-mono text-xs uppercase">
            <tr>
              <th className="px-3 py-2">Created</th>
              <th className="px-3 py-2">Email</th>
              <th className="px-3 py-2">Action</th>
              <th className="px-3 py-2">Env</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr>
                <td colSpan={4} className="px-3 py-4 text-center text-gray-400">Loading…</td>
              </tr>
            )}
            {error && !loading && (
              <tr>
                <td colSpan={4} className="px-3 py-4 text-center text-red-400">{error}</td>
              </tr>
            )}
            {!loading && !error && rows.length === 0 && (
              <tr>
                <td colSpan={4} className="px-3 py-4 text-center text-gray-400">No results</td>
              </tr>
            )}
            {!loading && !error && rows.map((r) => (
              <tr key={r.id} className="border-t border-gray-800">
                <td className="px-3 py-2 whitespace-nowrap">{r.createdAt.toLocaleString()}</td>
                <td className="px-3 py-2 whitespace-nowrap">{r.email}</td>
                <td className="px-3 py-2 whitespace-nowrap">{r.action}</td>
                <td className="px-3 py-2 whitespace-nowrap">{r.env ?? ""}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between">
        <div className="text-xs text-gray-400 font-mono">Page {pageIndex + 1} of {totalPages}</div>
        <div className="flex gap-2">
          <button
            type="button"
            className="px-3 py-2 border border-gray-700 text-white rounded text-sm disabled:opacity-50"
            onClick={() => fetchPage(Math.max(0, pageIndex - 1))}
            disabled={loading || pageIndex === 0}
          >
            Prev
          </button>
          <button
            type="button"
            className="px-3 py-2 border border-gray-700 text-white rounded text-sm disabled:opacity-50"
            onClick={() => fetchPage(pageIndex + 1)}
            disabled={loading || !hasNextPage}
          >
            Next
          </button>
        </div>
      </div>
    </div>
  )
}


