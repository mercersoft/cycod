import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

export const onUserCreate = functions.auth.user().onCreate(async (user) => {
  await admin.firestore().collection('users').doc(user.uid).set({
    email: user.email,
    displayName: user.displayName,
    photoURL: user.photoURL,
    role: 'user',
    createdAt: admin.firestore.FieldValue.serverTimestamp(),
  });

  await admin.auth().setCustomUserClaims(user.uid, { role: 'user' });
});

export const onUserDelete = functions.auth.user().onDelete(async (user) => {
  await admin.firestore().collection('users').doc(user.uid).delete();
});

export const syncUserRole = functions.firestore
  .document('users/{userId}')
  .onUpdate(async (change, context) => {
    const newData = change.after.data() as { role?: string };
    const previousData = change.before.data() as { role?: string };

    if (newData.role !== previousData.role && newData.role) {
      const customClaims: Record<string, unknown> = { role: newData.role };
      if (newData.role === 'admin' || newData.role === 'super-admin') customClaims.admin = true;
      if (newData.role === 'moderator' || newData.role === 'admin' || newData.role === 'super-admin') customClaims.moderator = true;
      try {
        await admin.auth().setCustomUserClaims(context.params.userId, customClaims);
      } catch (error) {
        console.error('Error updating custom claims:', error);
      }
    }
  });


