# UC11 – Verificare un campo univoco del cliente

**Attore:** Amministratore
**Obiettivo:** sapere subito, mentre compila l'anagrafica, se ragione sociale, P.IVA completa o PEC sono valide e non già usate da un altro cliente (di supporto a UC3 e UC4).

## Flusso principale
1. L'amministratore compila ragione sociale, P.IVA completa o PEC e passa al campo successivo.
2. Il sistema verifica quel solo campo con le stesse regole del salvataggio (UC3 R1–R4).
3. Il sistema risponde che il valore è valido oppure con il codice dell'errore, lo stesso che darebbe il salvataggio.

## Flussi alternativi
- **2a. Modifica di un cliente esistente (UC4):** l'amministratore indica quale cliente sta modificando; i valori di quel cliente non contano come duplicati.
- **2b. Campo che non si può verificare:** la richiesta è rifiutata come non valida.

## Regole
- R1. Si possono verificare solo ragione sociale (`companyName`), P.IVA completa (`completeVatNumber`) e PEC (`certifiedEmail`).
- R2. Valgono le regole di UC3: obbligatorietà, formato, lunghezza e unicità, con gli stessi codici (`Required`, `InvalidFormat`, `MaxLengthExceeded`, `NotUnique`).
- R3. La verifica non registra nulla e non sostituisce i controlli del salvataggio: un altro amministratore può usare lo stesso valore nel frattempo (UC3 flusso 3b).
- R4. Se il valore è già usato, la risposta indica quale cliente lo usa (id e ragione sociale), per poterlo aprire.

## Scenari di accettazione

Test: [`UC11_VerificareCampoCliente`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC11_VerificareCampoCliente.cs)

### UC11-S1

Test: `UC11_VerificareCampoCliente.S1_ValoreLibero`

```gherkin
# language: it
Funzionalità: Verificare un campo univoco del cliente

Schema dello scenario: Valore non usato da nessun cliente
  Dato che esiste il cliente "Alfa Srl" con P.IVA completa "IT11111111111" e PEC "alfa@pec.it"
  Quando l'amministratore verifica il campo "<campo>" con il valore "<valore>"
  Allora il valore risulta valido

  Esempi:
    | campo             | valore        |
    | companyName       | Beta Snc      |
    | completeVatNumber | IT22222222222 |
    | certifiedEmail    | beta@pec.it   |
```

### UC11-S2

Test: `UC11_VerificareCampoCliente.S2_ValoreGiaUsato`

```gherkin
# language: it
Funzionalità: Verificare un campo univoco del cliente

Schema dello scenario: Valore già usato, con lo stesso confronto del salvataggio
  Dato che esiste il cliente "Alfa Srl" con P.IVA completa "IT11111111111" e PEC "alfa@pec.it"
  Quando l'amministratore verifica il campo "<campo>" con il valore "<valore>"
  Allora la risposta è "NotUnique"

  Esempi:
    | campo             | valore        |
    | companyName       | ALFA s.r.l.   |
    | completeVatNumber | it11111111111 |
    | certifiedEmail    | ALFA@PEC.IT   |
```

### UC11-S3

Test: `UC11_VerificareCampoCliente.S3_IValoriDelClienteInModificaNonSonoDuplicati`

```gherkin
# language: it
Funzionalità: Verificare un campo univoco del cliente

Scenario: I valori del cliente in modifica non sono duplicati
  Dato che esiste il cliente "Alfa Srl"
  Quando l'amministratore verifica la ragione sociale "Alfa Srl" indicando che sta modificando "Alfa Srl"
  Allora il valore risulta valido
```

### UC11-S4

Test: `UC11_VerificareCampoCliente.S4_StesseRegoleDelSalvataggio`

```gherkin
# language: it
Funzionalità: Verificare un campo univoco del cliente

Schema dello scenario: Formato e obbligatorietà con gli stessi codici del salvataggio
  Quando l'amministratore verifica il campo "<campo>" con il valore "<valore>"
  Allora la risposta è "<codice>"

  Esempi:
    | campo             | valore                | codice            |
    | companyName       |                       | Required          |
    | companyName       | AAAAAAAAAAAAAAAAAAAAA | MaxLengthExceeded |
    | completeVatNumber | 12345                 | InvalidFormat     |
    | certifiedEmail    | non-una-pec           | InvalidFormat     |
```

### UC11-S5

Test: `UC11_VerificareCampoCliente.S5_CampoNonVerificabile`

```gherkin
# language: it
Funzionalità: Verificare un campo univoco del cliente

Scenario: Campo che non si può verificare
  Quando l'amministratore verifica il campo "street"
  Allora la richiesta è rifiutata come non valida
```

### UC11-S6

Test: `UC11_VerificareCampoCliente.S6_IndicaIlClienteCheUsaGiaIlValore`

```gherkin
# language: it
Funzionalità: Verificare un campo univoco del cliente

Scenario: Il duplicato indica quale cliente usa già il valore
  Dato che esiste il cliente "Alfa Srl"
  Quando l'amministratore verifica la ragione sociale "ALFA s.r.l."
  Allora il valore risulta "NotUnique" ed è indicato il cliente "Alfa Srl", per poterlo aprire
```
