"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.syncUserRole = exports.onUserDelete = exports.onUserCreate = exports.deleteUser = exports.getAllUsers = exports.createInitialAdmin = exports.setUserRole = void 0;
const admin = require("firebase-admin");
// Initialize admin SDK
admin.initializeApp();
var roleManagement_1 = require("./auth/roleManagement");
Object.defineProperty(exports, "setUserRole", { enumerable: true, get: function () { return roleManagement_1.setUserRole; } });
Object.defineProperty(exports, "createInitialAdmin", { enumerable: true, get: function () { return roleManagement_1.createInitialAdmin; } });
Object.defineProperty(exports, "getAllUsers", { enumerable: true, get: function () { return roleManagement_1.getAllUsers; } });
Object.defineProperty(exports, "deleteUser", { enumerable: true, get: function () { return roleManagement_1.deleteUser; } });
var userTriggers_1 = require("./triggers/userTriggers");
Object.defineProperty(exports, "onUserCreate", { enumerable: true, get: function () { return userTriggers_1.onUserCreate; } });
Object.defineProperty(exports, "onUserDelete", { enumerable: true, get: function () { return userTriggers_1.onUserDelete; } });
Object.defineProperty(exports, "syncUserRole", { enumerable: true, get: function () { return userTriggers_1.syncUserRole; } });
//# sourceMappingURL=index.js.map