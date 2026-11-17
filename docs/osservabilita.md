# Osservabilità: log centralizzati

Tutti i backend di FinHorizon scrivono log strutturati con gli stessi campi. **Alloy** li raccoglie, **Loki** li conserva e **Grafana** li mostra in una dashboard per servizio. In locale lo stack parte con l'AppHost di Aspire insieme al resto, senza nessun passaggio manuale.

```
mysync-backend ─┐  JSON, un file per servizio            ┌─ Panoramica
                ├─ src/logs/*.log ─► Alloy ─► Loki ─► Grafana ─┼─ MySync – Backend
adminportal-backend ─┘                                   └─ Admin Portal – Backend
```

## Avvio e indirizzi

Con `dotnet run --launch-profile https` in `src/FinHorizon.AppHost` partono anche tre container (immagini con versione fissa):

| Risorsa Aspire | Immagine | Indirizzo | A cosa serve |
|---|---|---|---|
| `grafana` | `grafana/grafana:12.1.1` | http://localhost:3000 | Dashboard ed Explore. Accesso anonimo in sola lettura, senza login (solo in sviluppo) |
| `loki` | `grafana/loki:3.5.3` | porta assegnata da Aspire | Archivio dei log, conservati **30 giorni** (`retention_period`), sul volume `finhorizon-loki-data` |
| `alloy` | `grafana/alloy:v1.10.2` | porta assegnata da Aspire (UI di debug) | Legge `src/logs/*-backend.log`, estrae i campi e li invia a Loki. Ricorda fin dove ha letto (volume `finhorizon-alloy-data`) |

Le configurazioni sono in [`src/FinHorizon.AppHost/Observability`](../src/FinHorizon.AppHost/Observability): `loki/loki.yaml`, `alloy/config.alloy`, `grafana/provisioning` (datasource Loki e cartella delle dashboard) e `grafana/dashboards`.

## Formato dei log

| Dove | Formato | Esempio |
|---|---|---|
| Console in sviluppo (target `console`) | `<timestamp>\|<livello>\|<classe>\|<messaggio>`, da leggere a occhio | `2026-11-17 20:31:20.817\|INFO\|FinHorizon.Http\|HTTP GET /api/accounts responded 200 in 4 ms` |
| File in sviluppo (target `jsonFile`), letto da Alloy | Una riga JSON per evento, `src/logs/<servizio>.log`, ruotato a 10 MB (5 archivi) | sotto |
| stdout in produzione e nei test (target `stdout`) | La stessa riga JSON | sotto |

```json
{"timestamp":"2026-11-17T20:30:31.137+01:00","level":"WARN","service":"mysync-backend","logger":"FinHorizon.Http",
 "message":"HTTP GET /api/accounts responded 401 in 4 ms","messageTemplate":"HTTP {Method} {Route} responded {StatusCode} in {ElapsedMs} ms",
 "traceId":"00-a5c6…-00","Method":"GET","Route":"/api/accounts","StatusCode":401,"ElapsedMs":4,"Event":"http_request"}
```

Il JSON è il formato di riferimento: Loki lo interpreta con `| json` e le dashboard filtrano per campo. Il formato con le barre esiste solo per chi legge la console mentre sviluppa.

### Campi

| Campo | Dove c'è | Significato |
|---|---|---|
| `timestamp`, `level`, `service`, `logger`, `message`, `messageTemplate`, `exception`, `traceId` | Ogni riga | `service` è `mysync-backend` o `adminportal-backend`. `messageTemplate` raggruppa le righe dello stesso tipo |
| `UserId` | Ogni riga scritta durante una richiesta autenticata | Il `sub` di Keycloak: un id **pseudonimo**, mai nome, username o email |
| `Event` | Righe di richiesta e di caso d'uso | `http_request` o `use_case` |
| `Method`, `Route`, `StatusCode`, `ElapsedMs` | `Event = http_request` | Una riga per richiesta, scritta da `RequestLoggingMiddleware` ([FinHorizon.Observability](../src/Shared/FinHorizon.Observability)). `Route` è il template (`/api/subscriptions/{id:guid}`), mai l'URL reale |
| `UseCase`, `Status`, `ElapsedMs`, `Fields` | `Event = use_case` (MySync) | Esito di ogni caso d'uso (`LoggingDecorator`). `Fields` elenca i campi rifiutati (`name:Required,amount:InvalidValue`) |

### Dati personali

- Nessun log contiene username, email, importi, note, query string o percorsi reali. I target JSON escludono `RequestPath` e `ConnectionId` che ASP.NET Core mette nello scope della richiesta.
- Per seguire tutto quello che ha fatto un utente si usa il suo `UserId` (filtro "Utente (id)" nelle dashboard). Per risalire alla persona serve Keycloak, che non è nei log.
- Le email dell'Admin Portal registrano solo il canale (`smtp` o `pickup`), mai destinatario o oggetto.
- I test [`BackendLogConfigurationTests`](../src/Shared/Tests/FinHorizon.Observability.Tests/BackendLogConfigurationTests.cs) e [`RequestLoggingMiddlewareTests`](../src/Shared/Tests/FinHorizon.Observability.Tests/RequestLoggingMiddlewareTests.cs) fanno fallire la build se tornano username, URL o percorsi.

## Etichette in Loki

Le etichette (indicizzate) sono poche e con pochi valori: `service`, `level` ed `env`. `event` è *structured metadata*. Tutto il resto si legge dal JSON della riga:

```logql
sum by (Route) (count_over_time({service="mysync-backend"} | json | Event="http_request" | StatusCode=~"5.." [1h]))
{service="mysync-backend"} | json | UserId="6f1c2a7e-…"
```

## Dashboard

Si trovano nella cartella **FinHorizon** di Grafana. La panoramica è la home.

| Dashboard | Contenuto |
|---|---|
| **FinHorizon – Panoramica** | Richieste, errori 5xx, latenza p95 e volume di log per servizio, errori di tutti i servizi |
| **MySync – Backend** | Traffico HTTP (richieste, 4xx, 5xx, p95, utenti attivi, 401/403, rotte più chiamate e con più errori); casi d'uso per esito, durata, rifiuti e campi rifiutati; dipendenze (servizio valute non disponibile, abbonamenti creati e annullati, movimenti registrati, migrazioni); log con filtro per testo e per utente |
| **Admin Portal – Backend** | Lo stesso traffico HTTP; eventi per componente, operazioni sulle aziende, email inviate, Keycloak non raggiungibile, moduli non raggiungibili, conflitti di unicità; job di fatturazione e provisioning; log con filtri |

Le dashboard sono generate da [`generate_dashboards.py`](../src/FinHorizon.AppHost/Observability/grafana/generate_dashboards.py): si modifica lo script, si lancia `python3 generate_dashboards.py` nella sua cartella e Grafana ricarica i file in pochi secondi. Dalla UI non si possono modificare.

## Aggiungere un servizio

1. Il backend referenzia `FinHorizon.Observability` e chiama `app.UseRequestLogging()` tra `UseAuthentication()` e `UseAuthorization()`.
2. In `appsettings.json` copia i target `stdout`, `console` e `jsonFile` di un altro backend, con il proprio nome in `service` e nel nome del file. In `appsettings.Development.json` le regole scrivono su `console,jsonFile`.
3. Il file deve chiamarsi `<nome>-backend.log`, così Alloy lo raccoglie senza modifiche. Nell'AppHost si aggiunge `.WithEnvironment("Logs__Directory", logsDirectory)`.
4. Si aggiunge il servizio a `BackendLogConfigurationTests` e una dashboard in `generate_dashboards.py`.

## In produzione

I backend scrivono le stesse righe JSON su stdout. Alloy le raccoglie dai container: in `config.alloy` i componenti `local.file_match` e `loki.source.file` si sostituiscono con `discovery.docker` (o `discovery.kubernetes`) e `loki.source.docker`. Il resto della pipeline, Loki e le dashboard restano uguali. Servono ancora, e non sono in questo repository: Grafana con login (SSO Keycloak), Loki su object storage, allarmi e il deploy dei container.

## Non ancora coperto

- Log dei frontend: oggi restano nella console del browser. Il passo naturale è il receiver `faro.receiver` di Alloy.
- Tracce e metriche (OpenTelemetry, Tempo, Prometheus) e allarmi in Grafana.
- Audit log di MySync.
