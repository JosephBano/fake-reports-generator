/**
 * Mock in-memory de `localStorage` para tests.
 *
 * El runner de Vitest + jsdom que usa `@angular/build:unit-test` no expone
 * `localStorage` en `globalThis`/`window` por defecto, así que este helper
 * crea uno propio basado en `Map` para no contaminar el entorno.
 */
export interface LocalStorageMock {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
  clear(): void;
  key(index: number): string | null;
  readonly length: number;
}

export function crearLocalStorageMock(): LocalStorageMock {
  const store = new Map<string, string>();
  return {
    getItem: (k) => store.get(k) ?? null,
    setItem: (k, v) => { store.set(k, v); },
    removeItem: (k) => { store.delete(k); },
    clear: () => { store.clear(); },
    key: (i) => Array.from(store.keys())[i] ?? null,
    get length() { return store.size; }
  };
}

export function instalarLocalStorageMock(): LocalStorageMock {
  const mock = crearLocalStorageMock();
  (globalThis as unknown as { localStorage: LocalStorageMock }).localStorage = mock;
  if (typeof window !== 'undefined') {
    (window as unknown as { localStorage: LocalStorageMock }).localStorage = mock;
  }
  return mock;
}