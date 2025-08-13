"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.deleteUser = exports.getAllUsers = exports.createInitialAdmin = exports.setUserRole = void 0;
const functions = require("firebase-functions");
const admin = require("firebase-admin");
const VALID_ROLES = ['user', 'moderator', 'admin', 'super-admin'];
exports.setUserRole = functions.https.onCall(async (data, context) => {
    if (!context.auth?.token?.admin && context.auth?.token?.role !== 'super-admin') {
        throw new functions.https.HttpsError('permission-denied', 'Only admins can modify roles');
    }
    const { uid, role } = data;
    if (!uid || !role) {
        throw new functions.https.HttpsError('invalid-argument', 'Missing uid or role');
    }
    if (!VALID_ROLES.includes(role)) {
        throw new functions.https.HttpsError('invalid-argument', `Invalid role. Must be one of: ${VALID_ROLES.join(', ')}`);
    }
    if (role === 'super-admin' && context.auth?.token?.role !== 'super-admin') {
        throw new functions.https.HttpsError('permission-denied', 'Only super-admins can create other super-admins');
    }
    try {
        const customClaims = { role };
        if (role === 'admin' || role === 'super-admin')
            customClaims.admin = true;
        if (role === 'moderator' || role === 'admin' || role === 'super-admin')
            customClaims.moderator = true;
        await admin.auth().setCustomUserClaims(uid, customClaims);
        await admin.firestore().collection('users').doc(uid).set({
            role,
            updatedAt: admin.firestore.FieldValue.serverTimestamp(),
            updatedBy: context.auth?.uid ?? null,
        }, { merge: true });
        await admin.firestore().collection('audit_logs').add({
            action: 'role_change',
            targetUid: uid,
            newRole: role,
            performedBy: context.auth?.uid ?? null,
            timestamp: admin.firestore.FieldValue.serverTimestamp(),
        });
        return { success: true, message: `Role ${role} assigned to user ${uid}` };
    }
    catch (error) {
        console.error('Error setting role:', error);
        throw new functions.https.HttpsError('internal', 'Error setting role');
    }
});
exports.createInitialAdmin = functions.https.onRequest(async (req, res) => {
    try {
        const admins = await admin.firestore().collection('users').where('role', '==', 'super-admin').limit(1).get();
        if (!admins.empty) {
            res.status(400).json({ error: 'Super-admin already exists' });
            return;
        }
        const { email, password, secretKey } = req.body || {};
        const configuredSecret = functions.config().admin?.secret || process.env.ADMIN_SECRET;
        if (!configuredSecret || secretKey !== configuredSecret) {
            res.status(403).json({ error: 'Invalid secret key' });
            return;
        }
        if (!email || !password) {
            res.status(400).json({ error: 'Missing email or password' });
            return;
        }
        const userRecord = await admin.auth().createUser({ email, password, emailVerified: true });
        await admin.auth().setCustomUserClaims(userRecord.uid, { role: 'super-admin', admin: true, moderator: true });
        await admin.firestore().collection('users').doc(userRecord.uid).set({
            email,
            role: 'super-admin',
            createdAt: admin.firestore.FieldValue.serverTimestamp(),
        });
        res.status(200).json({ success: true, message: 'Super-admin created successfully', uid: userRecord.uid });
    }
    catch (error) {
        console.error('Error creating admin:', error);
        res.status(500).json({ error: 'Failed to create admin' });
    }
});
exports.getAllUsers = functions.https.onCall(async (data, context) => {
    if (!context.auth?.token?.admin && !context.auth?.token?.moderator) {
        throw new functions.https.HttpsError('permission-denied', 'Only admins and moderators can view all users');
    }
    const { pageSize = 50, pageToken } = (data || {});
    try {
        const listUsersResult = await admin.auth().listUsers(pageSize, pageToken);
        const users = await Promise.all(listUsersResult.users.map(async (userRecord) => {
            const userDoc = await admin.firestore().collection('users').doc(userRecord.uid).get();
            return {
                uid: userRecord.uid,
                email: userRecord.email,
                displayName: userRecord.displayName,
                photoURL: userRecord.photoURL,
                role: userDoc.data()?.role || 'user',
                disabled: userRecord.disabled,
                emailVerified: userRecord.emailVerified,
                createdAt: userRecord.metadata.creationTime,
                lastSignInTime: userRecord.metadata.lastSignInTime,
            };
        }));
        return { users, pageToken: listUsersResult.pageToken };
    }
    catch (error) {
        console.error('Error fetching users:', error);
        throw new functions.https.HttpsError('internal', 'Error fetching users');
    }
});
exports.deleteUser = functions.https.onCall(async (data, context) => {
    if (!context.auth?.token?.admin) {
        throw new functions.https.HttpsError('permission-denied', 'Only admins can delete users');
    }
    const { uid } = (data || {});
    if (!uid)
        throw new functions.https.HttpsError('invalid-argument', 'Missing uid');
    try {
        await admin.auth().deleteUser(uid);
        await admin.firestore().collection('users').doc(uid).delete();
        await admin.firestore().collection('audit_logs').add({
            action: 'user_deleted',
            targetUid: uid,
            performedBy: context.auth?.uid ?? null,
            timestamp: admin.firestore.FieldValue.serverTimestamp(),
        });
        return { success: true, message: `User ${uid} deleted successfully` };
    }
    catch (error) {
        console.error('Error deleting user:', error);
        throw new functions.https.HttpsError('internal', 'Error deleting user');
    }
});
//# sourceMappingURL=roleManagement.js.map