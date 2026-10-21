# Test unitari derivati dagli scenari

Ogni scenario di accettazione (`UCn-Sm`) scende negli strati interni: qui si vede quale classe ne porta la responsabilità e quale test unitario la verifica. I nomi dei test iniziano con l'identificativo dello scenario da cui derivano (`UC3_S5_...`).

| Progetto | Cosa isola | Dipendenze |
|---|---|---|
| `PSAAS.Domain.Tests` | entità del dominio | nessuna |
| `PSAAS.Application.Tests` | casi d'uso e validatori | porte simulate (`Fakes.cs`) |
| `PSAAS.Infrastructure.Tests` | repository, convertitori, provider esterni | EF InMemory, `HttpMessageHandler` simulato |

## Copertura per scenario

| Scenari | Responsabilità | Test unitari |
|---|---|---|
| UC1-S1, S2, S3, S5 | Ricerca, ordinamento e paginazione dei clienti | `TenantReadRepositoryTests.UC1_*` |
| UC1-S2, UC3-S5 | Normalizzazione di ragione sociale, P.IVA, PEC | `TenantFieldNormalizerTests` |
| **UC1-S4** | Ricerca per PEC | `TenantReadRepositoryTests.UC1_S4_*` — **rosso: da implementare** |
| UC1-S6 | Limiti di paginazione | solo accettazione (regola del controller) |
| UC2-S1, S2, S3 | Lettura del cliente e della sua organizzazione | `TenantReadRepositoryTests.UC2_*`, `TenantTests.Rehydrate_*`, `TenantDbModelConverterTests` |
| UC3-S1 | Creazione: P.IVA nazionale ricavata, paese IT, nome ufficiale del comune | `TenantCreateServiceTests.UC3_S1_*`, `TenantWriteRepositoryTests.UC3_S1_*`, `TenantTests.Create_*` |
| UC3-S2 | Profili e unità collegati tramite chiave | `TenantCreateServiceTests.UC3_S2_*`, `TenantWriteRepositoryTests.UC3_S2_*` |
| UC3-S3, S4 | Campi obbligatori e formati | `TenantCreateValidatorTests`, `TenantCreateServiceTests.UC3_S3_*`, `UC3_S4_*` |
| UC3-S5 | Unicità | `TenantCreateValidatorTests`, `TenantCreateServiceTests.UC3_S5_*`, `TenantUniquenessCheckerTests.UC3_*` |
| UC3-S6, S7 | Città e CAP dalla lista comuni, lista non raggiungibile | `TenantCreateServiceTests.UC3_S6_*`, `UC3_S7_*` |
| UC3-S8 | Errori dell'organizzazione per posizione | `TenantCreateServiceTests.UC3_S8_*` |
| UC3 flusso 3b | Duplicato concorrente | `TenantCreateServiceTests.UC3_3b_*` (la conversione dell'errore Postgres nel repository non è testabile con InMemory) |
| UC4-S2, S3 | Unicità che esclude il cliente stesso | `TenantCreateValidatorTests.ExcludedTenant_*`, `TenantUniquenessCheckerTests.UC4_*` |
| UC6-S6 | Esistenza dei profili scelti | `ProfileReadRepositoryTests.UC6_S6_*` |
| UC8-S1…S5 | Identità e ruoli dal token | `PSAAS.Auth.Tests` (lettura dei ruoli Keycloak) |
| UC9-S1, S2 | Panoramica dei profili | `ProfileReadRepositoryTests.UC9_*` |
| UC10-S1…S3 | Ricerca comuni: filtro, ordine, limite di 20, errori | `ItalianCityProviderTests` |

## Da scrivere insieme all'implementazione

Gli use case non ancora implementati avranno i loro test unitari scritti prima del codice (test rosso → implementazione → verde):

| Scenari | Test unitari previsti |
|---|---|
| UC4-S1…S6 | caso d'uso di modifica anagrafica (Application); `TenantWriteRepository.UpdateAsync` |
| UC5-S1…S8 | caso d'uso di modifica organizzazione (nomi unici, id di altri clienti); `TenantWriteRepository.UpdateWithStructureAsync` (aggiunte, rinomine, rimozioni, collegamenti) |
| UC6-S1…S7 | caso d'uso di creazione utente (validazione, profili esistenti, email duplicata, Keycloak non raggiungibile); implementazione Keycloak di `IUserDirectory` (chiamate HTTP all'API di amministrazione, email di impostazione password) |
| UC7-S1…S6 | caso d'uso di ricerca utenti (profili cancellati omessi, cliente ricavato dal profilo); ricerca nell'implementazione Keycloak di `IUserDirectory` |
