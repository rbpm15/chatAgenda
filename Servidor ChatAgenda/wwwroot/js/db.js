const DB_NAME = 'ChatAgendaDB';
const DB_VERSION = 1;

let db;

const DB = {
    init: function() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(DB_NAME, DB_VERSION);
            
            request.onerror = event => {
                console.error("Database error: " + event.target.errorCode);
                reject(event.target.errorCode);
            };
            
            request.onsuccess = event => {
                db = event.target.result;
                resolve(db);
            };
            
            request.onupgradeneeded = event => {
                db = event.target.result;
                
                // Store for chat messages
                if (!db.objectStoreNames.contains('messages')) {
                    const objectStore = db.createObjectStore('messages', { keyPath: 'id' });
                    objectStore.createIndex('chatId', 'chatId', { unique: false }); 
                    objectStore.createIndex('timestamp', 'timestamp', { unique: false });
                }
                
                // Store for calendar events
                if (!db.objectStoreNames.contains('events')) {
                    const objectStore = db.createObjectStore('events', { keyPath: 'id' });
                    objectStore.createIndex('startTime', 'startTime', { unique: false });
                    objectStore.createIndex('syncStatus', 'syncStatus', { unique: false }); 
                }

                // Store for contacts
                if (!db.objectStoreNames.contains('contacts')) {
                    db.createObjectStore('contacts', { keyPath: 'id' });
                }
            };
        });
    },

    saveMessage: function(msg, chatId) {
        if (!db) return Promise.resolve();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(['messages'], 'readwrite');
            const store = transaction.objectStore('messages');
            
            const localMsg = { ...msg, chatId: chatId };
            const request = store.put(localMsg);
            
            request.onsuccess = () => resolve();
            request.onerror = (e) => reject(e);
        });
    },

    getMessages: function(chatId) {
        if (!db) return Promise.resolve([]);
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(['messages'], 'readonly');
            const store = transaction.objectStore('messages');
            const index = store.index('chatId');
            
            const request = index.getAll(chatId);
            
            request.onsuccess = () => {
                const sorted = request.result.sort((a, b) => new Date(a.timestamp) - new Date(b.timestamp));
                resolve(sorted);
            };
            request.onerror = (e) => reject(e);
        });
    },
    
    saveEvents: function(events) {
        if (!db) return Promise.resolve();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(['events'], 'readwrite');
            const store = transaction.objectStore('events');
            
            events.forEach(ev => store.put(ev));
            
            transaction.oncomplete = () => resolve();
            transaction.onerror = (e) => reject(e);
        });
    },

    saveEvent: function(ev) {
        if (!db) return Promise.resolve();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(['events'], 'readwrite');
            const store = transaction.objectStore('events');
            const request = store.put(ev);
            request.onsuccess = () => resolve();
            request.onerror = (e) => reject(e);
        });
    },

    getEvents: function() {
        if (!db) return Promise.resolve([]);
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(['events'], 'readonly');
            const store = transaction.objectStore('events');
            const request = store.getAll();
            
            request.onsuccess = () => resolve(request.result);
            request.onerror = (e) => reject(e);
        });
    },
    
    getPendingSyncEvents: function() {
        if (!db) return Promise.resolve([]);
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(['events'], 'readonly');
            const store = transaction.objectStore('events');
            const index = store.index('syncStatus');
            const request = index.getAll('PendingUpdate');
            
            request.onsuccess = () => resolve(request.result);
            request.onerror = (e) => reject(e);
        });
    },
    
    deleteEvent: function(id) {
        if (!db) return Promise.resolve();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(['events'], 'readwrite');
            const store = transaction.objectStore('events');
            const request = store.delete(id);
            
            request.onsuccess = () => resolve();
            request.onerror = (e) => reject(e);
        });
    },

    clearEvents: function() {
        if (!db) return Promise.resolve();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(['events'], 'readwrite');
            const store = transaction.objectStore('events');
            const request = store.clear();
            request.onsuccess = () => resolve();
            request.onerror = (e) => reject(e);
        });
    }
};
window.DB = DB;
