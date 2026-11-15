# MySync

Applicazione FinHorizon per le finanze personali (fase 1: solo privati). Stessa struttura dell'Admin Portal: clean architecture propria, database proprio (`mysync-db`), backend ASP.NET Core (.NET 10), frontend React + Vite.

## Requisiti e use case

MVP (conti, debiti, movimenti, categorie, dashboard e andamento del patrimonio): [use-cases/README.md](use-cases/README.md), contratto HTTP in [use-cases/API.md](use-cases/API.md).

## Stato

Tutti gli use case dell'MVP (UC1–UC8) sono implementati, backend e frontend, con login Keycloak.

| Suite | Cosa verifica | Test |
|---|---|---|
| `FinHorizon.MySync.Domain.Tests` | Regole del dominio: importi, conti e debiti, regole dei movimenti, calcolo del patrimonio e campionamento dei periodi | 45 |
| `FinHorizon.MySync.Application.Tests` | Casi d'uso su archivi in memoria: validazione, unicità, limiti, isolamento, composizione dei decoratori | 34 |
| `FinHorizon.MySync.Infrastructure.Tests` | Configurazione: limiti, fuso orario, stringa di connessione | 7 |
| `FinHorizon.MySync.Backend.Tests` | Scenari Gherkin di UC2–UC8 e GEN sul backend reale e su **un unico Postgres (Testcontainers) per tutta la suite**, con token firmati localmente e un orologio controllabile | 104 |
| Frontend (Vitest) | UC1, UC6-S5, UC8-S8 sull'app reale con API finta; formattazione, importi, tema | 26 |

Per i test di integrazione serve Docker.

## Architettura del backend

```
src/MySync/
├─ FinHorizon.MySync.Domain/          entità (Account, Debt, Category, Transaction), valori (Money, OwnerId, Descriptor),
│                                     regole dei movimenti (una classe per regola, composte da TransactionPolicy),
│                                     calcolo del patrimonio (NetWorthCalculator, periodi con la loro strategia di campionamento)
├─ FinHorizon.MySync.Application/     un handler per caso d'uso (Features/<area>), validatori FluentValidation,
│                                     decoratori di validazione e log, porte verso persistenza e letture, limiti
├─ FinHorizon.MySync.Infrastructure/  EF Core (una configurazione per entità, migrazioni), repository, letture in SQL,
│                                     calendario con fuso orario, binding e validazione della configurazione
├─ FinHorizon.MySync.Backend/         controller sottili, utente corrente dal token, mappatura Result → HTTP
└─ Tests/                             un progetto per strato
```

Scelte che rendono il codice estensibile per le prossime fasi:

- **Un caso d'uso = una classe** (`IRequestHandler<TRequest, TResponse>`). Handler, validatori e regole si registrano da soli per convenzione (Scrutor): un caso d'uso nuovo è una classe nuova, niente da registrare a mano.
- **Decoratori** per validazione e log, applicati a tutti gli handler: gli handler contengono solo il caso d'uso.
- **Regole dei movimenti** come classi `ITransactionRule` indipendenti, composte da `TransactionPolicy`: una fase può aggiungerne o toglierne una senza toccare gli altri casi d'uso.
- **Esiti come valori** (`Result<T>`: Ok, Created, NotFound, Invalid), mappati a HTTP in un solo punto.
- **Limiti dietro una porta** (`IResourceLimits`): oggi da configurazione, domani dal piano dell'utente cambiando una sola implementazione.
- **Saldi e residui sempre calcolati** da Postgres sui movimenti, mai salvati: non possono divergere.
- **Periodi dell'andamento** come oggetti con la propria strategia di campionamento: un periodo nuovo è un'istanza nuova.
- Standard di codice imposti in build: analizzatori .NET al livello consigliato, warning come errori (`Directory.Build.props`, `.editorconfig`).

## Frontend

Feature-Sliced Design (controllato da Steiger): `app` (shell, rotte), `pages` (setup, dashboard, movimenti, andamento), `widgets` (grafico, tabella movimenti, schede conti e debiti), `features` (creazione di conti, debiti e movimenti, eliminazione, preferenze), `entities` (tipi e query per area, selettore di categoria), `shared` (API, login, formattazione, tema, UI di base).

- **Internazionalizzazione**: i18next + react-i18next, traduzioni in `public/locales/<lingua>/common.json` caricate con i18next-http-backend, lingua rilevata e ricordata da i18next-browser-languagedetector; importi, date e percentuali con i formattatori Intl integrati in i18next (definiti nei file di traduzione, chiavi `format.*`); plurali CLDR (`_one`, `_other`, `_zero`).
- **Icone**: solo Font Awesome (`@fortawesome/react-fontawesome`).
- **Tema chiaro e scuro**: interruttore nella barra in alto, ricordato nel browser; senza scelta segue il sistema.
- **File di tema**: `src/app/styles/theme.css` è l'unico file da modificare per cambiare palette e colori (in entrambi i temi), forma e font; i componenti usano solo i token semantici di daisyUI e le variabili `--chart-*`.
- Grafica ispirata al template "Enfix": accento verde su fondo menta, schede arrotondate, sidebar a icone (barra in basso da telefono). Grafici con Recharts.

## Avvio

Con Aspire (`src/FinHorizon.AppHost`): Postgres crea `mysync-db`, poi partono `mysync-backend` e `mysync-frontend` su `http://localhost:5174` (porta fissa per Keycloak). In Development il backend applica le migrazioni all'avvio.

## Keycloak (realm `PSAAS`)

| Client | Tipo | Configurazione |
|---|---|---|
| `mysync-frontend` | Pubblico (Client authentication off), Standard flow, PKCE `S256` | Valid redirect URIs `http://localhost:5174/*`; Web origins `http://localhost:5174`; Valid post logout redirect URIs `http://localhost:5174/*`; mapper *Audience* che aggiunge `mysync-api` all'access token |
| `mysync-api` | Confidenziale, nessun flusso abilitato | Serve solo come audience del backend (`Keycloak:ValidAudiences`) |

### Chi accede a cosa

| Gruppo | Ruolo di realm | Applicazione |
|---|---|---|
| `Service Administrators` | `systemadministrator` | Admin Portal (gli altri ricevono 403) |
| `end_users` | `enduser` | MySync (gli altri ricevono 403) |

Gli utenti creati dall'Admin Portal entrano da soli in `end_users` (UC6-R7). I ruoli si assegnano ai gruppi, non ai *Service account roles* dei client: quelli valgono solo per l'identità tecnica del client.
