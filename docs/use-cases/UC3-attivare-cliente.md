# UC3 – Attivare un nuovo cliente

**Attore:** Amministratore
**Obiettivo:** registrare una nuova azienda cliente, eventualmente già con la sua organizzazione.

## Flusso principale
1. L'amministratore inserisce ragione sociale, P.IVA completa, PEC e indirizzo (via, città, CAP), aiutandosi con la ricerca dei comuni (UC10).
2. Facoltativamente aggiunge profili e unità produttive, indicando quali unità apre ogni profilo.
3. Il sistema verifica i dati e registra cliente, profili e unità tutti insieme.
4. Il sistema mostra la scheda del nuovo cliente (UC2).

## Flussi alternativi
- **3a. Dati non validi o duplicati:** il sistema non registra nulla e indica, campo per campo, cosa correggere.
- **3b. Un altro amministratore registra nello stesso momento un cliente con gli stessi dati unici:** il secondo riceve l'indicazione dei campi duplicati, non un errore generico.
- **3c. La lista comuni non è raggiungibile:** città e CAP vengono accettati come scritti.

## Regole
- R1. Tutti i dati anagrafici sono obbligatori.
- R2. Ragione sociale: al massimo 20 caratteri; unica (confronto che ignora maiuscole, spazi e punteggiatura).
- R3. P.IVA completa: due lettere di paese più 11 cifre (es. `IT12345678901`), unica. La P.IVA nazionale (11 cifre) si ricava da essa.
- R4. PEC: indirizzo email valido, unico (senza distinzione di maiuscole).
- R5. Indirizzo solo italiano: il paese è sempre `IT`; la città deve essere un comune esistente e il CAP uno dei suoi (comuni omonimi: decide il CAP).
- R6. Profili e unità produttive sono facoltativi. Nella stessa richiesta i nomi dei profili sono unici tra loro, e così quelli delle unità (senza distinzione di maiuscole); un nome è obbligatorio.
- R7. Un profilo può non aprire nessuna unità; un'unità può non essere aperta da nessun profilo.
- R8. Un cliente cancellato in futuro (soft delete) continuerà a occupare ragione sociale, P.IVA e PEC.

## Scenari di accettazione

Test: [`UC3_AttivareCliente`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC3_AttivareCliente.cs)

### UC3-S1

Test: `UC3_AttivareCliente.S1_AttivazioneConSolaAnagrafica`

```gherkin
# language: it
Funzionalità: Attivare un nuovo cliente

Scenario: Attivazione con la sola anagrafica
  Quando l'amministratore attiva "Alfa Srl" con P.IVA completa "it12345678901", PEC "alfa@pec.it", sede "Via Roma 1", "roma", "00118"
  Allora il cliente è creato e la risposta indica dove consultarlo
  E la scheda mostra la P.IVA "12345678901" e la P.IVA completa "IT12345678901"
  E l'indirizzo ha città "Roma" (nome ufficiale del comune) e paese "IT"
  E il cliente compare nell'elenco dei clienti
```

### UC3-S2

Test: `UC3_AttivareCliente.S2_AttivazioneConProfiliEUnitaCollegati`

```gherkin
# language: it
Funzionalità: Attivare un nuovo cliente

Scenario: Attivazione con profili e unità produttive collegati
  Quando l'amministratore attiva "Alfa Srl" con i profili "Operatore" (chiave "op") e "Ospite" (chiave "osp")
  E con l'unità "Stabilimento" aperta dal profilo "op" e l'unità "Magazzino" senza profili
  Allora il cliente è creato
  E la scheda mostra "Stabilimento" aperta da "Operatore", "Magazzino" senza profili e "Ospite" senza unità
```

### UC3-S3

Test: `UC3_AttivareCliente.S3_DatiObbligatoriMancanti`

```gherkin
# language: it
Funzionalità: Attivare un nuovo cliente

Scenario: Dati obbligatori mancanti
  Quando l'amministratore attiva un cliente senza compilare nessun campo
  Allora la richiesta è rifiutata con "Required" su ragione sociale, P.IVA completa, PEC, via, città e CAP
  E nessun cliente è stato creato
```

### UC3-S4

Test: `UC3_AttivareCliente.S4_FormatiNonValidi`

```gherkin
# language: it
Funzionalità: Attivare un nuovo cliente

Scenario: Formati non validi
  Quando l'amministratore attiva un cliente con ragione sociale di 21 caratteri, P.IVA completa "12345" e PEC "non-una-pec"
  Allora la richiesta è rifiutata con "MaxLengthExceeded" sulla ragione sociale, "InvalidFormat" su P.IVA completa e PEC
```

### UC3-S5

Test: `UC3_AttivareCliente.S5_DatiUniciGiaUsati`

```gherkin
# language: it
Funzionalità: Attivare un nuovo cliente

Scenario: Ragione sociale, P.IVA e PEC già usate da un altro cliente
  Dato che esiste il cliente "Alfa Srl" con P.IVA completa "IT11111111111" e PEC "alfa@pec.it"
  Quando l'amministratore attiva "ALFA s.r.l." con P.IVA completa "it11111111111" e PEC "ALFA@PEC.IT"
  Allora la richiesta è rifiutata con "NotUnique" su ragione sociale, P.IVA completa e PEC
  E esiste ancora un solo cliente
```

### UC3-S6

Test: `UC3_AttivareCliente.S6_IndirizzoNonPresenteNellaListaComuni`

```gherkin
# language: it
Funzionalità: Attivare un nuovo cliente

Schema dello scenario: Città o CAP non presenti nella lista comuni
  Quando l'amministratore attiva un cliente con città "<città>" e CAP "<cap>"
  Allora la richiesta è rifiutata con "InvalidValue" su <campo>

  Esempi:
    | città     | cap   | campo      |
    | Atlantide | 00118 | city       |
    | Roma      | 20100 | postalCode |
```

### UC3-S7

Test: `UC3_AttivareCliente.S7_ListaComuniNonRaggiungibile`

```gherkin
# language: it
Funzionalità: Attivare un nuovo cliente

Scenario: La lista comuni non è raggiungibile
  Dato che la lista comuni non risponde
  Quando l'amministratore attiva un cliente con città "Atlantide" e CAP "00000"
  Allora il cliente è creato con città e CAP come scritti
```

### UC3-S8

Test: `UC3_AttivareCliente.S8_OrganizzazioneNonValida`

```gherkin
# language: it
Funzionalità: Attivare un nuovo cliente

Scenario: Errori nell'organizzazione indicati per posizione
  Quando l'amministratore attiva un cliente con i profili "Operatore" e "operatore " con la stessa chiave
  E un'unità senza nome che apre il profilo con chiave "inesistente"
  Allora la richiesta è rifiutata con "NotUnique" sul nome e sulla chiave del secondo profilo
  E "Required" sul nome dell'unità e "InvalidValue" sul suo riferimento al profilo
  E nessun cliente è stato creato
```
