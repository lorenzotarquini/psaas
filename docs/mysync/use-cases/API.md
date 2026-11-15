# MySync – Contratto HTTP (MVP)

Tutte le rotte sono sotto `/api`, richiedono un token Keycloak con audience `mysync-api` e ruolo di realm `enduser` (GEN-S1, GEN-S3) e operano solo sui dati dell'utente del token (`sub`). Gli importi sono numeri decimali in euro con al massimo 2 decimali; le date sono `yyyy-MM-dd`. `GET /health` è pubblico.

**Errori.** `400` con `ValidationProblemDetails`: in `errors` un codice per campo, gli stessi dell'Admin Portal (`Required`, `InvalidFormat`, `MaxLengthExceeded`, `NotUnique`, `InvalidValue`) più `LimitReached`, che arriva sul campo `limit` perché non riguarda un singolo input. Un id nell'URL inesistente o di un altro utente risponde `404`; un id nel corpo (conto, categoria, debito) di un altro utente è `InvalidValue` su quel campo.

**Enumerazioni** per nome, senza distinzione di maiuscole (in risposta in PascalCase): conto `CurrentAccount`, `VirtualBank`, `Cash`, `Valuables`, `Other`; debito `Loan`, `Mortgage`, `Overdraft`, `Other`; movimento e categoria `Expense`, `Income`; periodo `30d`, `90d`, `12m`, `all`.

| Metodo e rotta | Use case | Richiesta | Risposta |
|---|---|---|---|
| `GET /api/me` | — | — | `{ keycloakId, name, email, roles }` dal token |
| `GET /api/accounts` | UC1, UC4 | — | `{ items: [{ id, kind, name, description, tag, initialBalance, openedOn, balance }], max }` |
| `POST /api/accounts` | UC2 | `{ kind, name, description?, tag?, initialBalance }` | `201` con il conto |
| `GET /api/debts` | UC1, UC4 | — | `{ items: [{ id, kind, name, description, tag, accountId, initialResidual, residual, createdOn, paidOff }], max }`; attivi per residuo decrescente, estinti in fondo |
| `POST /api/debts` | UC3 | `{ kind, name, description?, tag?, accountId, residual }` | `201` con il debito |
| `GET /api/tags` | UC2, UC3 | — | Tag già usati su conti e debiti, distinti e ordinati |
| `GET /api/categories?kind=&term=` | UC6 | — | Categorie dell'utente (di quel tipo, se indicato) il cui nome contiene `term`; al massimo 50 |
| `POST /api/categories` | UC6 | `{ kind, name }` | `201` con la categoria nuova, `200` con quella esistente (UC6-R4) |
| `GET /api/transactions?from=&to=&kind=&accountId=&categoryId=&debtId=&page=&pageSize=` | UC4, UC8 | — | `{ items: [{ id, kind, date, amount, accountId, accountName, categoryId, categoryName, debtId, debtName, note, recordedAt }], page, pageSize, totalCount, expenseTotal, incomeTotal }`; dal più recente; `pageSize` 1–100 (25); totali su tutto il filtro; `from` > `to` è `InvalidValue` su `period` |
| `GET /api/transactions/{id}` | UC8 | — | Il movimento |
| `POST /api/transactions` | UC5 | `{ kind, date, amount, accountId, categoryId, debtId?, note? }` | `201` con il movimento |
| `PUT /api/transactions/{id}` | UC8 | come `POST` | `200` con il movimento aggiornato |
| `DELETE /api/transactions/{id}` | UC8 | — | `204` |
| `GET /api/dashboard` | UC4 | — | `{ netWorth, debtsResidual, accounts, debts, recentTransactions (10), limits: { maxAccounts, maxDebts } }`; `netWorth` è la somma dei saldi, i debiti non vengono sottratti (D9) |
| `GET /api/net-worth?period=` | UC7 | — | `{ period, points: [{ date, netWorth, debts }], change, changePercent }`; 30 giorni se `period` manca; `changePercent` è `null` se il patrimonio iniziale è 0 |

## Configurazione del backend

| Chiave | Variabile d'ambiente | Predefinito | Note |
|---|---|---|---|
| `MySync:MaxAccounts` | `MySync__MaxAccounts` | 2 | 1–1000; un valore fuori intervallo blocca l'avvio (UC2-R6) |
| `MySync:MaxDebts` | `MySync__MaxDebts` | 1 | 0–1000; 0 disattiva i debiti (UC3-R5) |
| `Calendar:TimeZone` | `Calendar__TimeZone` | `Europe/Rome` | Decide cos'è "oggi" (RNF4) |
| `Database:MigrateOnStartup` | `Database__MigrateOnStartup` | `false` (`true` in Development) | Applica le migrazioni EF all'avvio |
