/** Euro amounts as the invoices print them: "1.234,50 €" in Italian. */
export const formatMoney = (amount: number, language: string) =>
  new Intl.NumberFormat(language, { style: 'currency', currency: 'EUR' }).format(amount)
