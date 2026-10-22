# Avanzamento – restyling e funzionalità dei mock

Piano: `C:\Users\ltarq\.claude\plans\voglio-che-prepari-un-abundant-quail.md` · branch `feature/admin-console` · niente push.

## Stato delle fasi

| Fase | Stato | Commit |
|---|---|---|
| 0 Preparazione | ✅ | vedi log |
| 1 Design system e shell | ✅ | vedi log |
| 2 Chiusura perimetro (UC1-S4, UC4, UC5, conteggi, UC11) | ✅ | vedi log |
| 3 Audit log e storico | ✅ | vedi log |
| 4 Ciclo di vita e wizard | ⏳ | |
| 5 Moduli, piani, sottoscrizioni | | |
| 6 Gestione utenti completa | | |
| 7 Elenco clienti avanzato | | |
| 8 Fatturazione interna | | |
| 9 Uso e stato dei servizi | | |
| 10 Comunicazioni e notifiche | | |
| 11 Panoramica, ricerca, impersonificazione | | |
| 12 Consolidamento | | |

## Test rossi attesi

Nessuno: dopo la fase 3 tutta la suite è verde (accettazione 107/107).

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
8. **Pagine di modifica anagrafica e organizzazione** sostituite dalle schede Anagrafica e Organizzazione della scheda cliente (`/tenants/{id}?tab=registry|organization`); la conferma con `window.confirm` alla rimozione di profili è sostituita dall'avviso nella barra delle modifiche (mock 1d).
9. **Matrice dell'organizzazione**: nomi delle unità e dei profili modificabili direttamente nelle intestazioni della matrice (il mock mostra testo statico ma UC5 richiede la rinomina).
10. **UC5 con Keycloak giù**: l'organizzazione si salva comunque; gli utenti tengono un id di profilo che le letture già omettono (UC7-R3).
11. **Audit registrato dopo il salvataggio**, non nella stessa transazione (le scritture Keycloak non sono transazionali comunque): se la registrazione fallisce la modifica resta e l'errore va nel log (UC12 flusso 1a).
12. **Creazione utente nello storico**: un evento per ogni cliente dei profili assegnati, così compare nello storico di ciascuno (mock 1b "ha creato 4 utenti per Officine Rinaldi").

## Da far vedere al designer

- Tema scuro, vista mobile (sidebar a scomparsa, tabelle con scorrimento orizzontale).
- Pagine senza mock: Profili, Impostazioni, 404 (e in seguito Moduli, Piani, Sottoscrizioni, Fatturazione, Comunicazioni, Audit log).
