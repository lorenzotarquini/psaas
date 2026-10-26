# UC23 – Fatturare i periodi delle sottoscrizioni

**Attore:** Amministratore
**Epica:** EXP-08 (fatturazione interna, senza SDI)
**Obiettivo:** che ogni periodo pagato di una sottoscrizione abbia la sua fattura, senza interventi manuali.

## Flusso principale
1. Quando un cliente sottoscrive un piano a pagamento (UC17, non in prova), il sistema emette la fattura del primo periodo.
2. Ogni giorno il sistema rinnova le sottoscrizioni arrivate alla data di rinnovo con rinnovo automatico: emette la fattura del periodo successivo e sposta la data di rinnovo.
3. Alla fine della prova il sistema converte la sottoscrizione in pagata (cliente da Trial ad Attivo) ed emette la prima fattura.
4. Se il cliente cambia piano a periodo in corso, il sistema fattura la differenza per i giorni che restano, oppure emette una nota di credito se il nuovo piano costa meno.
5. Ogni fattura emessa viene consegnata al canale di trasmissione (oggi interno, in futuro SDI).

## Flussi alternativi
- **2a. Rinnovo automatico spento:** la sottoscrizione scade (Expired) senza fattura.
- **3a. Prova senza rinnovo automatico:** la sottoscrizione scade e il cliente resta in Trial, da gestire.

## Regole
- R1. Numero progressivo per anno: `2026/0001`; le note di credito hanno la stessa numerazione con tipo `CreditNote`.
- R2. Scadenza a 30 giorni dall'emissione; IVA 22%; importi in euro con due decimali.
- R3. La fattura riporta cliente (ragione sociale, P.IVA, PEC, codice SDI come erano all'emissione), periodo e righe; non cambia più.
- R4. Pro-rata: differenza di prezzo × giorni rimanenti / giorni del periodo, arrotondata al centesimo; niente se la differenza è zero.
- R5. Ogni fattura e nota di credito emessa va nell'audit log e nello storico del cliente.
- R6. Il processo giornaliero si può lanciare anche a mano (`POST /api/billing/run`); rilanciarlo nello stesso giorno non emette doppioni.

## Scenari di accettazione

Test: [`UC23_Fatturazione`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC23_Fatturazione.cs)

### UC23-S1

Test: `UC23_Fatturazione.S1_PrimaFatturaAllaSottoscrizione`

```gherkin
# language: it
Funzionalità: Fatturazione dei periodi

Scenario: Prima fattura alla sottoscrizione
  Dato il piano "pro" a 100 € al mese
  Quando "Alfa Srl" sottoscrive "pro"
  Allora c'è una fattura di "Alfa Srl" con imponibile 100 €, IVA 22 €, totale 122 €, scadenza tra 30 giorni, numero "<anno>/0001"
```

### UC23-S2

Test: `UC23_Fatturazione.S2_RinnovoMensile`

```gherkin
# language: it
Funzionalità: Fatturazione dei periodi

Scenario: Rinnovo del periodo
  Dato che "Alfa Srl" ha sottoscritto "pro" mensile
  Quando passano 31 giorni e gira la fatturazione giornaliera
  Allora "Alfa Srl" ha 2 fatture e il rinnovo è spostato di un mese
  E rilanciando la fatturazione lo stesso giorno non si aggiungono fatture
```

### UC23-S3

Test: `UC23_Fatturazione.S3_FineProva`

```gherkin
# language: it
Funzionalità: Fatturazione dei periodi

Scenario: Fine della prova
  Dato che "Alfa Srl" è in prova di 14 giorni su "pro"
  Quando passano 15 giorni e gira la fatturazione
  Allora la sottoscrizione è attiva, "Alfa Srl" è "Attivo" e ha la prima fattura
```

### UC23-S4

Test: `UC23_Fatturazione.S4_CambioPianoProRata`

```gherkin
# language: it
Funzionalità: Fatturazione dei periodi

Scenario: Cambio di piano a metà periodo
  Dato che "Alfa Srl" ha "base" a 100 € al mese da 15 giorni
  Quando passa a "pro" a 160 € al mese
  Allora c'è una fattura di adeguamento di 30 € di imponibile
  E tornando a "base" c'è una nota di credito di 30 € di imponibile
```

### UC23-S5

Test: `UC23_Fatturazione.S5_SenzaRinnovoAutomatico`

```gherkin
# language: it
Funzionalità: Fatturazione dei periodi

Scenario: Senza rinnovo automatico
  Dato che "Alfa Srl" ha "pro" senza rinnovo automatico
  Quando passa la data di rinnovo e gira la fatturazione
  Allora la sottoscrizione è scaduta e non c'è una seconda fattura
```
