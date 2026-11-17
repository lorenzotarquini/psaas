# MySync – Contratto HTTP (MVP)

Tutte le rotte sono sotto `/api`, richiedono un token Keycloak con audience `mysync-api` e ruolo di realm `module-mysync`, dato dall'Admin Portal con il modulo (GEN-S1, GEN-S3) e operano solo sui dati dell'utente del token (`sub`). Gli importi sono numeri decimali nella valuta del loro conto o debito, con al massimo 2 decimali (8 per le criptovalute); totali e controvalori sono nella valuta di visualizzazione dell'utente (campo `currency`, UC13). Le date sono `yyyy-MM-dd`. `GET /health` è pubblico.

**Errori.** `400` con `ValidationProblemDetails`: in `errors` un codice per campo, gli stessi dell'Admin Portal (`Required`, `InvalidFormat`, `MaxLengthExceeded`, `NotUnique`, `InvalidValue`) più `LimitReached`, che arriva sul campo `limit` perché non riguarda un singolo input. Un id nell'URL inesistente o di un altro utente risponde `404`; un id nel corpo (conto, categoria, debito, abbonamento) di un altro utente è `InvalidValue` su quel campo.

**Enumerazioni** per nome, senza distinzione di maiuscole (in risposta in PascalCase): conto `CurrentAccount`, `VirtualBank`, `DepositAccount`, `Investments`, `SavingsPlan` (PAC), `VirtualCurrency` (moneta virtuale), `Cash`, `Valuables`, `Other`; valuta `Fiat`, `Crypto`; debito `Loan`, `Mortgage`, `Overdraft`, `Other`; movimento `Expense`, `Income`, `Transfer`; categoria `Expense`, `Income`; periodo `30d`, `90d`, `12m`, `all`.

| Metodo e rotta | Use case | Richiesta | Risposta |
|---|---|---|---|
| `GET /api/me` | — | — | `{ keycloakId, name, email, roles }` dal token |
| `GET /api/accounts` | UC1, UC4 | — | `{ items: [{ id, kind, name, description, tag, initialBalance, openedOn, balance, group, currency, displayBalance }], max }`; `displayBalance` è il saldo nella valuta di visualizzazione |
| `POST /api/accounts` | UC2, UC13 | `{ kind, name, description?, tag?, initialBalance, currency? }`: `currency` dal catalogo del tipo giusto (`InvalidValue`), obbligatoria per `VirtualCurrency`, altrimenti la valuta di visualizzazione | `201` con il conto (`displayBalance` `null` se serve un cambio) |
| `PUT /api/accounts/{id}` | UC10 | come `POST`; la data di apertura non cambia; la valuta non cambia se il conto ha movimenti (`InvalidValue` su `currency`) | `200` con il conto e il saldo ricalcolato |
| `GET /api/debts` | UC1, UC4 | — | `{ items: [{ id, kind, name, description, tag, accountId, initialResidual, residual, monthlyPayment, createdOn, paidOff, group, currency, displayResidual }], max }`; `monthlyPayment` è `null` se non indicata; attivi per residuo convertito decrescente, estinti in fondo |
| `POST /api/debts` | UC3 | `{ kind, name, description?, tag?, accountId, residual, monthlyPayment?, currency? }`: valuta tradizionale; `monthlyPayment` > 0 con 2 decimali (UC3-R8) | `201` con il debito |
| `PUT /api/debts/{id}` | UC10 | come `POST`; `residual` è il residuo iniziale e non può scendere sotto le rate già pagate (`InvalidValue`); la valuta non cambia se ci sono rate | `200` con il debito |
| `GET /api/currencies` | UC13 | — | `[{ code, name, symbol, type }]`, prima le valute tradizionali; `503` se il servizio delle valute non risponde |
| `GET /api/preferences` | UC1, UC11, UC13 | — | `{ displayCurrency, chosen }`; `EUR` e `chosen: false` finché l'utente non sceglie |
| `PUT /api/preferences` | UC1, UC11, UC13 | `{ displayCurrency }`: valuta tradizionale del catalogo (`InvalidValue`) | `200` con le preferenze |
| `GET /api/tags` | UC2, UC3 | — | Tag già usati su conti e debiti, distinti e ordinati |
| `GET /api/categories?kind=&term=` | UC6, UC15 | — | Categorie dell'utente (di quel tipo, se indicato) il cui nome contiene `term`; al massimo 50; ognuna `{ id, kind, name, fixed }` (`fixed` sempre `false` per i ricavi) |
| `PUT /api/categories/{id}/fixed` | UC15 | `{ fixed }`: solo per categorie di spesa (`InvalidValue` su `fixed`) | `200` con la categoria |
| `POST /api/categories` | UC6 | `{ kind, name }` | `201` con la categoria nuova, `200` con quella esistente (UC6-R4) |
| `GET /api/transactions?from=&to=&kind=&accountId=&categoryId=&debtId=&subscriptionId=&page=&pageSize=` | UC4, UC8 | — | `{ items: [{ id, kind, date, amount, accountId, accountName, counterAccountId, counterAccountName, categoryId, categoryName, debtId, debtName, subscriptionId, subscriptionName, note, recordedAt, currency, counterAmount, counterCurrency }], page, pageSize, totalCount, expenseTotal, incomeTotal, currency }`; dal più recente; `pageSize` 1–100 (25); totali su tutto il filtro (i trasferimenti non ci entrano), convertiti giorno per giorno nella valuta di visualizzazione; `accountId` trova un trasferimento anche come destinazione; `from` > `to` è `InvalidValue` su `period` |
| `GET /api/transactions/{id}` | UC8 | — | Il movimento |
| `POST /api/transactions` | UC5, UC9 | `{ kind, date, amount, accountId, categoryId?, counterAccountId?, counterAmount?, debtId?, subscriptionId?, note? }`: `categoryId` obbligatorio per spese e ricavi, `counterAccountId` (destinazione) per i trasferimenti, `counterAmount` (quanto arriva) solo tra conti di valute diverse (`Required` se manca, `InvalidValue` se le valute sono uguali); `amount` con i decimali della valuta del conto (`InvalidFormat`); una rata da un conto in un'altra valuta è `InvalidValue` su `accountId`; `subscriptionId` solo su una spesa senza `debtId`, da un conto nella valuta dell'abbonamento e con data tra primo addebito e data di fine (UC16-R10, R11) | `201` con il movimento |
| `PUT /api/transactions/{id}` | UC8 | come `POST` | `200` con il movimento aggiornato |
| `DELETE /api/transactions/{id}` | UC8 | — | `204` |
| `GET /api/dashboard?includeStable=&includeInvestments=` | UC4, UC12 | — | `{ netWorth, debtsResidual, accounts, debts, recentTransactions (10), limits: { maxAccounts, maxDebts }, currency }`; totali nella valuta di visualizzazione `currency`, `503` se serve un cambio e il servizio non risponde (UC13-5a); `netWorth` è la somma dei saldi, i debiti non vengono sottratti (D9); con `includeStable=false` i totali escludono preziosi e mutui, con `includeInvestments=false` conti deposito, investimenti e PAC (predefiniti `true`); le liste restano complete e ogni voce ha il suo `group` (`Everyday`, `Stable`, `Investments`) |
| `GET /api/net-worth?period=&includeStable=&includeInvestments=` | UC7, UC12 | — | `{ period, points: [{ date, netWorth, debts }], change, changePercent, currency }`; ogni giorno convertito con il suo tasso (UC13-R3); 30 giorni se `period` manca; `changePercent` è `null` se il patrimonio iniziale è 0; `includeStable` e `includeInvestments` come per la dashboard |

| `GET /api/projection` | UC4, UC14 | — | `{ today, start: { liquidity, investments, stable }, debts: [{ id, name, residual, monthlyPayment }], subscriptions: [{ id, name, amount, firstChargeOn, endsOn }], suggested: { income, fixedExpenses, variableExpenses, saving, annualReturn, inflation, payments: [{ debtId, amount }], subscriptions: [{ subscriptionId, amount, included }] }, monthsOfData, saved, currency }`; in `subscriptions` gli abbonamenti senza data di fine o con data di fine da oggi in poi, con l'importo convertito al tasso di oggi (UC14-R3); importi nella valuta di visualizzazione `currency`; in `debts` il residuo convertito e la rata indicata (`null` se manca); `today` è l'"oggi" del backend (il mese 1 è il mese dopo, UC14-R2); `saved` è `null` o come il corpo di `PUT`; `503` se serve un cambio e il servizio non risponde |
| `PUT /api/projection` | UC14 | `{ horizonMonths, income, fixedExpenses, variableExpenses, saving, annualReturn, inflation, inflationOn, payments: [{ debtId, amount }], subscriptions: [{ subscriptionId, amount, included }], events: [{ month, amount, note }], goal? }`; percentuali come numeri (2 = 2%); regole in UC14-R9; un `debtId` che non è dell'utente è `InvalidValue` su `payments[i].debtId`, un `subscriptionId` su `subscriptions[i].subscriptionId` | `200` con la proiezione salvata |
| `DELETE /api/projection` | UC14 | — | `204`; anche se non c'era niente di salvato |
| `GET /api/subscriptions` | UC4, UC16 | — | `{ items: [{ id, name, description, accountId, accountName, categoryId, categoryName, amount, currency, displayAmount, firstChargeOn, endsOn, createdOn, active, nextChargeOn, dueOn, hasPayments }], monthlyTotal, currency, max }`; `dueOn` è la data dell'addebito da registrare (UC16-R7) o `null`; `nextChargeOn` è `null` se non ci sono più addebiti; `monthlyTotal` e `displayAmount` nella valuta di visualizzazione `currency` (`503` se serve un cambio e il servizio non risponde); prima gli attivi per nome, poi gli annullati per data di fine decrescente |
| `POST /api/subscriptions` | UC16 | `{ name, description?, accountId, categoryId, amount, firstChargeOn }`: conto in valuta tradizionale e categoria di spesa dell'utente (`InvalidValue`), `amount` > 0 con 2 decimali; oltre `max` è `LimitReached` | `201` con l'abbonamento |
| `PUT /api/subscriptions/{id}` | UC16 | come `POST`; con spese collegate il conto resta nella stessa valuta (`InvalidValue` su `accountId`) e `firstChargeOn` non supera la prima spesa collegata (`InvalidValue`) | `200` con l'abbonamento |
| `POST /api/subscriptions/{id}/cancel` | UC16 | `{ endsOn }`: non prima del primo addebito né dell'ultima spesa collegata (`InvalidValue` su `endsOn`) | `200` con l'abbonamento |
| `POST /api/subscriptions/{id}/resume` | UC16 | — | `200` con l'abbonamento senza data di fine, senza addebiti da registrare del periodo annullato (UC16-R14); `LimitReached` oltre `max` |
| `POST /api/subscriptions/{id}/skip` | UC16 | `{ dueOn }`: deve essere l'addebito da registrare in quel momento (`InvalidValue` su `dueOn`) | `200` con l'abbonamento |
| `DELETE /api/subscriptions/{id}` | UC16 | — | `204`; con spese collegate `InvalidValue` su `payments` (UC16-R15) |

## Configurazione del backend

Tutta la configurazione sta nell'AppHost di Aspire: parametri in `src/FinHorizon.AppHost/appsettings.json` (sezione `Parameters`), passati a `mysync-backend` come variabili d'ambiente in `AppHost.cs`. Gli `appsettings` del backend non la contengono; nel codice resta solo un valore predefinito.

| Parametro Aspire | Variabile d'ambiente | Valore | Note |
|---|---|---|---|
| `mysync-max-accounts` | `MySync__MaxAccounts` | 2 | 1–1000; un valore fuori intervallo blocca l'avvio (UC2-R6) |
| `mysync-max-debts` | `MySync__MaxDebts` | 1 | 0–1000; 0 disattiva i debiti (UC3-R5) |
| `mysync-max-subscriptions` | `MySync__MaxSubscriptions` | 10 | 0–1000; 0 disattiva gli abbonamenti (UC16-R4) |
| `mysync-time-zone` | `Calendar__TimeZone` | `Europe/Rome` | Decide cos'è "oggi" (RNF4) |
| `mysync-rates-url` | `Currencies__Url` | `https://api.frankfurter.dev/` | Catalogo delle valute e tassi di cambio (UC13, D10); obbligatorio |
