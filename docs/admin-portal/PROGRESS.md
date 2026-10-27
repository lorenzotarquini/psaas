# Avanzamento – restyling e funzionalità dei mock

Piano: `C:\Users\ltarq\.claude\plans\voglio-che-prepari-un-abundant-quail.md` · branch `feature/admin-console` · niente push.

## Stato delle fasi

| Fase | Stato | Commit |
|---|---|---|
| 0 Preparazione | ✅ | vedi log |
| 1 Design system e shell | ✅ | vedi log |
| 2 Chiusura perimetro (UC1-S4, UC4, UC5, conteggi, UC11) | ✅ | vedi log |
| 3 Audit log e storico | ✅ | vedi log |
| 4 Ciclo di vita e wizard | ✅ | vedi log |
| 5 Moduli, piani, sottoscrizioni | ✅ | vedi log |
| 6 Gestione utenti completa | ✅ | vedi log |
| 7 Elenco clienti avanzato | ✅ | vedi log |
| 8 Fatturazione interna | ✅ | vedi log |
| 9 Uso e stato dei servizi | ✅ | vedi log |
| 10 Comunicazioni e notifiche | ✅ | vedi log |
| 11 Panoramica, ricerca, impersonificazione | ✅ | vedi log |
| 12 Consolidamento | ✅ | vedi log |

## Test rossi attesi

Nessuno. A fine lavoro (fase 12): accettazione 267/267, unitari Application 146, Infrastructure 55, Domain 25, Auth 5; Vitest 69 in 26 file; `tsc` pulito; lint senza avvisi di oxlint (restano 7 suggerimenti "insignificant slice" di steiger, vedi decisione 62). Collection Postman eseguita per intero con newman contro il backend locale: 63/63 asserzioni, ripetibile.

## Ambiente di sviluppo autonomo (fuori dal repo, nello scratchpad della sessione)

Per verificare l'interfaccia senza il login reale: provider OIDC locale (`devidp/server.mjs`, porta 8099: login automatico come "Marta Russo", token RS256, sottoinsieme dell'API utenti di Keycloak in memoria), Postgres dedicato (container `psaas-dev-pg`, porta 5433), backend su 5300 (`backend.sh`), Vite su 5180 (`VITE_KEYCLOAK_URL=http://localhost:8099`), screenshot con `e2e/look.mjs`, dati d'esempio con `e2e/seed.mjs` e `e2e/seed-catalog.mjs` (moduli, piani, sottoscrizioni; l'hook di Magazzino punta a una porta chiusa per mostrare il provisioning fallito).

## Decisioni prese in autonomia

1. **Ragione sociale ≤ 20 caratteri (UC3-R2) mantenuta**: alcuni nomi dei mock la superano ("Acciaierie Ventura Spa", 22). Nel seed sono accorciati. Da chiarire con il designer o rivedere la regola.
2. **Tema scuro `psaas-dark`** derivato dagli stessi toni (i mock non lo includono): da validare con il designer.
3. **Font IBM Plex self-hosted** con `@fontsource` invece di Google Fonts.
4. **UC8 (chi sono)** spostato nel menu dell'avatar: ID Keycloak e ruoli (client e realm) dal token.
5. **Piè di pagina della sidebar**: versione del modulo core (`core 2.0.3`, mock 1b) e realm; la versione del portale resta nel tooltip.
6. **Badge d'ambiente** da `VITE_ENVIRONMENT_LABEL` (`.env`: `SVILUPPO`).
7. **Tema scelto persistito** in localStorage (`psaas-theme`); senza scelta segue il sistema.
8. **Pagine di modifica anagrafica e organizzazione** sostituite dalle schede Anagrafica e Organizzazione della scheda cliente (`/tenants/{id}?tab=registry|organization`); la conferma con `window.confirm` alla rimozione di profili è sostituita dall'avviso nella barra delle modifiche (mock 1d).
9. **Matrice dell'organizzazione**: nomi delle unità e dei profili modificabili direttamente nelle intestazioni della matrice (il mock mostra testo statico ma UC5 richiede la rinomina).
10. **UC5 con Keycloak giù**: l'organizzazione si salva comunque; gli utenti tengono un id di profilo che le letture già omettono (UC7-R3).
11. **Audit registrato dopo il salvataggio**, non nella stessa transazione (le scritture Keycloak non sono transazionali comunque): se la registrazione fallisce la modifica resta e l'errore va nel log (UC12 flusso 1a).
12. **Creazione utente nello storico**: un evento per ogni cliente dei profili assegnati, così compare nello storico di ciascuno (mock 1b "ha creato 4 utenti per Officine Rinaldi").

13. **Wizard, passo Piano**: completato in fase 5 (UC17 sulla bozza); senza piano scelto si prosegue e il controllo "Piano" resta grigio.
14. **Passo Primi utenti**: gli utenti si creano uno alla volta; una riga rifiutata mostra gli errori e ferma il passo, quelle già create restano.
15. **Annullare l'onboarding** elimina la bozza (profili e unità compresi); gli utenti già creati restano in Keycloak (detto nel dialog).
16. **Referente commerciale**: testo libero precompilato con l'amministratore corrente (il mock mostra una select "Marta Russo (tu)"; non esiste ancora un elenco di amministratori).
17. **Prezzo bloccato senza versioni dei piani**: la sottoscrizione copia prezzo, periodicità e limiti; modificare un piano vale solo per le nuove sottoscrizioni. Più semplice delle versioni di piano (grandfathering) del piano di lavoro, stesso effetto per il cliente.
18. **Manifest incollato come JSON** nella pagina Moduli (nessun upload di file né registro remoto); un id già presente aggiorna il modulo.
19. **Limiti con Keycloak giù**: la sottoscrizione non viene bloccata se non si possono contare gli utenti (warning nel log); il limite viene ricontrollato a ogni nuovo utente (UC6).
20. **Cambio piano**: i moduli non più inclusi vengono tolti subito (nessun hook di deprovisioning chiamato per ora: `deprovisionUrl` è solo registrato).
21. **Icone dei moduli**: il manifest indica un nome (`warehouse`, `hr`, `maintenance`…), mappato su icone Font Awesome; nomi sconosciuti → pezzo di puzzle.
22. **Stato "Sospeso" dell'utente** calcolato: attivo in Keycloak ma tutti i clienti dei suoi profili sono sospesi o cessati. Nessuna sospensione manuale (il mock ha solo "Disattiva").
23. **Invito in sospeso** = azione richiesta `UPDATE_PASSWORD` impostata alla creazione; contatore inviti e autore negli attributi Keycloak (`invitesSent`, `lastInviteAt`, `createdBy`): vanno dichiarati nel *User profile*.
24. **Azioni del pannello sempre visibili** (come nel mock), disabilitate quando lo stato non le consente; il backend risponde `409`.
25. **Filtri e pagine utenti in memoria** sui primi 1000 risultati di Keycloak (shortcut annotato nel codice).
26. **Ultimo accesso** dagli eventi `LOGIN` di Keycloak, una richiesta per utente della pagina; se gli eventi sono spenti resta "mai" senza errori.
27. **Import CSV**: virgola o punto e virgola, profili come `Cliente:Profilo` separati da `|`, intestazioni in italiano o inglese; la conferma crea solo le righe valide.
28. **Scheda Utenti del cliente**: la riga apre l'utente nella vista Utenti (filtrata sul cliente) invece di un secondo pannello.
29. **Provincia nella sede**: salvata nell'indirizzo (JSON) dalla lista dei comuni al salvataggio dell'anagrafica; i clienti già esistenti la ricevono al primo salvataggio. In locale è stata riempita ri-salvando le anagrafiche (`e2e/backfill-province.mjs`).
30. **Conteggi delle schede** su tutti i clienti, non sui filtri correnti (il mock non lo chiarisce).
31. **"Cambia piano" multiplo** dal frontend, una sottoscrizione (UC17) per cliente con esito per riga; nessun endpoint di massa. Gli add-on dei clienti vengono tolti.
32. **"Invia comunicazione"** visibile ma disattivato fino alla fase 10.
33. **Colonne scelte** salvate nel browser (`psaas-tenant-columns`); le viste salvate (UC22) sono invece sul server, per amministratore.
34. **Export senza conteggio utenti** (eviterebbe migliaia di chiamate a Keycloak); valori che iniziano con `=+-@` neutralizzati contro la formula injection.
35. **Utenti nell'elenco** contati con Keycloak solo per la pagina visibile (shortcut annotato); niente ordinamento per utenti.
36. **Fatture emesse "oggi"** anche per periodi arretrati recuperati dal processo giornaliero: la scadenza parte dall'emissione.
37. **Pro-rata su base giornaliera** (prezzo / 30 o 365 giorni) per i giorni che restano; nessuna fattura sotto il centesimo; gli add-on non hanno prezzo.
38. **Note di credito già "Pagate"** (compensate) e importi sempre positivi: è il tipo a dire che è uno storno.
39. **Niente riattivazione automatica** dopo il pagamento di un cliente sospeso per morosità: resta all'amministratore (UC13).
40. **Solleciti registrati** sulla fattura e nello storico; l'email partirà con le comunicazioni (fase 10).
41. **Orologio dei test di accettazione** spostabile (`FakeTimeProvider`); i token JWT restano validati sull'ora reale.
42. **Pagina Fatturazione** disegnata con il sistema 1a (non c'è un mock): da far vedere al designer.
43. **Invio dell'uso con ruolo `UsageReporter`** sul client `adminportal-api` (service account del modulo), endpoint mappato fuori dai controller per non ereditare la policy `Administrator`; nessuna chiave per modulo da gestire nel portale.
44. **Uso aggregato per giorno** (contatori sommati, misure all'ultimo valore); niente serie orarie.
45. **Utenti attivi** dall'ultimo accesso Keycloak (eventi LOGIN): leggono gli utenti del realm e filtrano per profilo (shortcut annotato).
46. **Stato dei servizi in Impostazioni** finché la Panoramica (fase 11) non lo ospita; ricontrollato ogni minuto mentre è aperto.
47. **Hook dei moduli "Degradato"** quando un provisioning è fallito, invece di chiamare gli URL dei moduli (eviterebbe effetti collaterali sugli hook).
48. **Email alla PEC del cliente**: non esiste ancora un referente email dei clienti; la PEC è l'unico recapito certo.
49. **Annuncio nella piattaforma registrato ma non ancora letto da nessuno**: l'endpoint per i moduli/core che lo mostrano agli utenti arriverà con il core (porta pronta nei dati: `visibleUntil`).
50. **Invio sincrono** fino a 1000 clienti; oltre serve una coda (shortcut implicito nel limite).
51. **Campanella senza stato "letto"**: le voci spariscono quando il problema è risolto; il numero in Panoramica (mock 1b) è lo stesso conteggio.
52. **SMTP con `System.Net.Mail`** e cartella di pickup per lo sviluppo (nessuna nuova dipendenza); lo stato dei servizi mostra solo se è configurato, senza collegarsi.
53. **Conferma dell'invio con `window.confirm`**: un dialog del design system arriverà con il consolidamento (fase 12).
54. **La Panoramica sostituisce la Home**: le informazioni di UC8 restano nel menu dell'avatar.
55. **"Moduli installati"** conta i moduli del catalogo con quanti in beta: non c'è ancora un "aggiornamento disponibile" per cliente (le versioni installate per cliente non sono tracciate).
56. **Azioni della coda**: "Converti" apre l'abbonamento del cliente, "Proponi upgrade" apre una comunicazione al cliente; "Riprova" e "Reinvia inviti" agiscono subito.
57. **Scorciatoie**: ⌘K/Ctrl+K, N (nuovo cliente), Esc; J/K per scorrere le righe non implementate (il mock non le mostra).
58. **Impersonificazione solo dalla scheda cliente** (mock 1d): il pannello utente non la ospita per le regole FSD (una feature non importa un'altra).
59. **Token exchange "legacy" di Keycloak** (`requested_subject`): richiede la feature `token-exchange` attiva nel realm; la piattaforma deve leggere il token dal frammento `/impersonate#access_token=…`.
60. **Collection Postman come esecuzione unica**: crea il proprio cliente, piano, utente, vista e comunicazione con nomi unici (`runId`), cattura gli id dalle risposte, ottiene il token con client credentials se `token` è vuoto, e verifica lo stato atteso di ogni richiesta (anche i 409 voluti). Comando: `npx newman run docs/postman/PSAAS.AdminPortal.postman_collection.json --env-var baseUrl=… --env-var keycloakUrl=…/realms/PSAAS --env-var clientSecret=…`.
61. **Conferma dell'invio delle comunicazioni** con un secondo passo nel modulo (barra scura come quella delle modifiche non salvate), al posto di `window.confirm`.
62. **Suggerimenti steiger "insignificant slice" lasciati**: entità e feature usate da una sola pagina (comuni, utente corrente, viste salvate, pannelli) restano slice a sé per tenere separati dati, azioni e pagine; si possono accorpare se il designer o il team preferiscono meno cartelle.
63. **Giro visivo sui mock (27/10, ambiente locale con OIDC di sviluppo)**: confronto schermata per schermata con 1b–1f e controllo delle pagine senza mock, tema scuro e mobile. Corretti: pulsanti secondari bianchi con bordo come nei mock (regola nel layer `utilities`, perché daisyUI sta in un layer annidato lì dentro); righe dell'elenco clienti su una riga e checkbox da 16 px; contatore caratteri allineato a destra; etichette delle card numeriche di Fatturazione e Uso come in 1b; badge "Nessun profilo" col bordo chiaro; badge grigio leggibile nel tema scuro; etichette `sdiCode` e `accountManager` nell'audit. **Bug**: "Annulla" nella conferma d'invio delle comunicazioni la riapriva (React riusava il pulsante cliccato come pulsante di invio durante lo stesso clic); risolto con `key` distinte, verificato con Playwright (jsdom non riproduce il caso).

## Da far vedere al designer

Mock coperti: 1a (design system), 1b (Panoramica), 1c (Clienti), 1d (Scheda cliente), 1e (Onboarding), 1f (Utenti).

Disegnati con il sistema 1a, senza mock, da validare:
- **Pagine**: Moduli, Piani, Sottoscrizioni, Fatturazione (indicatori e pannello fattura), Comunicazioni (composizione e dettaglio), Audit log, Profili, Impostazioni (con i servizi collegati), 404.
- **Schede del cliente** oltre a Organizzazione: Anagrafica, Abbonamento e moduli, Utenti, Uso (grafici a barre giornalieri), Fatture, Storico.
- **Dialog e pannelli**: Importa CSV, Cambia piano multiplo, Impersona, palette ⌘K, campanella "Da gestire", viste salvate accanto alle schede.
- **Tema scuro** (derivato dai token del chiaro) e **vista mobile** (sidebar a scomparsa, tabelle con scorrimento orizzontale).

## Prossimo passo: verifica E2E finale

Su richiesta, dopo la configurazione di Keycloak (checklist nel piano: redirect URI, service account con `view-users`, `manage-users`, `view-events`, `impersonation`, attributi del *User profile*, SMTP, eventi di login, token exchange, ruolo `UsageReporter`): avvio con Aspire, login in Edge, script Playwright su tutte le schermate, screenshot e tabella esiti nella sezione 4 di `ADMIN-PORTAL.md`, confronto con i mock 1b–1f.
