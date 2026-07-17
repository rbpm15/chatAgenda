const CACHE_NAME = 'chatagenda-v2';
const ASSETS_TO_CACHE = [
  './',
  './index.html',
  './login.html',
  './css/style.css',
  './js/app.js',
  './js/chat.js',
  './js/calendar.js',
  './js/admin.js',
  './js/db.js',
  './manifest.json'
];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(ASSETS_TO_CACHE))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(keys => Promise.all(
      keys.filter(key => key !== CACHE_NAME)
        .map(key => caches.delete(key))
    )).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', event => {
  if (event.request.method !== 'GET') return;
  
  // No cachear la API para mantenerla actualizada, pero la aplicación web sí se cachea
  if (event.request.url.includes('/api/') || event.request.url.includes('/chatHub')) {
    return;
  }
  
  event.respondWith(
    caches.match(event.request)
      .then(response => {
        return response || fetch(event.request).then(fetchRes => {
          return caches.open(CACHE_NAME).then(cache => {
            // Cacheamos nuevos recursos de forma dinámica
            cache.put(event.request.url, fetchRes.clone());
            return fetchRes;
          });
        });
      }).catch(() => {
        // Fallback offline (ya carga desde cache, esto es si no está en cache y falla red)
      })
  );
});

self.addEventListener('notificationclick', function(event) {
  event.notification.close();
  const appUrl = self.location.origin + '/';
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function(clientList) {
      // Si hay una ventana abierta, enfocarla (y navegar a la raíz si hace falta)
      for (let i = 0; i < clientList.length; i++) {
        const client = clientList[i];
        if ('focus' in client) {
          return client.navigate(appUrl).then(c => c.focus()).catch(() => client.focus());
        }
      }
      // Si no hay ventanas abiertas, abrir una nueva
      return clients.openWindow(appUrl);
    })
  );
});
