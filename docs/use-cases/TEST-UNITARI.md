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
| UC1-S4 | Ricerca per PEC | `TenantReadRepositoryTests.UC1_S4_*` |
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
| UC11-S1…S4 | Verifica di un campo con le regole del salvataggio | `TenantCreateValidatorTests` (gli stessi validatori di UC3/UC4); la scelta del campo (S5) è solo accettazione |
| UC11-S6 | Cliente che usa già il valore | `TenantReadRepositoryTests.UC11_S6_*` |
| UC2-S4 | Utenti per profilo | `KeycloakUserDirectoryTests.UC2_S4_*` (lo scenario S5, Keycloak giù, è solo accettazione) |
| UC4-S1…S3, S6 | Modifica anagrafica: P.IVA ricavata, unicità che esclude il cliente, inesistente, duplicato concorrente | `TenantRegistryUpdateServiceTests.UC4_*` |
| UC5-S1…S8 | Organizzazione: identità mantenute, collegamenti per chiave, nomi duplicati, id di altri clienti, inesistente | `TenantOrganizationServiceTests.UC5_*` |
| UC5-S4 | Profili tolti rimossi dagli utenti Keycloak (anche con Keycloak giù) | `TenantOrganizationServiceTests.UC5_S4_*`, `KeycloakUserDirectoryTests.UC5_S4_*` |
| UC6-S1…S7 | Creazione utente: validazione, profili esistenti, email duplicata, Keycloak non raggiungibile | `UserCreateServiceTests.UC6_*` |
| UC6-S6 | Esistenza dei profili scelti | `ProfileReadRepositoryTests.UC6_S6_*` |
| UC6-S1, S5, S7, flusso 3d | Chiamate all'API di amministrazione Keycloak: attributi, email di impostazione password, conflitto, errori, rimozione se l'email non parte | `KeycloakUserDirectoryTests.UC6_*` |
| UC7-S1, S3, S4, S6 | Ricerca utenti: cliente ricavato dal profilo, profili cancellati omessi | `UserSearchServiceTests.UC7_*`, `ProfileReadRepositoryTests.UC7_*` |
| UC7-S1, S2, S6 | Ricerca in Keycloak: ricerca interna al testo, lettura degli attributi, errori | `KeycloakUserDirectoryTests.UC7_*` |
| UC8-S1…S5 | Identità e ruoli dal token | `PSAAS.Auth.Tests` (lettura dei ruoli Keycloak) |
| UC9-S1, S2 | Panoramica dei profili | `ProfileReadRepositoryTests.UC9_*` |
| UC10-S1…S3 | Ricerca comuni: filtro, ordine, limite di 20, errori | `ItalianCityProviderTests` |
| UC12-S1…S7 | Audit: registrazione con autore e ora, filtri, ordine, pagine | `AuditLogTests.UC12_*` |
| UC12-S1…S4 | Eventi scritti da ogni caso d'uso (anagrafica solo se cambia, uno per cliente per gli utenti) | `TenantCreateServiceTests.UC12_*`, `TenantRegistryUpdateServiceTests.UC12_*`, `TenantOrganizationServiceTests.UC12_*`, `UserCreateServiceTests.UC12_*` |
| UC12-S3 | Differenze dell'organizzazione per nome | `OrganizationDiffTests.UC12_*` |
| UC13-S1, S3, S4 | Transizioni consentite, motivo obbligatorio, data di creazione invariata | `TenantLifecycleTests.UC13_*`, `TenantLifecycleServiceTests.UC13_*` |
| UC14-S3, S4 | Solo una bozza si elimina | `TenantLifecycleServiceTests.UC14_*` |
| UC14-S5 | Codice SDI facoltativo, 6–7 caratteri | `TenantCreateValidatorTests.UC14_S5_*` |
| UC15-S2, S3, S5 | Manifest: formato di id, versione e URL, dipendenza da se stesso, core non deprecabile | `CatalogServiceTests.UC15_*` |
| UC16-S1…S3 | Piani: core sempre incluso, codice unico tranne per se stesso, valori non validi per campo | `CatalogServiceTests.UC16_*` |
| UC17-S1…S3 | Sottoscrizione: dipendenze transitive, prova solo la prima volta, limite utenti, piano e add-on inesistenti | `SubscriptionServiceTests.UC17_*`, `SubscriptionServiceTests.WithDependencies_*` |
| UC17-S6 | Nuovo utente oltre il limite del piano | `UserCreateServiceTests.UC17_S6_*` |
| UC18-S1, S2 | Hook fallito, riprova del job, riprova di un modulo già attivo | `SubscriptionServiceTests.UC18_*` |
| UC19-R1 | Stato dell'utente da Keycloak e dai clienti dei profili | `UserManagementServiceTests.UC19_R1_*` |
| UC19-S2, S4…S6 | Modifica con audit solo se cambia, limite solo per clienti nuovi, azioni consentite per stato | `UserManagementServiceTests.UC19_*` |
| UC19 | Chiamate a Keycloak: lettura di stato/inviti/MFA, aggiornamento con `CONFIGURE_TOTP`, reinvio contato, ultimi accessi dagli eventi (e niente se spenti) | `KeycloakUserDirectoryTests.UC19_*` |
| UC20 | Profili per nome senza maiuscole, email ripetute, file vuoto o oltre 500 righe | `UserManagementServiceTests.UC20_*` |
