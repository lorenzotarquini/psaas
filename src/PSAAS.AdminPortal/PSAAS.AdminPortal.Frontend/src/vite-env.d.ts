interface ImportMetaEnv {
  readonly VITE_KEYCLOAK_URL: string
  readonly VITE_KEYCLOAK_REALM: string
  readonly VITE_KEYCLOAK_CLIENT_ID: string
  /** Topbar badge (e.g. PRODUZIONE, SVILUPPO); none when empty. */
  readonly VITE_ENVIRONMENT_LABEL?: string
}

/** package.json version, injected by vite.config.ts. */
declare const __APP_VERSION__: string

interface ImportMeta {
  readonly env: ImportMetaEnv
}
