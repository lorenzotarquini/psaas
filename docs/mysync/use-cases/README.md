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
| Utente | Una persona fisica con un account FinHorizon creato dall'Admin Portal (UC6 dell'Admin Portal). Entra solo con il modulo MySync, cioè il ruolo di realm Keycloak `module-mysync` assegnato dall'Admin Portal (UC32). Vede e modifica **solo i propri dati**. |
| Keycloak | Sistema esterno di identità: autentica l'utente. MySync identifica l'utente con il `sub` del token. |
| Stripe | Sistema esterno di pagamento: incassa l'abbonamento a MySync, rinnova, riprova i pagamenti falliti e manda fatture ed email (UC17). |

## Glossario

| Termine | Significato |
|---|---|
| Conto | Un contenitore di valore dell'utente: ha un tipo, un nome, una descrizione, un tag e un saldo iniziale. Il suo saldo cambia solo con i movimenti. |
| Tipo di conto | Conto corrente, Banca virtuale, Conto deposito, Investimenti, PAC (piano di accumulo), Moneta virtuale, Contanti, Preziosi, Altro. |
| Valuta | Ogni conto e debito ha la sua: una valuta tradizionale (EUR, USD, …) o, per la moneta virtuale, una criptovaluta (BTC, ETH, …) (UC13). |
| Valuta di visualizzazione | La valuta in cui l'utente vede patrimonio, debiti residui e andamento; scelta alla configurazione e nelle Impostazioni (UC13). |
| Saldo iniziale | Il saldo del conto al giorno in cui l'utente lo crea (la **data di apertura** in MySync). |
| Saldo | Saldo iniziale + ricavi − spese del conto, fino a una data. |
| Debito | Una somma che l'utente deve restituire: ha un tipo, un nome, una descrizione, un tag, il conto da cui si paga e il residuo da pagare alla creazione. |
| Tipo di debito | Finanziamento, Mutuo, Fido, Altro. |
| Residuo | Quanto resta da pagare di un debito: residuo iniziale − rate pagate. Un debito con residuo 0 è **estinto**. |
| Movimento | Una spesa (soldi che escono da un conto), un ricavo (soldi che entrano) o un trasferimento tra due conti dell'utente, con data e importo. Spese e ricavi hanno una categoria e, per le spese, l'eventuale debito di cui sono una rata. |
| Trasferimento | Soldi spostati da un conto dell'utente a un altro (UC9): né spesa né ricavo, il patrimonio netto non cambia. |
| Categoria | Etichetta dell'utente per raggruppare i movimenti (es. "Spesa alimentare", "Stipendio"). Le categorie di spesa e quelle di ricavo sono separate. |
| Patrimonio netto | Somma dei saldi di tutti i conti, a una certa data. I debiti non vengono sottratti: si mostrano accanto come debiti residui (D9). |
| Patrimonio stabile e debiti a lungo termine | Conti di tipo Preziosi (beni che restano lì e cambiano poco, come i gioielli) e debiti di tipo Mutuo; nascosti finché il loro switch è spento (UC12). |
| Investimenti e depositi | Conti di tipo Conto deposito, Investimenti, PAC e Moneta virtuale; nascosti finché il loro switch è spento (UC12). |
| Debiti residui | Somma dei residui di tutti i debiti, a una certa data. |
| Rata mensile | Quanto l'utente paga ogni mese per un debito; facoltativa, serve alla proiezione (UC3-R8). |
| Spesa fissa / variabile | Una categoria di spesa è fissa (affitto, bollette) o variabile (tutto il resto, di default) (UC15). |
| Abbonamento | Una spesa dello stesso importo ogni mese, da un conto e in una categoria, finché l'utente non la annulla (streaming, palestra, telefono). Non registra movimenti da solo: ogni addebito si registra come spesa collegata all'abbonamento (UC16). |
| Addebito | Il giorno del mese in cui un abbonamento si paga. Un addebito scaduto senza spesa collegata è **da registrare** (UC16). |
| Proiezione | Stima mese per mese del patrimonio futuro, partendo da oggi e da parametri proposti dalle medie degli ultimi mesi (UC14). |

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
| RF12 | L'utente trasferisce denaro tra due suoi conti; il trasferimento compare tra i movimenti ma non è né una spesa né un ricavo. | UC9 |
| RF13 | L'utente modifica conti e debiti da una sezione dedicata; modificare un conto chiede conferma perché il patrimonio viene ricalcolato. I pulsanti di aggiunta sono disabilitati oltre i limiti. | UC10 |
| RF14 | L'utente vede e modifica i propri dati e reimposta la password dalle Impostazioni, tramite le pagine di Keycloak. | UC11 |
| RF15 | Dashboard e Andamento hanno due switch, spenti di default: "Patrimonio stabile e debiti a lungo termine" e "Investimenti e depositi"; la dashboard ha anche un occhio per nascondere gli importi. | UC12 |
| RF16 | Ogni conto e debito ha una valuta; il tipo di conto "Moneta virtuale" usa le criptovalute. Il catalogo delle valute viene da un servizio esterno. | UC13 |
| RF17 | Patrimonio, debiti residui e andamento si vedono nella valuta di visualizzazione dell'utente, convertendo con il tasso di cambio di ogni giorno. | UC13 |
| RF18 | L'utente sceglie la valuta di visualizzazione alla configurazione e la cambia nelle Impostazioni. | UC1, UC11, UC13 |
| RF19 | L'utente proietta il patrimonio da 1 a 360 mesi, con parametri proposti dai suoi movimenti che può cambiare, eventi una tantum, obiettivo e inflazione; la proiezione si aggiorna mentre cambia i parametri e si può salvare. | UC14 |
| RF20 | L'utente indica quali categorie di spesa sono fisse. | UC15 |
| RF21 | Un debito può avere una rata mensile. | UC3, UC10 |
| RF22 | La dashboard mostra il patrimonio previsto tra 12 mesi. | UC4, UC14 |
| RF23 | L'utente crea, modifica, annulla, riattiva ed elimina i propri abbonamenti mensili. Il numero massimo di abbonamenti non annullati è dato dalla configurazione `MySync:MaxSubscriptions` (variabile d'ambiente `MySync__MaxSubscriptions`). | UC16 |
| RF24 | La dashboard mostra gli abbonamenti da registrare; un clic apre la spesa già compilata. Una spesa può essere collegata a un abbonamento. | UC4, UC16 |
| RF26 | L'utente paga MySync da solo su Stripe (4,99 € al mese), vede nelle Impostazioni stato, rinnovo, metodo di pagamento ed errore dell'ultimo pagamento, e rinnova; con l'abbonamento scaduto MySync è in sola lettura. | UC17 |
| RF25 | La proiezione considera gli abbonamenti uno per uno, con la loro data di fine, e permette di toglierne uno per vedere cosa cambia. | UC14, UC16 |

### Non funzionali

| Id | Requisito |
|---|---|
| RNF1 | Ogni operazione richiede un utente autenticato su Keycloak con ruolo `module-mysync` (policy `User`); senza, il frontend mostra "Non hai accesso a MySync". Con l'abbonamento a MySync scaduto le modifiche rispondono `402` (UC17-R4). |
| RNF2 | Isolamento: ogni dato appartiene a un utente (`sub` del token) e nessuna operazione legge o modifica dati di un altro utente. Un id di un altro utente si comporta come un id inesistente (404, mai 403, per non rivelarne l'esistenza). |
| RNF3 | Gli importi sono nella valuta del loro conto, con 2 decimali per le valute tradizionali e 8 per le criptovalute, e si salvano come `decimal` (mai `float`/`double`). |
| RNF4 | Le date dei movimenti sono date di calendario (senza ora né fuso). |
| RNF5 | Stessi codici d'errore per campo dell'Admin Portal (`Required`, `InvalidFormat`, `MaxLengthExceeded`, `NotUnique`, `InvalidValue`) più `LimitReached` per i limiti di RF2, RF4 e RF23; il frontend li traduce. |
| RNF6 | Interfaccia in italiano e inglese, tema chiaro e scuro, utilizzabile da telefono (la registrazione di una spesa si fa spesso in mobilità). |
| RNF7 | Log NLog strutturati in JSON in ogni strato, senza importi né descrizioni nei messaggi (sono dati personali); l'utente compare solo come id pseudonimo (`UserId`, il `sub` di Keycloak). I log sono raccolti da Alloy in Loki e consultati in Grafana ([osservabilita.md](../../osservabilita.md)). |

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
| [UC9](UC9-trasferire-denaro.md) | Trasferire denaro tra due conti | in ambito |
| [UC10](UC10-modificare-conti-e-debiti.md) | Modificare conti e debiti | in ambito |
| [UC11](UC11-gestire-il-proprio-profilo.md) | Gestire il proprio profilo (dati, password, preferenze) | in ambito |
| [UC12](UC12-vista-leggera-e-importi-nascosti.md) | Vista leggera e importi nascosti | in ambito |
| [UC13](UC13-valute-e-moneta-virtuale.md) | Valute e moneta virtuale | in ambito |
| [UC14](UC14-proiettare-il-patrimonio.md) | Proiettare il patrimonio | in ambito |
| [UC15](UC15-spese-fisse-e-variabili.md) | Indicare le spese fisse (di supporto a UC14) | in ambito |
| [UC16](UC16-gestire-abbonamenti.md) | Gestire gli abbonamenti | in ambito |
| [UC17](UC17-abbonamento-mysync.md) | Pagare l'abbonamento a MySync (Stripe) | in ambito |

Contratto HTTP: [API.md](API.md).

## Fuori ambito per l'MVP

- Finanze aziendali: conti, debiti e movimenti di un'organizzazione B2B (fase 2, D8).
- Eliminazione di conti e debiti; modifica ed eliminazione delle categorie, tranne il tipo fissa/variabile (UC15). Conti e debiti si modificano (UC10), i movimenti hanno il CRUD completo (D1).
- Rivalutazione dei preziosi; prezzi di azioni e fondi dei conti Investimenti e PAC.
- Movimenti ricorrenti registrati in automatico, anche per gli abbonamenti (fase 2 di UC16, D19); abbonamenti non mensili. Piani di ammortamento, separazione tra quota capitale e interessi di una rata.
- Import da estratto conto o collegamento alle banche (open banking).
- Budget, notifiche. L'obiettivo esiste solo nella proiezione (UC14), senza avvisi.
- Più scenari di proiezione e confronto tra proiezione e realtà (fase 2 di UC14).
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
| D8 | MySync è per le finanze personali o anche per quelle di un'azienda? | Fase 1 (MVP) solo privati: i dati sono personali e legati all'utente (`sub`), non a un'organizzazione. Il conto aziendale arriva nella fase 2. Nella fase 1 l'accesso è regolato dal modulo MySync assegnato al singolo utente (ruolo `module-mysync`, UC32 dell'Admin Portal), senza controllare il tipo di organizzazione dei profili. |
| D9 | Il patrimonio netto sottrae i debiti? | No: è la somma dei saldi dei conti; i debiti residui si mostrano accanto, in una card e una linea del grafico a parte. Decisa il 2026-11-15 dopo la prova della dashboard. |

| D10 | Da dove vengono il catalogo delle valute e i tassi di cambio? | Da [Frankfurter](https://frankfurter.dev) v2: gratuito e senza chiave, 160+ valute tradizionali e 35 criptovalute, tassi giornalieri con storico. Alternative valutate: CoinGecko (17.000+ criptovalute, ma chiave obbligatoria e solo 1 anno di storico gratis). Il provider sta dietro alle interfacce `ICurrencyCatalog` e `IExchangeRates`: cambiarlo è una classe in più. Decisa il 2026-11-15. |
| D11 | Dove si sceglie la valuta di visualizzazione? | Alla configurazione (UC1) e nelle Impostazioni (UC11); si salva in MySync, l'euro finché non è scelta. |
| D12 | I debiti hanno una valuta? | Sì, tradizionale; la rata esce da un conto nella stessa valuta. |
| D13 | Trasferimento tra valute diverse? | L'utente indica l'importo che esce e quello che entra; il cambio applicato è il suo. |
| D14 | Si può cambiare la valuta di un conto? | Solo finché non ha movimenti (di un debito: finché non ha rate), perché gli importi già registrati sono in quella valuta. |
| D15 | La proiezione rispetta gli switch di UC12? | No: proietta sempre tutti i conti e i debiti, perché una proiezione senza investimenti o preziosi sarebbe fuorviante. Decisa il 2026-11-17. |
| D16 | Dove si calcola la proiezione? | Nel browser: il backend dà punto di partenza, parametri proposti e parametri salvati; così ogni modifica aggiorna il grafico subito. Il calcolo sta in un solo modulo del frontend con i suoi test. Decisa il 2026-11-17. |
| D17 | Come si separano spese fisse e variabili? | Con il tipo della categoria di spesa (UC15), variabile di default; le rate dei debiti e i pagamenti degli abbonamenti non sono né l'una né l'altra, perché la proiezione li gestisce debito per debito e abbonamento per abbonamento (UC15-R5). Decisa il 2026-11-17. |
| D18 | Mettere soldi da parte cambia il patrimonio? | No, è un trasferimento (UC9). Nella proiezione la quota messa da parte passa dalla liquidità agli investimenti, e conta solo per il rendimento. Decisa il 2026-11-17. |
| D19 | Gli abbonamenti registrano da soli i loro pagamenti? | No: MySync segnala l'addebito da registrare e un clic apre la spesa già compilata, che l'utente conferma. Il saldo cambia solo con ciò che l'utente salva, non servono job in background e un movimento eliminato non si ricrea da solo. La registrazione automatica è nella fase 2 di UC16. Decisa il 2026-11-17. |
| D20 | Nella proiezione un abbonamento conta per la media delle sue spese o per il suo importo? | Per il suo importo di oggi, con primo addebito e data di fine: un abbonamento nuovo conta subito per intero e uno annullato esce dai mesi dopo la data di fine, senza aspettare che la media dei 6 mesi se ne accorga. Decisa il 2026-11-17. |

| D21 | Chi incassa MySync e dove si decide la sola lettura? | Il backend di MySync, direttamente con Stripe: cliente Stripe = utente Keycloak del gruppo `end_users`, copia dello stato in `mysync-db` aggiornata dai webhook. Così il controllo su ogni modifica legge il database di MySync e non serve un client Keycloak fra MySync e Admin Portal. L'Admin Portal resta il punto di Stripe per le organizzazioni ([STRIPE.md](../../admin-portal/STRIPE.md)). Decisa il 2026-11-17. |
| D22 | Cosa succede a chi usava MySync prima di Stripe? | Ha l'abbonamento scaduto: entra, vede i suoi dati e rinnova dalle Impostazioni. Nessuna migrazione. Decisa il 2026-11-17. |

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
    | GET /api/projection        |
    | PUT /api/projection        |
    | DELETE /api/projection     |
    | PUT /api/categories/{id}/fixed |
    | GET /api/subscriptions     |
    | POST /api/subscriptions    |
    | PUT /api/subscriptions/{id} |
    | POST /api/subscriptions/{id}/cancel |
    | POST /api/subscriptions/{id}/resume |
    | POST /api/subscriptions/{id}/skip |
    | DELETE /api/subscriptions/{id} |
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

Test: `GEN_Sicurezza.S3_OgniOperazioneRichiedeIlModuloMySync`

```gherkin
# language: it
Funzionalità: Accesso a MySync

Schema dello scenario: Ogni operazione richiede il modulo MySync
  Quando un utente della piattaforma (ruolo "enduser") senza ruolo "module-mysync" chiama "<operazione>"
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
