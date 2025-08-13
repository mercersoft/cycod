import * as admin from 'firebase-admin';

// Initialize admin SDK
admin.initializeApp();

export { setUserRole, createInitialAdmin, getAllUsers, deleteUser } from './auth/roleManagement';
export { onUserCreate, onUserDelete, syncUserRole } from './triggers/userTriggers';


