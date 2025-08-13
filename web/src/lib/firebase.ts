import { initializeApp, getApps } from "firebase/app"
import { getAuth, GoogleAuthProvider, OAuthProvider, signInWithPopup, signOut } from "firebase/auth"
import { getFirestore, collection, addDoc, serverTimestamp } from "firebase/firestore"

// Fill these with your project's values from Firebase console
export const firebaseConfig = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY as string,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN as string,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID as string,
  appId: import.meta.env.VITE_FIREBASE_APP_ID as string,
} as const


export function getFirebaseApp() {
  const app = getApps().length ? getApps()[0]! : initializeApp(firebaseConfig)
  return app
}

function detectOsEnv(): string {
  const navAny = navigator as unknown as { userAgentData?: { platform?: string } }
  const source = (navAny.userAgentData?.platform || navigator.platform || navigator.userAgent || "").toLowerCase()
  if (source.includes("win")) return "Windows"
  if (source.includes("mac")) return "macOS"
  if (source.includes("iphone") || source.includes("ipad") || source.includes("ipod") || source.includes("ios")) return "iOS"
  if (source.includes("android")) return "Android"
  if (source.includes("linux")) return "Linux"
  return "Unknown"
}

export async function signInWithGooglePopup() {
  const app = getFirebaseApp()
  const auth = getAuth(app)
  const provider = new GoogleAuthProvider()
  const result = await signInWithPopup(auth, provider)
  try {
    const db = getFirestore(app)
    const env = detectOsEnv()
    await addDoc(collection(db, "auth"), {
      createdAt: serverTimestamp(),
      email: result.user.email ?? "",
      action: "signin",
      env,
    })
  } catch (err) {
    // eslint-disable-next-line no-console
    console.warn("[auth-log] failed to log signin", err)
  }
  return result.user
}

export async function signInWithMicrosoftPopup() {
  const app = getFirebaseApp()
  const auth = getAuth(app)
  const provider = new OAuthProvider("microsoft.com")
  // Optional scopes for basic profile and email via Graph
  provider.setCustomParameters({ prompt: "select_account" })
  provider.addScope("User.Read")
  const result = await signInWithPopup(auth, provider)
  try {
    const db = getFirestore(app)
    const env = detectOsEnv()
    await addDoc(collection(db, "auth"), {
      createdAt: serverTimestamp(),
      email: result.user.email ?? "",
      action: "signin",
      env,
    })
  } catch (err) {
    // eslint-disable-next-line no-console
    console.warn("[auth-log] failed to log signin (microsoft)", err)
  }
  return result.user
}

export async function signOutAndLog(): Promise<void> {
  const app = getFirebaseApp()
  const auth = getAuth(app)
  const email = auth.currentUser?.email ?? ""
  try {
    const db = getFirestore(app)
    const env = detectOsEnv()
    await addDoc(collection(db, "auth"), {
      createdAt: serverTimestamp(),
      email,
      action: "signout",
      env,
    })
  } catch (err) {
    // eslint-disable-next-line no-console
    console.warn("[auth-log] failed to log signout", err)
  } finally {
    try {
      await signOut(auth)
    } catch (e) {
      // eslint-disable-next-line no-console
      console.error("Sign out failed", e)
    }
  }
}


