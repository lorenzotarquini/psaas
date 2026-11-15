# MySync – Use case (MVP)

Requisiti raccolti il 2026-11-15. Ogni use case ha un file dedicato con flusso, regole e scenari di accettazione (Gherkin) identificati da `UCn-Sm`, ognuno collegato al suo test (convenzione dell'Admin Portal: classe `UCn_Nome`, metodo `Sm_Descrizione`). Gli scenari del backend sono test di integrazione xUnit sul backend reale e su un unico Postgres (Testcontainers) per tutta la suite; quelli dell'interfaccia (UC1, UC6-S5, UC8-S8) sono test Vitest + Testing Library.

Oltre agli scenari scritti qui, le suite coprono anche: UC3-S7 (limite 0 disattiva i debiti), UC4-S5 (debiti estinti in fondo), UC5-S10 (rata prima della creazione del debito, R7), UC5-S11 (nota salvata), UC6-S6 (creazioni concorrenti: una sola categoria), UC7-S5 (periodi lunghi che finiscono oggi), UC8-S9 (periodo al rovescio), UC8-S10 (paginazione), GEN-S6 (identità dal token). Le regole del dominio e dei casi d'uso hanno inoltre test unitari propri.

## Il prodotto

**MySync** è il portale dove una persona gestisce le proprie finanze. Mette insieme in un solo posto:

- i suoi **conti**: dove tiene i soldi o il valore (conto corrente, banca virtuale, contanti, preziosi, …);
- i suoi **debiti**: quanto deve ancora pagare (finanziamento, mutuo, fido, …), ognuno legato al conto da cui si paga;
- i suoi **movimenti**: le spese e i ricavi, con data, conto e categoria; una spesa può essere la rata di un debito.

Da questi dati MySync calcola il **patrimonio netto** e ne mostra l'andamento nel tempo.

**Fase 1 (questo MVP): solo privati.** MySync gestisce le finanze personali dell'utente; i dati sono suoi, non di un'organizzazione. Il conto aziendale arriverà nella fase 2 (D8).

### Flusso tipico

1. Primo accesso: la persona configura MySync creando almeno un conto e, se vuole, i suoi debiti (UC1, che usa UC2 e UC3).
2. Ogni giorno: apre la dashboard (UC4) e registra spese e ricavi (UC5), creando le categorie che le servono al momento (UC6).
3. Quando vuole: guarda come cambia il patrimonio (UC7).

## Attori

| Attore | Descrizione |
|---|---|
| Utente | Una persona fisica con un account FinHorizon creato dall'Admin Portal (UC6 dell'Admin Portal). Ruolo di realm Keycloak `enduser`, assegnato dal gruppo `end_users`. Vede e modifica **solo i propri dati**. |
| Keycloak | Sistema esterno di identità: autentica l'utente. MySync identifica l'utente con il `sub` del token. |

## Glossario

| Termine | Significato |
|---|---|
| Conto | Un contenitore di valore dell'utente: ha un tipo, un nome, una descrizione, un tag e un saldo iniziale. Il suo saldo cambia solo con i movimenti. |
| Tipo di conto | Conto corrente, Banca virtuale, Contanti, Preziosi, Altro. |
| Saldo iniziale | Il saldo del conto al giorno in cui l'utente lo crea (la **data di apertura** in MySync). |
| Saldo | Saldo iniziale + ricavi − spese del conto, fino a una data. |
| Debito | Una somma che l'utente deve restituire: ha un tipo, un nome, una descrizione, un tag, il conto da cui si paga e il residuo da pagare alla creazione. |
| Tipo di debito | Finanziamento, Mutuo, Fido, Altro. |
| Residuo | Quanto resta da pagare di un debito: residuo iniziale − rate pagate. Un debito con residuo 0 è **estinto**. |
| Movimento | Una spesa (soldi che escono da un conto) o un ricavo (soldi che entrano), con data, importo, conto, categoria e, per le spese, l'eventuale debito di cui è una rata. |
| Categoria | Etichetta dell'utente per raggruppare i movimenti (es. "Spesa alimentare", "Stipendio"). Le categorie di spesa e quelle di ricavo sono separate. |
| Patrimonio netto | Somma dei saldi di tutti i conti, a una certa data. I debiti non vengono sottratti: si mostrano accanto come debiti residui (D9). |
| Debiti residui | Somma dei residui di tutti i debiti, a una certa data. |

## Requisiti

### Funzionali

| Id | Requisito | Use case |
|---|---|---|
| RF1 | L'utente crea i propri conti con tipo, nome, descrizione, tag e saldo iniziale. | UC2 |
| RF2 | Il numero massimo di conti per utente è dato dalla configurazione `MySync:MaxAccounts` (variabile d'ambiente `MySync__MaxAccounts`). | UC2 |
| RF3 | L'utente crea i propri debiti con tipo, nome, descrizione, tag, conto collegato e residuo iniziale. | UC3 |
| RF4 | Il numero massimo di debiti per utente è dato dalla configurazione `MySync:MaxDebts` (variabile d'ambiente `MySync__MaxDebts`). | UC3 |
| RF5 | Al primo accesso, senza conti, l'utente è guidato a configurare MySync; la dashboard si apre solo quando esiste almeno un conto. | UC1 |
| RF6 | La dashboard mostra patrimonio netto, conti con saldo, debiti con residuo, ultimi movimenti e andamento del patrimonio. | UC4 |
| RF7 | L'utente registra una spesa o un ricavo con data, importo, conto, categoria e, per una spesa, l'eventuale debito. | UC5 |
| RF8 | Se la categoria non esiste, l'utente la crea dallo stesso campo senza lasciare il movimento. | UC6 |
| RF9 | Una spesa collegata a un debito ne riduce il residuo. | UC5 |
| RF10 | MySync mostra l'andamento del patrimonio netto e dei debiti residui su un periodo scelto. | UC7 |
| RF11 | L'utente consulta tutti i suoi movimenti con filtri, li modifica e li elimina; saldi, residui e andamento si ricalcolano di conseguenza. | UC8 |

### Non funzionali

| Id | Requisito |
|---|---|
| RNF1 | Ogni operazione richiede un utente autenticato su Keycloak con ruolo `enduser` (policy `User`). |
| RNF2 | Isolamento: ogni dato appartiene a un utente (`sub` del token) e nessuna operazione legge o modifica dati di un altro utente. Un id di un altro utente si comporta come un id inesistente (404, mai 403, per non rivelarne l'esistenza). |
| RNF3 | Gli importi sono in euro, con 2 decimali, e si salvano come `decimal` (mai `float`/`double`). |
| RNF4 | Le date dei movimenti sono date di calendario (senza ora né fuso). |
| RNF5 | Stessi codici d'errore per campo dell'Admin Portal (`Required`, `InvalidFormat`, `MaxLengthExceeded`, `NotUnique`, `InvalidValue`) più `LimitReached` per i limiti di RF2 e RF4; il frontend li traduce. |
| RNF6 | Interfaccia in italiano e inglese, tema chiaro e scuro, utilizzabile da telefono (la registrazione di una spesa si fa spesso in mobilità). |
| RNF7 | Log NLog strutturati in JSON in ogni strato, senza importi né descrizioni nei messaggi (sono dati personali). |

## Use case

| Id | Use case | Stato |
|---|---|---|
| [UC1](UC1-configurare-mysync.md) | Configurare MySync al primo accesso | in ambito |
| [UC2](UC2-creare-conto.md) | Creare un conto | in ambito |
| [UC3](UC3-creare-debito.md) | Creare un debito | in ambito |
| [UC4](UC4-consultare-dashboard.md) | Consultare la dashboard | in ambito |
| [UC5](UC5-registrare-movimento.md) | Registrare una spesa o un ricavo | in ambito |
| [UC6](UC6-creare-categoria.md) | Creare una categoria al volo (di supporto a UC5) | in ambito |
| [UC7](UC7-andamento-patrimonio.md) | Vedere l'andamento del patrimonio | in ambito |
| [UC8](UC8-gestire-movimenti.md) | Consultare, modificare ed eliminare i movimenti | in ambito |

Contratto HTTP: [API.md](API.md).

## Fuori ambito per l'MVP

- Finanze aziendali: conti, debiti e movimenti di un'organizzazione B2B (fase 2, D8).
- Modifica ed eliminazione di conti, debiti e categorie (i movimenti invece hanno il CRUD completo, D1).
- Giroconti tra due conti dell'utente.
- Valute diverse dall'euro, rivalutazione dei preziosi.
- Movimenti ricorrenti, piani di ammortamento, separazione tra quota capitale e interessi di una rata.
- Import da estratto conto o collegamento alle banche (open banking).
- Budget, obiettivi di risparmio, notifiche.
- Condivisione dei conti con altri utenti (famiglia).

## Decisioni

Prese il 2026-11-15.

| Id | Domanda | Decisione |
|---|---|---|
| D1 | L'MVP senza modifica ed eliminazione dei movimenti è usabile? | No: i movimenti hanno il CRUD completo (UC5 crea, UC8 consulta, modifica ed elimina). |
| D2 | Valori predefiniti di `MySync:MaxAccounts` e `MySync:MaxDebts`? | 2 conti e 1 debito. |
| D3 | Un movimento può avere una data precedente all'apertura del conto? | No: il saldo iniziale è già la fotografia di quel giorno. |
| D4 | Un movimento può avere una data futura? | No: MySync registra ciò che è successo. |
| D5 | Un ricavo può essere collegato a un debito (es. erogazione di un fido)? | No: solo le spese, come rate. |
| D6 | La rata di un debito deve uscire dal conto collegato al debito? | No: il conto collegato è solo proposto come predefinito, l'utente può cambiarlo. |
| D7 | Il saldo di un conto può essere negativo (scoperto)? | Sì per tutti i tipi; MySync non blocca la spesa, la dashboard evidenzia il saldo negativo. |
| D8 | MySync è per le finanze personali o anche per quelle di un'azienda? | Fase 1 (MVP) solo privati: i dati sono personali e legati all'utente (`sub`), non a un'organizzazione. Il conto aziendale arriva nella fase 2. Nella fase 1 l'accesso resta regolato solo dal ruolo `enduser`, senza controllare il tipo di organizzazione dei profili. |
| D9 | Il patrimonio netto sottrae i debiti? | No: è la somma dei saldi dei conti; i debiti residui si mostrano accanto, in una card e una linea del grafico a parte. Decisa il 2026-11-15 dopo la prova della dashboard. |

Non ci sono domande aperte.

## Scenari comuni

Test: [`GEN_Sicurezza`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/GEN_Sicurezza.cs).

### GEN-S1

Test: `GEN_Sicurezza.S1_SenzaAutenticazioneLAccessoENegato`

```gherkin
# language: it
Funzionalità: Accesso a MySync

Schema dello scenario: Ogni operazione richiede un utente autenticato
  Quando qualcuno chiama "<operazione>" senza essere autenticato
  Allora l'accesso è negato come non autenticato

  Esempi:
    | operazione                 |
    | GET /api/me                |
    | GET /api/accounts          |
    | POST /api/accounts         |
    | GET /api/debts             |
    | POST /api/debts            |
    | GET /api/tags              |
    | GET /api/categories        |
    | POST /api/categories       |
    | GET /api/transactions      |
    | POST /api/transactions     |
    | GET /api/transactions/{id} |
    | PUT /api/transactions/{id} |
    | DELETE /api/transactions/{id} |
    | GET /api/dashboard         |
    | GET /api/net-worth         |
```

### GEN-S2

Test: `GEN_Sicurezza.S2_UnTokenPerUnAltroClientNonVale`

```gherkin
# language: it
Funzionalità: Accesso a MySync

Scenario: Un token emesso per un'altra applicazione non vale
  Quando qualcuno chiama "GET /api/me" con un token senza audience "mysync-api"
  Allora l'accesso è negato come non autenticato
```

### GEN-S3

Test: `GEN_Sicurezza.S3_OgniOperazioneRichiedeIlRuoloEnduser`

```gherkin
# language: it
Funzionalità: Accesso a MySync

Schema dello scenario: Ogni operazione richiede il ruolo enduser
  Quando un utente autenticato senza ruolo "enduser" chiama "<operazione>"
  Allora l'accesso è negato come non autorizzato

  Esempi: le stesse operazioni di GEN-S1
```

### GEN-S4

Test: `GEN_Sicurezza.S4_LoStatoDelServizioEPubblico`

```gherkin
# language: it
Funzionalità: Accesso a MySync

Scenario: Lo stato del servizio è consultabile senza autenticazione
  Quando qualcuno chiama "GET /health" senza essere autenticato
  Allora il servizio risponde che è attivo
```

### GEN-S5

Test: `GEN_Sicurezza.S5_DatiDiUnAltroUtenteInvisibili`

```gherkin
# language: it
Funzionalità: Accesso a MySync

Scenario: I dati di un altro utente sono invisibili
  Dato che "Mario" ha il conto "Conto Intesa"
  Quando "Anna" elenca i suoi conti
  Allora l'elenco non contiene "Conto Intesa"
  Quando "Anna" registra una spesa sul conto "Conto Intesa" di "Mario"
  Allora la richiesta è rifiutata con "InvalidValue" sul conto
  E il saldo di "Conto Intesa" non è cambiato
```
