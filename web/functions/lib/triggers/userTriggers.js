"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.syncUserRole = exports.onUserDelete = exports.onUserCreate = void 0;
const functions = require("firebase-functions");
const admin = require("firebase-admin");
exports.onUserCreate = functions.auth.user().onCreate(async (user) => {
    await admin.firestore().collection('users').doc(user.uid).set({
        email: user.email,
        displayName: user.displayName,
        photoURL: user.photoURL,
        role: 'user',
        createdAt: admin.firestore.FieldValue.serverTimestamp(),
    });
    await admin.auth().setCustomUserClaims(user.uid, { role: 'user' });
});
exports.onUserDelete = functions.auth.user().onDelete(async (user) => {
    await admin.firestore().collection('users').doc(user.uid).delete();
});
exports.syncUserRole = functions.firestore
    .document('users/{userId}')
    .onUpdate(async (change, context) => {
    const newData = change.after.data();
    const previousData = change.before.data();
    if (newData.role !== previousData.role && newData.role) {
        const customClaims = { role: newData.role };
        if (newData.role === 'admin' || newData.role === 'super-admin')
            customClaims.admin = true;
        if (newData.role === 'moderator' || newData.role === 'admin' || newData.role === 'super-admin')
            customClaims.moderator = true;
        try {
            await admin.auth().setCustomUserClaims(context.params.userId, customClaims);
        }
        catch (error) {
            console.error('Error updating custom claims:', error);
        }
    }
});
//# sourceMappingURL=userTriggers.js.map