# UC9 – Gestire i profili

**Attore:** Amministratore
**Obiettivo:** vedere tutti i profili della piattaforma, crearne di nuovi ed eliminarli senza aprire la struttura di ogni organizzazione.

## Flusso principale
1. L'amministratore apre la pagina Profili.
2. Il sistema mostra tutti i profili di tutte le organizzazioni, ordinati per nome, ciascuno con l'organizzazione a cui appartiene.
3. Per creare un profilo l'amministratore sceglie l'organizzazione e indica nome e descrizione; il sistema lo aggiunge alla struttura dell'organizzazione.
4. Per eliminare un profilo l'amministratore conferma; il sistema lo toglie dalla struttura.

## Flussi alternativi
- **2a. Non esistono profili:** il sistema mostra un elenco vuoto.
- **3a. Dati non validi:** il sistema non crea nulla e indica cosa correggere.
- **4a. Il profilo non esiste più:** il sistema risponde che non è stato trovato.

## Regole
- R1. Creare ed eliminare un profilo è una modifica della struttura (UC5): stesse regole e una voce "ha modificato la struttura" nello storico (UC12).
- R2. Organizzazione obbligatoria (`tenantId`: `Required`) ed esistente (`InvalidValue`).
- R3. Nome obbligatorio e unico nell'organizzazione, senza distinzione di maiuscole (UC5-R1); descrizione facoltativa.
- R4. Un profilo nuovo non apre nessuna unità: si collegano dalla scheda dell'organizzazione (UC5).
- R5. Eliminare è sempre consentito (UC5-R4): gli utenti che avevano il profilo lo perdono, e lo perdono anche le unità produttive che apriva.

## Scenari di accettazione

Test: [`UC9_PanoramicaProfili`](../../src/AdminPortal/Tests/FinHorizon.AdminPortal.Backend.Tests/UC9_PanoramicaProfili.cs)

### UC9-S1

Test: `UC9_PanoramicaProfili.S1_TuttiIProfiliConIlLoroCliente`

```gherkin
# language: it
Funzionalità: Consultare la panoramica dei profili

Scenario: Tutti i profili di tutti i clienti, con il loro cliente
  Dato che "Alfa Srl" ha il profilo "Operatore" e "Beta Snc" ha il profilo "Amministrativo"
  Quando l'amministratore apre la panoramica dei profili
  Allora vede "Amministrativo" di "Beta Snc" e poi "Operatore" di "Alfa Srl"
```

### UC9-S2

Test: `UC9_PanoramicaProfili.S2_NessunProfilo`

```gherkin
# language: it
Funzionalità: Consultare la panoramica dei profili

Scenario: Nessun profilo
  Dato che non esistono profili
  Quando l'amministratore apre la panoramica dei profili
  Allora vede un elenco vuoto
```

### UC9-S3

Test: `UC9_PanoramicaProfili.S3_CreareUnProfilo`

```gherkin
# language: it
Funzionalità: Gestire i profili

Scenario: Creare un profilo
  Dato che "Alfa Srl" ha il profilo "Operatore" che apre l'unità "Stabilimento"
  Quando l'amministratore crea il profilo " Revisore " di "Alfa Srl" con descrizione "Sola lettura"
  Allora il profilo "Revisore" compare nella scheda di "Alfa Srl" e nella pagina Profili
  E "Stabilimento" resta aperta solo da "Operatore"
```

### UC9-S4

Test: `UC9_PanoramicaProfili.S4_ProfiloNonValido`

```gherkin
# language: it
Funzionalità: Gestire i profili

Scenario: Profilo non valido
  Dato che "Alfa Srl" ha il profilo "Operatore"
  Quando l'amministratore crea il profilo "OPERATORE" di "Alfa Srl"
  Allora la richiesta è rifiutata con "NotUnique" sul nome
  E senza nome è rifiutata con "Required" sul nome
  E senza organizzazione è rifiutata con "Required" su "tenantId", con un'organizzazione inesistente con "InvalidValue"
  E "Alfa Srl" ha ancora un solo profilo
```

### UC9-S5

Test: `UC9_PanoramicaProfili.S5_EliminareUnProfilo`

```gherkin
# language: it
Funzionalità: Gestire i profili

Scenario: Eliminare un profilo lo toglie a utenti e unità
  Dato che "Alfa Srl" ha i profili "Magazziniere" e "Operatore", entrambi aprono l'unità "Magazzino"
  E l'utente "Mario Rossi" ha solo il profilo "Magazziniere"
  Quando l'amministratore elimina il profilo "Magazziniere"
  Allora la scheda di "Alfa Srl" mostra solo "Operatore", che apre ancora "Magazzino"
  E cercando "Mario" l'utente risulta senza profili
```

### UC9-S6

Test: `UC9_PanoramicaProfili.S6_EliminareUnProfiloInesistente`

```gherkin
# language: it
Funzionalità: Gestire i profili

Scenario: Profilo inesistente
  Quando l'amministratore elimina un profilo che non esiste
  Allora il sistema risponde che il profilo non è stato trovato
```
