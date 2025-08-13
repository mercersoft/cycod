# Firebase Role-Based Access Control (RBAC) Implementation Plan

## Project Overview
Implement a complete role-based access control system for a React/TypeScript application using Firebase Authentication with custom claims, Firestore for role management, and Cloud Functions for secure role administration.

## Prerequisites
- Node.js 18+ installed
- Firebase CLI installed (`npm install -g firebase-tools`)
- Existing React/TypeScript project
- Firebase project created in Firebase Console

## Step 1: Initialize Firebase in Project

### 1.1 Install Firebase Dependencies
```bash
npm install firebase firebase-functions firebase-admin
npm install --save-dev @types/node typescript
```

### 1.2 Initialize Firebase CLI
```bash
firebase login
firebase init
```

Select:
- Functions (TypeScript)
- Firestore
- Hosting (optional)
- Emulators (for local testing)

## Step 2: Project Structure
Create the following directory structure:
```
project-root/
├── functions/
│   ├── src/
│   │   ├── index.ts
│   │   ├── auth/
│   │   │   ├── roleManagement.ts
│   │   │   └── userManagement.ts
│   │   └── triggers/
│   │       └── userTriggers.ts
│   ├── package.json
│   └── tsconfig.json
├── src/
│   ├── config/
│   │   └── firebase.ts
│   ├── hooks/
│   │   └── useAuth.tsx
│   ├── components/
│   │   ├── ProtectedRoute.tsx
│   │   └── AdminDashboard.tsx
│   ├── types/
│   │   └── auth.types.ts
│   └── App.tsx
├── firestore.rules
├── firestore.indexes.json
├── firebase.json
└── .firebaserc
```

## Step 3: Firebase Configuration Files

### 3.1 Create `firebase.json`
```json
{
  "functions": {
    "predeploy": [
      "npm --prefix \"$RESOURCE_DIR\" run lint",
      "npm --prefix \"$RESOURCE_DIR\" run build"
    ],
    "source": "functions"
  },
  "firestore": {
    "rules": "firestore.rules",
    "indexes": "firestore.indexes.json"
  },
  "emulators": {
    "auth": {
      "port": 9099
    },
    "functions": {
      "port": 5001
    },
    "firestore": {
      "port": 8080
    },
    "ui": {
      "enabled": true,
      "port": 4000
    }
  }
}
```

### 3.2 Create `.firebaserc`
```json
{
  "projects": {
    "default": "your-project-id"
  }
}
```

### 3.3 Create `firestore.indexes.json`
```json
{
  "indexes": [
    {
      "collectionGroup": "users",
      "queryScope": "COLLECTION",
      "fields": [
        {
          "fieldPath": "role",
          "order": "ASCENDING"
        },
        {
          "fieldPath": "createdAt",
          "order": "DESCENDING"
        }
      ]
    }
  ],
  "fieldOverrides": []
}
```

## Step 4: Cloud Functions Implementation

### 4.1 Create `functions/package.json`
```json
{
  "name": "functions",
  "scripts": {
    "lint": "eslint --ext .js,.ts .",
    "build": "tsc",
    "serve": "npm run build && firebase emulators:start --only functions",
    "shell": "npm run build && firebase functions:shell",
    "start": "npm run shell",
    "deploy": "firebase deploy --only functions",
    "logs": "firebase functions:log"
  },
  "engines": {
    "node": "18"
  },
  "main": "lib/index.js",
  "dependencies": {
    "firebase-admin": "^11.11.1",
    "firebase-functions": "^4.5.0"
  },
  "devDependencies": {
    "@typescript-eslint/eslint-plugin": "^5.48.0",
    "@typescript-eslint/parser": "^5.48.0",
    "eslint": "^8.31.0",
    "eslint-config-google": "^0.14.0",
    "eslint-plugin-import": "^2.26.0",
    "typescript": "^4.9.4"
  },
  "private": true
}
```

### 4.2 Create `functions/tsconfig.json`
```json
{
  "compilerOptions": {
    "module": "commonjs",
    "noImplicitReturns": true,
    "noUnusedLocals": true,
    "outDir": "lib",
    "sourceMap": true,
    "strict": true,
    "target": "es2017"
  },
  "compileOnSave": true,
  "include": [
    "src"
  ]
}
```

### 4.3 Create `functions/src/index.ts`
```typescript
import * as admin from 'firebase-admin';

// Initialize admin SDK
admin.initializeApp();

// Export all functions
export { setUserRole, createInitialAdmin, getAllUsers, deleteUser } from './auth/roleManagement';
export { onUserCreate, onUserDelete, syncUserRole } from './triggers/userTriggers';
```

### 4.4 Create `functions/src/auth/roleManagement.ts`
```typescript
import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

const VALID_ROLES = ['user', 'moderator', 'admin', 'super-admin'];

/**
 * Set or update a user's role
 */
export const setUserRole = functions.https.onCall(async (data, context) => {
  // Check if request is made by an admin
  if (!context.auth?.token?.admin && context.auth?.token?.role !== 'super-admin') {
    throw new functions.https.HttpsError(
      'permission-denied',
      'Only admins can modify roles'
    );
  }

  const { uid, role } = data;
  
  if (!uid || !role) {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Missing uid or role'
    );
  }

  if (!VALID_ROLES.includes(role)) {
    throw new functions.https.HttpsError(
      'invalid-argument',
      `Invalid role. Must be one of: ${VALID_ROLES.join(', ')}`
    );
  }

  // Prevent regular admins from creating super-admins
  if (role === 'super-admin' && context.auth?.token?.role !== 'super-admin') {
    throw new functions.https.HttpsError(
      'permission-denied',
      'Only super-admins can create other super-admins'
    );
  }

  try {
    // Set custom claims
    const customClaims: any = { role };
    if (role === 'admin' || role === 'super-admin') {
      customClaims.admin = true;
    }
    if (role === 'moderator' || role === 'admin' || role === 'super-admin') {
      customClaims.moderator = true;
    }
    
    await admin.auth().setCustomUserClaims(uid, customClaims);
    
    // Update Firestore
    await admin.firestore().collection('users').doc(uid).set({
      role,
      updatedAt: admin.firestore.FieldValue.serverTimestamp(),
      updatedBy: context.auth?.uid
    }, { merge: true });

    // Log the action
    await admin.firestore().collection('audit_logs').add({
      action: 'role_change',
      targetUid: uid,
      newRole: role,
      performedBy: context.auth?.uid,
      timestamp: admin.firestore.FieldValue.serverTimestamp()
    });

    return { success: true, message: `Role ${role} assigned to user ${uid}` };
  } catch (error: any) {
    console.error('Error setting role:', error);
    throw new functions.https.HttpsError('internal', 'Error setting role');
  }
});

/**
 * Create the initial super-admin user (one-time setup)
 */
export const createInitialAdmin = functions.https.onRequest(async (req, res) => {
  // Check if super-admin already exists
  const admins = await admin.firestore()
    .collection('users')
    .where('role', '==', 'super-admin')
    .limit(1)
    .get();

  if (!admins.empty) {
    res.status(400).json({ error: 'Super-admin already exists' });
    return;
  }

  const { email, password, secretKey } = req.body;
  
  // Verify secret key (set this in Firebase Functions config)
  const configuredSecret = functions.config().admin?.secret || process.env.ADMIN_SECRET;
  if (!configuredSecret || secretKey !== configuredSecret) {
    res.status(403).json({ error: 'Invalid secret key' });
    return;
  }

  if (!email || !password) {
    res.status(400).json({ error: 'Missing email or password' });
    return;
  }

  try {
    // Create user
    const userRecord = await admin.auth().createUser({
      email,
      password,
      emailVerified: true
    });

    // Set super-admin custom claims
    await admin.auth().setCustomUserClaims(userRecord.uid, {
      role: 'super-admin',
      admin: true,
      moderator: true
    });

    // Store in Firestore
    await admin.firestore().collection('users').doc(userRecord.uid).set({
      email,
      role: 'super-admin',
      createdAt: admin.firestore.FieldValue.serverTimestamp()
    });

    res.status(200).json({ 
      success: true,
      message: 'Super-admin created successfully',
      uid: userRecord.uid 
    });
  } catch (error: any) {
    console.error('Error creating admin:', error);
    res.status(500).json({ error: 'Failed to create admin' });
  }
});

/**
 * Get all users with their roles (paginated)
 */
export const getAllUsers = functions.https.onCall(async (data, context) => {
  // Check permissions
  if (!context.auth?.token?.admin && !context.auth?.token?.moderator) {
    throw new functions.https.HttpsError(
      'permission-denied',
      'Only admins and moderators can view all users'
    );
  }

  const { pageSize = 50, pageToken } = data;

  try {
    const listUsersResult = await admin.auth().listUsers(pageSize, pageToken);
    
    const users = await Promise.all(
      listUsersResult.users.map(async (userRecord) => {
        const userDoc = await admin.firestore()
          .collection('users')
          .doc(userRecord.uid)
          .get();
        
        return {
          uid: userRecord.uid,
          email: userRecord.email,
          displayName: userRecord.displayName,
          photoURL: userRecord.photoURL,
          role: userDoc.data()?.role || 'user',
          disabled: userRecord.disabled,
          emailVerified: userRecord.emailVerified,
          createdAt: userRecord.metadata.creationTime,
          lastSignInTime: userRecord.metadata.lastSignInTime
        };
      })
    );
    
    return { 
      users,
      pageToken: listUsersResult.pageToken
    };
  } catch (error: any) {
    console.error('Error fetching users:', error);
    throw new functions.https.HttpsError('internal', 'Error fetching users');
  }
});

/**
 * Delete a user (admin only)
 */
export const deleteUser = functions.https.onCall(async (data, context) => {
  if (!context.auth?.token?.admin) {
    throw new functions.https.HttpsError(
      'permission-denied',
      'Only admins can delete users'
    );
  }

  const { uid } = data;
  
  if (!uid) {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Missing uid'
    );
  }

  try {
    // Delete from Authentication
    await admin.auth().deleteUser(uid);
    
    // Delete from Firestore
    await admin.firestore().collection('users').doc(uid).delete();
    
    // Log the action
    await admin.firestore().collection('audit_logs').add({
      action: 'user_deleted',
      targetUid: uid,
      performedBy: context.auth?.uid,
      timestamp: admin.firestore.FieldValue.serverTimestamp()
    });

    return { success: true, message: `User ${uid} deleted successfully` };
  } catch (error: any) {
    console.error('Error deleting user:', error);
    throw new functions.https.HttpsError('internal', 'Error deleting user');
  }
});
```

### 4.5 Create `functions/src/triggers/userTriggers.ts`
```typescript
import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

/**
 * Trigger when a new user is created
 */
export const onUserCreate = functions.auth.user().onCreate(async (user) => {
  // Create user document with default role
  await admin.firestore().collection('users').doc(user.uid).set({
    email: user.email,
    displayName: user.displayName,
    photoURL: user.photoURL,
    role: 'user',
    createdAt: admin.firestore.FieldValue.serverTimestamp()
  });

  // Set default custom claims
  await admin.auth().setCustomUserClaims(user.uid, {
    role: 'user'
  });

  console.log(`User document created for ${user.uid}`);
});

/**
 * Trigger when a user is deleted
 */
export const onUserDelete = functions.auth.user().onDelete(async (user) => {
  // Clean up user data
  await admin.firestore().collection('users').doc(user.uid).delete();
  
  console.log(`User document deleted for ${user.uid}`);
});

/**
 * Sync role changes from Firestore to Auth custom claims
 */
export const syncUserRole = functions.firestore
  .document('users/{userId}')
  .onUpdate(async (change, context) => {
    const newData = change.after.data();
    const previousData = change.before.data();
    
    if (newData.role !== previousData.role) {
      const customClaims: any = { role: newData.role };
      
      if (newData.role === 'admin' || newData.role === 'super-admin') {
        customClaims.admin = true;
      }
      if (newData.role === 'moderator' || newData.role === 'admin' || newData.role === 'super-admin') {
        customClaims.moderator = true;
      }
      
      try {
        await admin.auth().setCustomUserClaims(context.params.userId, customClaims);
        console.log(`Updated custom claims for user ${context.params.userId} to role ${newData.role}`);
      } catch (error) {
        console.error('Error updating custom claims:', error);
      }
    }
  });
```

## Step 5: Firestore Security Rules

### 5.1 Create `firestore.rules`
```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    
    // Helper functions
    function isSignedIn() {
      return request.auth != null;
    }
    
    function getUserRole() {
      return request.auth.token.role;
    }
    
    function isAdmin() {
      return isSignedIn() && request.auth.token.admin == true;
    }
    
    function isModerator() {
      return isSignedIn() && request.auth.token.moderator == true;
    }
    
    function isSuperAdmin() {
      return isSignedIn() && getUserRole() == 'super-admin';
    }
    
    function isOwner(userId) {
      return isSignedIn() && request.auth.uid == userId;
    }
    
    // Users collection
    match /users/{userId} {
      allow read: if isOwner(userId) || isModerator();
      allow write: if isAdmin();
      allow create: if isOwner(userId) && 
        request.resource.data.role == 'user';
    }
    
    // Audit logs (read-only for admins)
    match /audit_logs/{logId} {
      allow read: if isAdmin();
      allow write: if false; // Only writable via Cloud Functions
    }
    
    // Example: Content that users can CRUD
    match /content/{contentId} {
      allow read: if isSignedIn();
      allow create: if isSignedIn() && 
        request.resource.data.authorId == request.auth.uid;
      allow update: if isOwner(resource.data.authorId) || isModerator();
      allow delete: if isOwner(resource.data.authorId) || isAdmin();
    }
    
    // Admin settings
    match /settings/{document=**} {
      allow read: if isModerator();
      allow write: if isAdmin();
    }
  }
}
```

## Step 6: React Application Files

### 6.1 Create `src/types/auth.types.ts`
```typescript
import { User } from 'firebase/auth';

export type UserRole = 'user' | 'moderator' | 'admin' | 'super-admin';

export interface AuthUser extends User {
  role?: UserRole;
  admin?: boolean;
  moderator?: boolean;
}

export interface UserData {
  uid: string;
  email: string | null;
  displayName?: string | null;
  photoURL?: string | null;
  role: UserRole;
  disabled: boolean;
  emailVerified: boolean;
  createdAt: string;
  lastSignInTime?: string;
}

export interface AuthContextType {
  user: AuthUser | null;
  loading: boolean;
  error: string | null;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
  setUserRole: (uid: string, role: UserRole) => Promise<void>;
  getAllUsers: (pageSize?: number, pageToken?: string) => Promise<{
    users: UserData[];
    pageToken?: string;
  }>;
  deleteUser: (uid: string) => Promise<void>;
}
```

### 6.2 Create `src/config/firebase.ts`
```typescript
import { initializeApp } from 'firebase/app';
import { getAuth, connectAuthEmulator } from 'firebase/auth';
import { getFirestore, connectFirestoreEmulator } from 'firebase/firestore';
import { getFunctions, connectFunctionsEmulator } from 'firebase/functions';

const firebaseConfig = {
  apiKey: process.env.REACT_APP_FIREBASE_API_KEY,
  authDomain: process.env.REACT_APP_FIREBASE_AUTH_DOMAIN,
  projectId: process.env.REACT_APP_FIREBASE_PROJECT_ID,
  storageBucket: process.env.REACT_APP_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: process.env.REACT_APP_FIREBASE_MESSAGING_SENDER_ID,
  appId: process.env.REACT_APP_FIREBASE_APP_ID
};

const app = initializeApp(firebaseConfig);

export const auth = getAuth(app);
export const db = getFirestore(app);
export const functions = getFunctions(app);

// Connect to emulators in development
if (process.env.NODE_ENV === 'development') {
  connectAuthEmulator(auth, 'http://localhost:9099');
  connectFirestoreEmulator(db, 'localhost', 8080);
  connectFunctionsEmulator(functions, 'localhost', 5001);
}
```

### 6.3 Create `.env.local`
```bash
REACT_APP_FIREBASE_API_KEY=your-api-key
REACT_APP_FIREBASE_AUTH_DOMAIN=your-auth-domain
REACT_APP_FIREBASE_PROJECT_ID=your-project-id
REACT_APP_FIREBASE_STORAGE_BUCKET=your-storage-bucket
REACT_APP_FIREBASE_MESSAGING_SENDER_ID=your-sender-id
REACT_APP_FIREBASE_APP_ID=your-app-id
```

## Step 7: Deployment Scripts

### 7.1 Create `scripts/deploy.sh`
```bash
#!/bin/bash

echo "🚀 Starting Firebase RBAC Deployment..."

# Build the React app (if needed)
echo "📦 Building React app..."
npm run build

# Deploy Firestore rules
echo "📜 Deploying Firestore rules..."
firebase deploy --only firestore:rules

# Deploy Firestore indexes
echo "📇 Deploying Firestore indexes..."
firebase deploy --only firestore:indexes

# Deploy Cloud Functions
echo "☁️ Deploying Cloud Functions..."
cd functions
npm install
npm run build
cd ..
firebase deploy --only functions

echo "✅ Deployment complete!"
```

### 7.2 Create `scripts/create-admin.sh`
```bash
#!/bin/bash

echo "Creating initial super-admin..."
echo "Enter admin email:"
read EMAIL
echo "Enter admin password:"
read -s PASSWORD
echo "Enter secret key:"
read -s SECRET

curl -X POST \
  https://us-central1-YOUR-PROJECT-ID.cloudfunctions.net/createInitialAdmin \
  -H 'Content-Type: application/json' \
  -d "{
    \"email\": \"$EMAIL\",
    \"password\": \"$PASSWORD\",
    \"secretKey\": \"$SECRET\"
  }"
```

### 7.3 Create `scripts/setup-local.sh`
```bash
#!/bin/bash

echo "🔧 Setting up local development environment..."

# Install dependencies
echo "📦 Installing dependencies..."
npm install
cd functions && npm install && cd ..

# Build functions
echo "🔨 Building functions..."
cd functions && npm run build && cd ..

# Start emulators
echo "🚀 Starting Firebase emulators..."
firebase emulators:start
```

## Step 8: Testing Script

### 8.1 Create `scripts/test-roles.js`
```javascript
const admin = require('firebase-admin');
const serviceAccount = require('../service-account-key.json');

admin.initializeApp({
  credential: admin.credential.cert(serviceAccount)
});

async function testRoles() {
  const testEmail = 'test@example.com';
  
  try {
    // Create test user
    const user = await admin.auth().createUser({
      email: testEmail,
      password: 'testpassword123'
    });
    
    console.log('✅ User created:', user.uid);
    
    // Set custom claims
    await admin.auth().setCustomUserClaims(user.uid, {
      role: 'admin',
      admin: true
    });
    
    console.log('✅ Custom claims set');
    
    // Verify claims
    const updatedUser = await admin.auth().getUser(user.uid);
    console.log('✅ Custom claims:', updatedUser.customClaims);
    
    // Clean up
    await admin.auth().deleteUser(user.uid);
    console.log('✅ Test user deleted');
    
  } catch (error) {
    console.error('❌ Error:', error);
  }
  
  process.exit(0);
}

testRoles();
```

## Step 9: Deployment Checklist

1. **Set up Firebase project**
   ```bash
   firebase projects:create your-project-id
   firebase use your-project-id
   ```

2. **Set Functions config (for secret key)**
   ```bash
   firebase functions:config:set admin.secret="your-super-secret-key"
   ```

3. **Enable Authentication in Firebase Console**
   - Go to Firebase Console > Authentication
   - Enable Email/Password provider

4. **Deploy everything**
   ```bash
   chmod +x scripts/deploy.sh
   ./scripts/deploy.sh
   ```

5. **Create first super-admin**
   ```bash
   chmod +x scripts/create-admin.sh
   ./scripts/create-admin.sh
   ```

6. **Test locally with emulators**
   ```bash
   chmod +x scripts/setup-local.sh
   ./scripts/setup-local.sh
   ```

## Step 10: Implementation Order

1. **Backend First**
   - Set up Firebase project and CLI
   - Implement and deploy Cloud Functions
   - Deploy Firestore rules and indexes
   - Create initial super-admin

2. **Frontend Integration**
   - Install Firebase SDK
   - Create Firebase config
   - Implement useAuth hook
   - Create ProtectedRoute component
   - Build AdminDashboard
   - Integrate with App.tsx

3. **Testing**
   - Test with Firebase emulators
   - Verify role assignments
   - Test security rules
   - Check token refresh

4. **Production**
   - Deploy to production
   - Monitor Cloud Functions logs
   - Set up error alerting

## Monitoring Commands

```bash
# View Functions logs
firebase functions:log

# View specific function logs
firebase functions:log --only setUserRole

# Run Firestore rules tests
npm test

# Check deployment status
firebase deploy --only functions --dry-run
```

## Security Notes

1. **Never expose service account keys** - Add `service-account-key.json` to `.gitignore`
2. **Use environment variables** for sensitive config
3. **Implement rate limiting** on Cloud Functions
4. **Enable App Check** for additional security
5. **Regular audit** of user roles and permissions
6. **Monitor** Cloud Functions invocations and costs

## Troubleshooting

### Common Issues:

1. **"Permission denied" errors**
   - Check if custom claims are properly set
   - Verify Firestore rules syntax
   - Ensure token is refreshed after role change

2. **Functions not deploying**
   - Check Node.js version (must be 18+)
   - Verify TypeScript compilation
   - Check functions/package.json dependencies

3. **Custom claims not updating**
   - Force token refresh in client
   - Check Cloud Functions logs
   - Verify syncUserRole trigger is working

4. **Emulators not working**
   - Check if ports are available
   - Verify Java is installed (required for emulators)
   - Clear emulator data: `firebase emulators:start --clear`

## Additional Resources

- [Firebase Custom Claims Documentation](https://firebase.google.com/docs/auth/admin/custom-claims)
- [Firestore Security Rules](https://firebase.google.com/docs/firestore/security/get-started)
- [Cloud Functions Best Practices](https://firebase.google.com/docs/functions/best-practices)
- [Firebase Admin SDK](https://firebase.google.com/docs/admin/setup)