#!/usr/bin/env node
import fs from 'node:fs'
import path from 'node:path'
import url from 'node:url'
import process from 'node:process'

// Requires service account credentials. Provide one of:
// - GOOGLE_APPLICATION_CREDENTIALS pointing to a service-account JSON
// - FIREBASE_SERVICE_ACCOUNT_JSON containing the JSON inline
import admin from 'firebase-admin'

function loadServiceAccount() {
  const inlineJson = process.env.FIREBASE_SERVICE_ACCOUNT_JSON
  const filePath = process.env.GOOGLE_APPLICATION_CREDENTIALS
  if (inlineJson) {
    return JSON.parse(inlineJson)
  }
  if (filePath) {
    const content = fs.readFileSync(filePath, 'utf8')
    return JSON.parse(content)
  }
  console.error('Missing service account. Set GOOGLE_APPLICATION_CREDENTIALS or FIREBASE_SERVICE_ACCOUNT_JSON')
  process.exit(1)
}

function parseArgs() {
  const args = process.argv.slice(2)
  const result = { file: null }
  for (let i = 0; i < args.length; i++) {
    if ((args[i] === '--file' || args[i] === '-f') && args[i + 1]) {
      result.file = args[i + 1]
      i++
    }
  }
  if (!result.file) {
    console.error('Usage: node scripts/seed-firestore.mjs --file path/to/seed.json')
    process.exit(1)
  }
  return result
}

function resolveFile(p) {
  if (p.startsWith('file:')) return url.fileURLToPath(p)
  if (path.isAbsolute(p)) return p
  return path.join(process.cwd(), p)
}

async function main() {
  const { file } = parseArgs()
  const seedPath = resolveFile(file)
  const serviceAccount = loadServiceAccount()

  if (admin.apps.length === 0) {
    admin.initializeApp({
      credential: admin.credential.cert(serviceAccount),
      projectId: process.env.FIREBASE_PROJECT_ID || serviceAccount.project_id,
    })
  }
  const db = admin.firestore()

  /**
   * Seed file format:
   * {
   *   "collections": {
   *     "collectionName": {
   *       "docId1": { "field": "value", "subcollections": { "subName": { "subDocId": { ... } } } },
   *       "docId2": { ... }
   *     },
   *     "anotherCollection": { ... }
   *   }
   * }
   */
  const raw = fs.readFileSync(seedPath, 'utf8')
  const data = JSON.parse(raw)
  const collections = data.collections || {}

  let writeCount = 0

  function transformFields(value) {
    if (Array.isArray(value)) {
      return value.map((v) => transformFields(v))
    }
    if (value && typeof value === 'object') {
      if (value._serverTimestamp === true) {
        return admin.firestore.FieldValue.serverTimestamp()
      }
      const out = {}
      for (const [k, v] of Object.entries(value)) {
        out[k] = transformFields(v)
      }
      return out
    }
    return value
  }

  async function writeDocumentRecursive(collectionPath, docId, payload) {
    const { subcollections, ...fields } = payload
    const transformed = transformFields(fields)
    await db.collection(collectionPath).doc(docId).set(transformed, { merge: true })
    writeCount++
    if (subcollections && typeof subcollections === 'object') {
      for (const [subName, subDocs] of Object.entries(subcollections)) {
        for (const [subDocId, subDocData] of Object.entries(subDocs)) {
          await writeDocumentRecursive(`${collectionPath}/${docId}/${subName}`, subDocId, subDocData)
        }
      }
    }
  }

  for (const [collectionName, docs] of Object.entries(collections)) {
    for (const [docId, payload] of Object.entries(docs)) {
      await writeDocumentRecursive(collectionName, docId, payload)
    }
  }

  console.log(`Seed complete. ${writeCount} writes.`)
}

main().catch((err) => {
  console.error(err)
  process.exit(1)
})


