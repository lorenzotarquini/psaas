# MySync – Contratto HTTP (MVP)

Tutte le rotte sono sotto `/api`, richiedono un token Keycloak con audience `mysync-api` e ruolo di realm `module-mysync`, dato dall'Admin Portal con il modulo (GEN-S1, GEN-S3) e operano solo sui dati dell'utente del token (`sub`). Gli importi sono numeri decimali nella valuta del loro conto o debito, con al massimo 2 decimali (8 per le criptovalute); totali e controvalori sono nella valuta di visualizzazione dell'utente (campo `currency`, UC13). Le date sono `yyyy-MM-dd`. `GET /health` è pubblico.

**Errori.** `400` con `ValidationProblemDetails`: in `errors` un codice per campo, gli stessi dell'Admin Portal (`Required`, `InvalidFormat`, `MaxLengthExceeded`, `NotUnique`, `InvalidValue`) più `LimitReached`, che arriva sul campo `limit` perché non riguarda un singolo input. Un id nell'URL inesistente o di un altro utente risponde `404`; un id nel corpo (conto, categoria, debito) di un altro utente è `InvalidValue` su quel campo.

**Enumerazioni** per nome, senza distinzione di maiuscole (in risposta in PascalCase): conto `CurrentAccount`, `VirtualBank`, `DepositAccount`, `Investments`, `SavingsPlan` (PAC), `VirtualCurrency` (moneta virtuale), `Cash`, `Valuables`, `Other`; valuta `Fiat`, `Crypto`; debito `Loan`, `Mortgage`, `Overdraft`, `Other`; movimento `Expense`, `Income`, `Transfer`; categoria `Expense`, `Income`; periodo `30d`, `90d`, `12m`, `all`.

| Metodo e rotta | Use case | Richiesta | Risposta |
|---|---|---|---|
| `GET /api/me` | — | — | `{ keycloakId, name, email, roles }` dal token |
| `GET /api/accounts` | UC1, UC4 | — | `{ items: [{ id, kind, name, description, tag, initialBalance, openedOn, balance, group, currency, displayBalance }], max }`; `displayBalance` è il saldo nella valuta di visualizzazione |
| `POST /api/accounts` | UC2, UC13 | `{ kind, name, description?, tag?, initialBalance, currency? }`: `currency` dal catalogo del tipo giusto (`InvalidValue`), obbligatoria per `VirtualCurrency`, altrimenti la valuta di visualizzazione | `201` con il conto (`displayBalance` `null` se serve un cambio) |
| `PUT /api/accounts/{id}` | UC10 | come `POST`; la data di apertura non cambia; la valuta non cambia se il conto ha movimenti (`InvalidValue` su `currency`) | `200` con il conto e il saldo ricalcolato |
| `GET /api/debts` | UC1, UC4 | — | `{ items: [{ id, kind, name, description, tag, accountId, initialResidual, residual, createdOn, paidOff, group, currency, displayResidual }], max }`; attivi per residuo convertito decrescente, estinti in fondo |
| `POST /api/debts` | UC3 | `{ kind, name, description?, tag?, accountId, residual, currency? }`: valuta tradizionale | `201` con il debito |
| `PUT /api/debts/{id}` | UC10 | come `POST`; `residual` è il residuo iniziale e non può scendere sotto le rate già pagate (`InvalidValue`); la valuta non cambia se ci sono rate | `200` con il debito |
| `GET /api/currencies` | UC13 | — | `[{ code, name, symbol, type }]`, prima le valute tradizionali; `503` se il servizio delle valute non risponde |
| `GET /api/preferences` | UC1, UC11, UC13 | — | `{ displayCurrency, chosen }`; `EUR` e `chosen: false` finché l'utente non sceglie |
| `PUT /api/preferences` | UC1, UC11, UC13 | `{ displayCurrency }`: valuta tradizionale del catalogo (`InvalidValue`) | `200` con le preferenze |
| `GET /api/tags` | UC2, UC3 | — | Tag già usati su conti e debiti, distinti e ordinati |
| `GET /api/categories?kind=&term=` | UC6 | — | Categorie dell'utente (di quel tipo, se indicato) il cui nome contiene `term`; al massimo 50 |
| `POST /api/categories` | UC6 | `{ kind, name }` | `201` con la categoria nuova, `200` con quella esistente (UC6-R4) |
| `GET /api/transactions?from=&to=&kind=&accountId=&categoryId=&debtId=&page=&pageSize=` | UC4, UC8 | — | `{ items: [{ id, kind, date, amount, accountId, accountName, counterAccountId, counterAccountName, categoryId, categoryName, debtId, debtName, note, recordedAt, currency, counterAmount, counterCurrency }], page, pageSize, totalCount, expenseTotal, incomeTotal, currency }`; dal più recente; `pageSize` 1–100 (25); totali su tutto il filtro (i trasferimenti non ci entrano), convertiti giorno per giorno nella valuta di visualizzazione; `accountId` trova un trasferimento anche come destinazione; `from` > `to` è `InvalidValue` su `period` |
| `GET /api/transactions/{id}` | UC8 | — | Il movimento |
| `POST /api/transactions` | UC5, UC9 | `{ kind, date, amount, accountId, categoryId?, counterAccountId?, counterAmount?, debtId?, note? }`: `categoryId` obbligatorio per spese e ricavi, `counterAccountId` (destinazione) per i trasferimenti, `counterAmount` (quanto arriva) solo tra conti di valute diverse (`Required` se manca, `InvalidValue` se le valute sono uguali); `amount` con i decimali della valuta del conto (`InvalidFormat`); una rata da un conto in un'altra valuta è `InvalidValue` su `accountId` | `201` con il movimento |
| `PUT /api/transactions/{id}` | UC8 | come `POST` | `200` con il movimento aggiornato |
| `DELETE /api/transactions/{id}` | UC8 | — | `204` |
| `GET /api/dashboard?includeStable=&includeInvestments=` | UC4, UC12 | — | `{ netWorth, debtsResidual, accounts, debts, recentTransactions (10), limits: { maxAccounts, maxDebts }, currency }`; totali nella valuta di visualizzazione `currency`, `503` se serve un cambio e il servizio non risponde (UC13-5a); `netWorth` è la somma dei saldi, i debiti non vengono sottratti (D9); con `includeStable=false` i totali escludono preziosi e mutui, con `includeInvestments=false` conti deposito, investimenti e PAC (predefiniti `true`); le liste restano complete e ogni voce ha il suo `group` (`Everyday`, `Stable`, `Investments`) |
| `GET /api/net-worth?period=&includeStable=&includeInvestments=` | UC7, UC12 | — | `{ period, points: [{ date, netWorth, debts }], change, changePercent, currency }`; ogni giorno convertito con il suo tasso (UC13-R3); 30 giorni se `period` manca; `changePercent` è `null` se il patrimonio iniziale è 0; `includeStable` e `includeInvestments` come per la dashboard |

## Configurazione del backend

Tutta la configurazione sta nell'AppHost di Aspire: parametri in `src/FinHorizon.AppHost/appsettings.json` (sezione `Parameters`), passati a `mysync-backend` come variabili d'ambiente in `AppHost.cs`. Gli `appsettings` del backend non la contengono; nel codice resta solo un valore predefinito.

| Parametro Aspire | Variabile d'ambiente | Valore | Note |
|---|---|---|---|
| `mysync-max-accounts` | `MySync__MaxAccounts` | 2 | 1–1000; un valore fuori intervallo blocca l'avvio (UC2-R6) |
| `mysync-max-debts` | `MySync__MaxDebts` | 1 | 0–1000; 0 disattiva i debiti (UC3-R5) |
| `mysync-time-zone` | `Calendar__TimeZone` | `Europe/Rome` | Decide cos'è "oggi" (RNF4) |
| `mysync-rates-url` | `Currencies__Url` | `https://api.frankfurter.dev/` | Catalogo delle valute e tassi di cambio (UC13, D10); obbligatorio |
