// IndexedDB helper functions for enhanced browser storage
window.initializeIndexedDb = function(dbName) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, 1);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
        
        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            if (!db.objectStoreNames.contains('data')) {
                db.createObjectStore('data', { keyPath: 'key' });
            }
        };
    });
};

window.indexedDbGet = function(dbName, key) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, 1);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => {
            const db = request.result;
            const transaction = db.transaction(['data'], 'readonly');
            const store = transaction.objectStore('data');
            const getRequest = store.get(key);
            
            getRequest.onerror = () => reject(getRequest.error);
            getRequest.onsuccess = () => {
                const result = getRequest.result;
                resolve(result ? result.value : null);
            };
        };
    });
};

window.indexedDbSet = function(dbName, key, value) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, 1);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => {
            const db = request.result;
            const transaction = db.transaction(['data'], 'readwrite');
            const store = transaction.objectStore('data');
            const putRequest = store.put({ key: key, value: value, timestamp: new Date().toISOString() });
            
            putRequest.onerror = () => reject(putRequest.error);
            putRequest.onsuccess = () => resolve();
        };
    });
};

window.indexedDbDelete = function(dbName, key) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, 1);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => {
            const db = request.result;
            const transaction = db.transaction(['data'], 'readwrite');
            const store = transaction.objectStore('data');
            const deleteRequest = store.delete(key);
            
            deleteRequest.onerror = () => reject(deleteRequest.error);
            deleteRequest.onsuccess = () => resolve();
        };
    });
};

window.scrollToBottom = function(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};