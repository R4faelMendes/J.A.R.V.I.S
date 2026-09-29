/* JARVIS 2.0 service worker — cache local para funcionar como PWA offline na rede local. */
const CACHE = "jarvis-20-v1";
const NUCLEO = ["/", "/static/style.css", "/static/app.js", "/static/manifest.json"];
self.addEventListener("install", (e) => {
  e.waitUntil(caches.open(CACHE).then((c) => c.addAll(NUCLEO)).then(() => self.skipWaiting()));
});
self.addEventListener("fetch", (e) => {
  if (e.request.method !== "GET" || !e.request.url.startsWith(self.location.origin)) return;
  e.respondWith(caches.match(e.request).then((r) => r || fetch(e.request)));
});
