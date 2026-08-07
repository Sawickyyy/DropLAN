const CACHE_NAME = "droplan-shell-v0.4.0";
const SHELL = [
  "/",
  "/manifest.webmanifest",
  "/icon-192.png",
  "/icon-512.png",
  "/apple-touch-icon.png"
];

self.addEventListener("install", event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(SHELL))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener("activate", event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(
        keys
          .filter(key => key !== CACHE_NAME)
          .map(key => caches.delete(key))
      ))
      .then(() => self.clients.claim())
  );
});

self.addEventListener("fetch", event => {
  const request = event.request;

  if (request.method !== "GET")
    return;

  // API/transfery mają zawsze iść bezpośrednio do aktywnego PC.
  const url = new URL(request.url);

  if (
    url.pathname.startsWith("/api/") ||
    url.pathname.startsWith("/download/") ||
    url.pathname === "/events"
  ) {
    return;
  }

  event.respondWith(
    fetch(request)
      .then(response => {
        const copy = response.clone();

        caches.open(CACHE_NAME)
          .then(cache => cache.put(request, copy));

        return response;
      })
      .catch(() => caches.match(request))
  );
});
