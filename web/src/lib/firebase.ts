import { initializeApp, getApps } from "firebase/app"
import { getAuth, GoogleAuthProvider, OAuthProvider, signInWithPopup } from "firebase/auth"

// Fill these with your project's values from Firebase console
export const firebaseConfig = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY as string,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN as string,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID as string,
  appId: import.meta.env.VITE_FIREBASE_APP_ID as string,
} as const

// Temporary debug to verify env var injection during CI builds
// Note: Values with Vite's VITE_ prefix are exposed to client bundles.
// Remove these logs after confirming deployment config.
// eslint-disable-next-line no-console
console.log("[env] VITE_FIREBASE_API_KEY:", import.meta.env.VITE_FIREBASE_API_KEY || "(undefined)")
// eslint-disable-next-line no-console
console.log("[env] VITE_FIREBASE_AUTH_DOMAIN:", import.meta.env.VITE_FIREBASE_AUTH_DOMAIN || "(undefined)")
// eslint-disable-next-line no-console
console.log("[env] VITE_FIREBASE_PROJECT_ID:", import.meta.env.VITE_FIREBASE_PROJECT_ID || "(undefined)")
// eslint-disable-next-line no-console
console.log("[env] VITE_FIREBASE_APP_ID:", import.meta.env.VITE_FIREBASE_APP_ID || "(undefined)")

export function getFirebaseApp() {
  return getApps().length ? getApps()[0]! : initializeApp(firebaseConfig)
}

export async function signInWithGooglePopup() {
  const app = getFirebaseApp()
  const auth = getAuth(app)
  const provider = new GoogleAuthProvider()
  const result = await signInWithPopup(auth, provider)
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
  return result.user
}


