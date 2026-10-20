# PLAN – Fix feature Tenants (AdminPortal)

Vincoli: vertical slice (`Features/Tenants`) + clean architecture (Domain / Application / Infrastructure). UI MudBlazor, test xUnit. Nessuna nuova dipendenza NuGet (si usano HttpClient, IMemoryCache e ILogger del framework).

## Decisioni prese
- Comuni: servizio pubblico esterno, interrogato a runtime con cache.
- Sidebar: confermato, oggi la voce attiva ha sfondo trasparente.
- Caricamento lento: fix **globale**, non per singola pagina.

## Requisiti, approccio e criteri di accettazione

### 1. Caricamento lento + spinner (globale)
- Causa: `Components/Routes.razor:2` usa `InteractiveServer` con prerender attivo. Ogni pagina esegue `OnInitializedAsync` due volte (SSR + circuito), quindi le query sono doppie.
- Fix globale:
  - **Prerender disattivato a livello app**: `new InteractiveServerRenderMode(prerender: false)` dove è dichiarato il rendermode (Routes/App.razor). Vale per tutte le feature, presenti e future.
  - **Componente condiviso** `Components/Shared/LoadingIndicator.razor` (`MudProgressCircular` centrato, parametro `Loading` + `ChildContent`). Ogni feature lo usa al posto di logica ad hoc. Lo adotta subito `TenantGrid.razor`.
  - Indicatore globale durante la navigazione: `<Navigating>` del `Router` in Routes.razor, che mostra `LoadingIndicator` tra un click in sidebar e il render della pagina.
- Accettazione: una sola query per apertura pagina (verificata con il log del punto 7); spinner visibile dal click in sidebar fino ai dati.
- Fuori scope: paginazione lato DB.

### 2. Sidebar: voce attiva con sfondo bianco
- Causa probabile: in `NavMenu.razor.css:53-57` il selettore `::deep` (CSS isolation) non raggiunge il `MudNavLink` perché la radice del componente è un componente Mud, non un elemento HTML con lo scope.
- Fix globale: si sposta lo stile della voce attiva in `wwwroot/app.css` (o si avvolge il menu in un `<div>` con lo scope) così vale per ogni voce, anche quelle future.
- Accettazione: voce attiva con testo blu, bordo bianco e sfondo bianco.

### 3. Validazione form `organizations/add`
- 3a. Rosso che non sparisce: i setter di CompanyName, VatNumber e CertifiedEmail (`TenantCreateFormViewModel.cs:53/59/65`) non rivalidano. Si allineano a Street/City/PostalCode (:71-89): se il campo ha già un errore, si rivalida subito il formato. L'unicità resta con debounce e blur.
- 3b. Messaggi specifici per campo: mappatura (campo, codice) -> chiave resx in `TenantCreateForm.razor:176-189`, nuove chiavi it/en:
  - `Validation.VatNumber.InvalidFormat`: "Formato partita IVA non valido, utilizzare il formato IT00000000000"
  - `Validation.CertifiedEmail.InvalidFormat`: "Formato PEC non valido (es. nome@pec.it)"
  - `Validation.PostalCode.InvalidFormat`: "CAP non valido, utilizzare 5 cifre"
  - `Validation.CompanyName.NotUnique`: "Ragione sociale già esistente"
  - `Validation.VatNumber.NotUnique`: "Partita IVA già esistente"
  - `Validation.CertifiedEmail.NotUnique`: "PEC già esistente"
  - `Validation.<Campo>.MaxLengthExceeded`: "Massimo N caratteri"
  - Il fallback resta quello generico.
- 3c. Duplicato mostrato come "Campo obbligatorio": un test lo riproduce prima del fix (service + ViewModel con uniqueness checker finto).
- Accettazione: l'errore di formato sparisce appena il valore è corretto; ogni errore ha il suo messaggio specifico; un duplicato mostra "... già esistente".

### 4. Città: combobox con comuni italiani da servizio esterno
- Application: `Abstractions/Geography/IItalianCityProvider` con `Task<IReadOnlyList<string>> SearchAsync(string term, CancellationToken)`.
- Infrastructure: `Geography/ItalianCityProvider`
  - typed HttpClient, URL base in `appsettings` (`ItalianCities:BaseUrl`);
  - scarica l'elenco una volta e lo tiene in `IMemoryCache` (24 h); filtra in memoria;
  - log, timeout e gestione errori.
- Fonte proposta: JSON pubblico `comuni-json` (dati ISTAT, `https://raw.githubusercontent.com/matteocontrini/comuni-json/master/comuni.json`). Nessuna chiave; un solo download. In alternativa un'API REST pubblica (es. comuni-ita), sostituibile solo da config/implementazione.
- UI: `MudAutocomplete<string>` al posto del `MudTextField` città (`TenantCreateForm.razor:85`), con `CoerceValue=false` (solo valori della lista) e `MinCharacters=2`.
- Servizio non raggiungibile: **DA DECIDERE** (Q1).
- Accettazione: digitando 2+ caratteri compaiono i comuni corrispondenti; il valore salvato è un comune della lista.

### 5. Profili: riga sempre presente
- ViewModel: all'avvio `Profiles` contiene 1 riga vuota; `RemoveProfile` non fa nulla se resta 1 riga; nuova proprietà `CanRemoveProfile` (Count > 1).
- UI (`TenantCreateForm.razor:98-110`):
  - riga `[Nome][Descrizione][Bin][+]` allineata al centro verticalmente;
  - Bin `Disabled="!CanRemoveProfile"`, con aria-label;
  - `+` chiama `AddProfile`;
  - si rimuovono il pulsante "Aggiungi" in alto e il `MudAlert` Empty.
- Accettazione: non si può scendere sotto 1 riga; il bin si abilita con 2+ righe.

### 6. Unità produttive: come il punto 5
- All'avvio 1 riga, già associata al primo profilo; `CanRemoveProductiveUnit`; stessa UI `[Nome][Descrizione][Profili][Bin][+]`.
- Il vincolo "Aggiungi unità disabilitato senza profili" cade, perché un profilo c'è sempre.
- Accettazione: non si può scendere sotto 1 riga.

### 7. Log
- `ILogger<T>` del framework, nessuna nuova libreria.
- Classi: TenantGridViewModel, TenantCreateFormViewModel, TenantService, TenantCreateService, TenantEditService, TenantReadRepository, TenantWriteRepository, TenantUniquenessChecker, ProfileReadRepository / repository Profiles, ItalianCityProvider.
- Livelli:
  - Debug: caricamenti e query;
  - Information: tenant creato o modificato (Id);
  - Warning: validazione fallita, duplicato, servizio comuni non raggiunto;
  - Error: eccezioni.
- Niente dati sensibili (PEC, P.IVA completa) nei messaggi: solo Id e codici errore.
- Livelli di default in `appsettings.json` / `appsettings.Development.json`.

## Fette di lavoro (TDD)

| # | Fetta | Test (actarus-tester) | Tipo |
|---|-------|-----------------------|------|
| F1 | Righe profili/unità min 1 (5-6) | ROSSI: init 1 riga; Remove sull'ultima riga non fa nulla; `CanRemove*`; prima unità associata al primo profilo; aggiornamento dei test esistenti che assumono liste vuote | nuova funzionalità |
| F2 | Validazione + messaggi (3) | ROSSI: errore azzerato al cambio valore per i 3 campi; NotUnique -> codice unicità (riproduce il bug); mappatura messaggi per campo | bugfix con riproduzione |
| F3 | Città (4) | ROSSI: provider con HttpMessageHandler finto -> filtro e cache (1 sola chiamata HTTP); comportamento su errore HTTP secondo Q1 | nuova funzionalità |
| F4 | Prerender off + LoadingIndicator + Navigating (1) | test ViewModel `IsLoading` true->false; il resto con verifica manuale | fix globale UI |
| F5 | Sidebar CSS (2) | nessuno (solo CSS), verifica visiva | |
| F6 | Logging (7) | nessun test nuovo; suite esistente verde | trasversale |

Ordine: F1 → F2 → F3 → F4 → F5 → F6. A fine di ogni fetta: suite completa e aggiornamento di `## Stato`. Se si blocca (stesso errore 2 volte) o si superano ~30 min: stop e nota qui sotto.

## Domande aperte
- **Q1 – Servizio comuni non raggiungibile:**
  - (A) campo città a testo libero con avviso (la creazione del tenant non si blocca);
  - (B) creazione bloccata finché il servizio non risponde.
  - Consiglio: A.
- **Q2 – Fonte:** va bene il JSON `comuni-json` su GitHub (dati ISTAT, un solo download in cache) o preferisci un'API REST specifica?

Risposte: Q1 = A (testo libero con avviso), Q2 = comuni-json.

## Iterazione 2 – feedback utente

### R1. Spinner dentro il layout
- Causa: `<Navigating>` del Router (`Routes.razor:4-6`) sostituisce l'intero `<Found>`, layout compreso, quindi sidebar e topbar spariscono.
- Fix: si rimuove `<Navigating>`. Lo spinner resta solo nel contenuto della pagina (`LoadingIndicator`), che è già dentro `<article class="content">` di MainLayout. Ogni pagina che carica dati lo usa con il proprio `IsLoading`, che parte a true. Nessun servizio di stato in più: la navigazione Blazor Server è istantanea, la lentezza è il caricamento dati.
- Accettazione: cliccando in sidebar, sidebar e topbar restano visibili e lo spinner compare solo nell'area contenuti.

### R2. Combobox città
- "Roma" mancante: filtro `Contains` + ordine alfabetico + `MaxItems` di MudAutocomplete (default 10), quindi Roma viene tagliata. Fix nel provider: prima corrispondenza esatta, poi "inizia con", poi "contiene"; limite risultati gestito dal provider (es. 20).
- Valore che sparisce e ricompare dopo la selezione: sincronizzazione Text/Value di MudAutocomplete. Fix: binding esplicito `Value` + `ValueChanged` sull'oggetto città (non sulla stringa), `ToStringFunc`, nessuna nuova ricerca dopo la selezione. Verifica a mano.
- Accettazione: "rom" mostra Roma per prima; la selezione resta stabile.

### R3. CAP automatico
- Il provider restituisce `ItalianCity(Name, ProvinceCode, PostalCodes)` (campi `nome`, `sigla`, `cap` di comuni-json) al posto di `string`. I comuni sono mostrati come "Roma (RM)" per distinguere gli omonimi.
- Alla selezione: CAP unico -> compilato automaticamente. CAP multipli: **DA DECIDERE** (Q4).
- Con servizio non disponibile resta tutto manuale.

### R4. Pulsante + fuori dalla card
- Il `+` va fuori dal bordo della card, sotto la lista o a destra, allineato. Nella card restano solo [campi][cestino].

### R5. Modifica di profili e unità produttive
- Situazione oggi:
  - `UpdateAsync` aggiorna solo i dati della società.
  - Manca una lettura per tenant di profili e unità.
  - I `TemporaryId` esistono solo nella creazione.
  - `BuildProductiveUnitProfiles` è riutilizzabile.
- Vincolo critico: `Profiles` non ha `TenantId` e il nome è unico su tutto il sistema; anche il nome delle unità è unico globale (`PsaasDbContext.cs:49, 84-96`). Eliminare un profilo dall'edit di una società lo toglie a tutte. Modello: **DA DECIDERE** (Q3).
- Approccio (dopo Q3):
  - estrarre le sezioni profili/unità in componenti condivisi tra create ed edit;
  - aggiungere la lettura per tenant;
  - aggiornare `UpdateAsync` per aggiungere, modificare e rimuovere in una transazione;
  - mantenere il vincolo "almeno 1 riga".
- Test TDD sul service e sul repository di edit.

### Domande aperte (iterazione 2)
- **Q3 – Profili:**
  - (A) per singola società: migration con `TenantId` su Profiles, nome unico per (TenantId, Name); idem per il nome dell'unità;
  - (B) condivisi tra società: l'edit può solo associare o dissociare, non eliminare né rinominare.
  - Consiglio: A.
- **Q4 – CAP multipli (es. Roma, Milano):**
  - (A) il campo CAP diventa una select con i CAP del comune;
  - (B) campo vuoto, da scrivere a mano;
  - (C) primo CAP precompilato.
  - Consiglio: A.

### Fette iterazione 2
| # | Fetta | Test |
|---|-------|------|
| G1 | R1 spinner nel layout | nessuno (UI), verifica manuale |
| G2 | R4 pulsante + fuori card | nessuno (UI) |
| G3 | R2+R3 provider (ordinamento, ItalianCity con CAP/sigla) + UI combobox + CAP | ROSSI: ordinamento esatto/inizia/contiene, mapping cap/sigla, ViewModel compila il CAP alla selezione |
| G4 | R5 migration (se Q3=A) | test repository |
| G5 | R5 edit profili/unità (service, repository, componenti condivisi) | ROSSI: aggiungi/modifica/rimuovi, min 1 riga, transazione |

## Stato
- [x] Analisi completata
- [x] Decisioni: comuni da servizio esterno, fix globale, sidebar confermata, Q1=A, Q2=comuni-json
- [x] Piano validato
- [x] F1 test scritti: 6 nuovi + FillValidValues/SubmitAsync adattati; rossi (non compila: CanRemove* mancanti). Nota: AdminPortal in esecuzione blocca i file, build con output alternativo.
- [x] F1 VERDE (51/51). File: TenantCreateFormViewModel.cs, TenantCreateForm.razor, resx it/en. Chiavi `.Empty` resx ora inutilizzate (lasciate). Allineamento visivo da verificare a mano.
- [x] F2 test: TenantFieldErrorFeedbackTests.cs, 22 test (13 rossi). Bug "duplicato = Campo obbligatorio" NON riprodotto nei percorsi service/GoNext/Submit: causa probabile = errore Required stantio non azzerato (punto 3a).
- [x] F2 VERDE (73/73). File: TenantCreateFormViewModel.cs, TenantFieldErrorMessages.cs (nuovo), TenantCreateForm.razor, resx it/en. Nota: rivalidazione sincrona via TenantCreateValidator interno con checker "sempre unico" (da rivedere se si preferisce metodo solo-formato).
- [x] F3 test: src/tests/PSAAS.Infrastructure.Tests/ItalianCityProviderTests.cs, 5 rossi (tipo mancante, via reflection).
- [x] F3 VERDE (Infra 13/13, AdminPortal 73/73). Nuovi: IItalianCityProvider, ItalianCityProvider. Note: HttpClient singleton creato a mano (Microsoft.Extensions.Http non referenziato in Infrastructure); provider iniettato nel razor, non nel ViewModel. Chiamata GitHub reale non verificata.
- [x] F4 VERDE (73/73). Prerender off in App.razor (Routes + HeadOutlet), Navigating + Components/Shared/LoadingIndicator.razor, TenantGrid lo usa, _isLoading parte true. Pages/Counter.razor (demo) non toccato.
- [x] F5 build OK. Causa: specificità MudBlazor sullo stato active; fix con !important nella regola active di NavMenu.razor.css. Verifica visiva da fare.
- [x] F6 VERDE (AdminPortal 73/73, Infra 13/13). Logger opzionale in 9 classi. NLog: minLevel regola `*` portata a Debug in appsettings.json (filtro reale nella sezione Logging).
- [x] Iterazione 2 validata: Q3 = A (profili e unità per società), Q4 = A (select CAP).
- [x] G1+G2 implementati (Routes.razor senza Navigating; TenantEditForm usa LoadingIndicator; + fuori card). Test NON eseguiti: app/VS di nuovo in esecuzione, DLL bloccate.
- [x] G3 test: ItalianCityProviderTests aggiornati (4 rossi su 8), TenantCreateSelectCityTests.cs nuovo (4, via reflection; esito da confermare per lock).
- [x] G3 VERDE (Infra 16/16, AdminPortal 77/77). Fallback servizio non disponibile: MudTextField al posto dell'autocomplete. Runtime non verificato.
- [x] Analisi edit:
  - TenantEditService.GetTenantForEditAsync (22-30) e UpdateTenantAsync (32-87) gestiscono solo la società.
  - Dominio: Profile senza TenantId; ProductiveUnit ha TenantId; Tenant senza collection.
  - DB: Profiles.Id è text nella migration ma Guid nel dominio (fuori scope, segnalato).
  - Sezioni profili/unità estraibili: TenantCreateForm.razor:130-166 + helper 182-200.
- [x] Decisione migration: profili orfani eliminati (A, DB develop).
- [x] G4 VERDE (Infra 16/16, AdminPortal 77/77). Migration 20261011210610_ProfilesPerTenant (SQL dati non eseguito su DB, non applicata). Aperto: TenantCreateService accetta ProfileIds esistenti di altri tenant (fuori scope).
- [x] G5a test: TenantEditStructureServiceTests.cs, 9 rossi. Errori a livello di result (codici TenantCreateErrorCodes), non per campo. Nessun DB di test in Infrastructure.Tests, quindi niente test sul repository. I fake nei test esistenti andranno aggiornati dal tester dopo l'implementazione.
- [x] G5a implementato: build funzionale ok. TenantStructureModels.cs nuovo; UpdateWithStructureAsync in transazione, filtro per TenantId. Edit da UI fallisce con ProfilesInvalid fino a G5b (atteso).
- [x] G5a VERDE (AdminPortal 86/86, Infra 16/16). Fake aggiornati.
- [x] G5b test: TenantEditFormViewModelProfilesTests.cs, 5 rossi (membri mancanti).
- [x] G5b implementato: TenantProfilesEditor.razor e TenantProductiveUnitsEditor.razor condivisi; VM edit con struttura. AdminPortal 90 verdi, 1 rosso per un difetto del test (helper reflection su valore null). Nota: con liste null nella read model le sezioni non sono validate né inviate (compatibilità con i test vecchi).
- [x] G5b VERDE. Suite complete: AdminPortal 91/91, Auth 8/8, Infra 16/16, MVVM 4/4.
- [x] Bugfix TaskCanceledException ricerca città (ItalianCityProvider.cs:24 usava il token della ricerca per il download): download unico condiviso con CancellationToken.None, ricerca con WaitAsync(ct). Test di riproduzione aggiunto; Infra 17/17. AdminPortal.Tests rilanciati: 91/91. Poi: ricerca annullata restituisce [] senza eccezioni (Task.WhenAny al posto di WaitAsync); Infra 17/17.
- [x] Fix lag Città/CAP: tolto OnBlur da Autocomplete città e Select CAP (validava il valore vecchio prima di ValueChanged); validazione su selezione. AdminPortal 91/91. Aperti: TenantEditForm ha città a testo libero (niente autocomplete); setter CompanyName/VatNumber/CertifiedEmail usano GetAwaiter().GetResult().
- [x] Iterazione 2 completata.

## Iterazione 3 – latenza form (analisi fatta, in attesa di validazione)
Cause dal codice, in ordine di impatto:
1. `Immediate="true"` su quasi tutti i campi (TenantCreateForm.razor:95-97,103,107,111,121; TenantEditForm.razor:58,61,64; editor profili/unità :13-14): ogni tasto è un roundtrip SignalR + render.
2. `MvvmComponentBase.cs:50` chiama `InvokeAsync(StateHasChanged)` per OGNI PropertyChanged, senza ShouldRender; tutto il form (MudStepper + tutti gli step) viene ri-renderizzato. SelectCity emette 5+ notifiche, quindi 5+ render per una scelta.
3. RenderFragment degli step e lambda `Message` ricreati a ogni render; GetProfileDisplayName calcolato a ogni render (impatto medio-basso).
4. Setter CompanyName/VatNumber/CertifiedEmail con `GetAwaiter().GetResult()` (impatto basso, da correggere).
Esclusi: trasporto (WebSocket su Kestrel), auth per messaggio, provider duplicati, log per tasto, DI.

Fix proposto:
- H1 (globale, PSAAS.MVVM): MvvmComponentBase raggruppa le notifiche, con un solo StateHasChanged per batch (flag `_renderPending`). Vale per tutte le feature.
- H2: campi di testo con `DebounceInterval` ~300 ms al posto di Immediate puro (DA DECIDERE, Q5); autocomplete città con DebounceInterval 300.
- H3: rimozione del sync-over-async nei 3 setter.
- Fuori scope salvo richiesta: ShouldRender/memoizzazione RenderFragment.
Test: H1 con un test in PSAAS.MVVM.Tests (N notifiche consecutive = 1 render); H2 nessuno (UI); H3 suite esistente.
Decisione Q5 = A (DebounceInterval 300 ms).
- [x] H2+H3 VERDE (AdminPortal 91/91). DebounceInterval 300 su tutti i campi Immediate (create, edit, editor profili/unità) e sull'autocomplete città. Unicità di CompanyName/VatNumber/CertifiedEmail ora solo al blur (non più a 2 s di inattività). Validator: 3 metodi sincroni di solo formato, riusati dagli async. Nota: blocco LF in un file CRLF (TenantCreateValidator.cs).
- [x] H1 VERDE (scelta B): test riscritti con notifiche sul dispatcher; MvvmComponentBase rimanda il render con Task.Yield + flag, niente render dopo dispose. MVVM 6/6, AdminPortal 91/91.
- [x] Iterazione 3 completata. Da verificare a mano: latenza percepita (anche con Ctrl+F5). Unicità ora solo al blur.
- (storico) H1 inizialmente bloccato: batching implementato (flag + Interlocked) ma i 2 test restano rossi (5 render invece di 1). Il test chiama le notifiche fuori dal dispatcher, dove InvokeAsync esegue la callback subito. Nell'app reale le notifiche arrivano dentro gli event handler, dove Blazor raggruppa già i StateHasChanged (un render a fine handler): il beneficio di H1 è dubbio. In attesa di decisione: revert H1 + rimozione test, oppure riscrittura del test. Verifiche manuali aperte: spinner, + fuori card, combobox/CAP, edit profili/unità. Migration ProfilesPerTenant da applicare e verificare su DB develop. G5b (ViewModel edit + componenti condivisi) dopo. Scelta test: nessun test nuovo sulla migration (SQL dati verificato con `dotnet ef`/build); le suite esistenti devono restare verdi; la logica per-tenant viene testata in G5.
- [x] Tutte le fette (iterazione 1) completate. Verifiche manuali aperte: allineamento righe profili/unità, autocomplete città con GitHub reale, spinner, sidebar. Scelta test: nessun nuovo test (IsLoading del TenantGridViewModel esiste già e non cambia; prerender/spinner sono solo UI, verifica manuale). Contratto: `IItalianCityProvider.SearchAsync(term, ct)` -> `IReadOnlyList<string>?` (null = servizio non disponibile -> testo libero con avviso)
