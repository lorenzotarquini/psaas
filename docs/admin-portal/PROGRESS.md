# Avanzamento – restyling e funzionalità dei mock

Piano: `C:\Users\ltarq\.claude\plans\voglio-che-prepari-un-abundant-quail.md` · branch `feature/admin-console` · niente push.

## Stato delle fasi

| Fase | Stato | Commit |
|---|---|---|
| 0 Preparazione | ✅ | vedi log |
| 1 Design system e shell | ✅ | vedi log |
| 2 Chiusura perimetro (UC1-S4, UC4, UC5, conteggi, UC11) | ⏳ | |
| 3 Audit log e storico | | |
| 4 Ciclo di vita e wizard | | |
| 5 Moduli, piani, sottoscrizioni | | |
| 6 Gestione utenti completa | | |
| 7 Elenco clienti avanzato | | |
| 8 Fatturazione interna | | |
| 9 Uso e stato dei servizi | | |
| 10 Comunicazioni e notifiche | | |
| 11 Panoramica, ricerca, impersonificazione | | |
| 12 Consolidamento | | |

## Test rossi attesi

UC1-S4, UC4 (6), UC5 (7), GEN-S1/S2 sulle `PUT /api/tenants` (4): da chiudere nella fase 2.

## Ambiente di sviluppo autonomo (fuori dal repo, nello scratchpad della sessione)

Per verificare l'interfaccia senza il login reale: provider OIDC locale (`devidp/server.mjs`, porta 8099: login automatico come "Marta Russo", token RS256, sottoinsieme dell'API utenti di Keycloak in memoria), Postgres dedicato (container `psaas-dev-pg`, porta 5433), backend su 5300 (`backend.sh`), Vite su 5180 (`VITE_KEYCLOAK_URL=http://localhost:8099`), screenshot con `e2e/look.mjs`, dati d'esempio con `e2e/seed.mjs`.

## Decisioni prese in autonomia

1. **Ragione sociale ≤ 20 caratteri (UC3-R2) mantenuta**: alcuni nomi dei mock la superano ("Acciaierie Ventura Spa", 22). Nel seed sono accorciati. Da chiarire con il designer o rivedere la regola.
2. **Tema scuro `psaas-dark`** derivato dagli stessi toni (i mock non lo includono): da validare con il designer.
3. **Font IBM Plex self-hosted** con `@fontsource` invece di Google Fonts.
4. **UC8 (chi sono)** spostato nel menu dell'avatar: ID Keycloak e ruoli (client e realm) dal token.
5. **Piè di pagina della sidebar**: versione del portale (`package.json`, 0.2.0) e realm; diventerà la versione del modulo core con la fase 5.
6. **Badge d'ambiente** da `VITE_ENVIRONMENT_LABEL` (`.env`: `SVILUPPO`).
7. **Tema scelto persistito** in localStorage (`psaas-theme`); senza scelta segue il sistema.

## Da far vedere al designer

- Tema scuro, vista mobile (sidebar a scomparsa, tabelle con scorrimento orizzontale).
- Pagine senza mock: Profili, Impostazioni, 404 (e in seguito Moduli, Piani, Sottoscrizioni, Fatturazione, Comunicazioni, Audit log).
