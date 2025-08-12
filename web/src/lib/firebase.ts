import { initializeApp, getApps } from "firebase/app"
import { getAuth, GoogleAuthProvider, OAuthProvider, signInWithPopup } from "firebase/auth"

// Fill these with your project's values from Firebase console
export const firebaseConfig = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY as string,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN as string,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID as string,
  appId: import.meta.env.VITE_FIREBASE_APP_ID as string,
} as const

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


