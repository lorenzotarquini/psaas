/**
 * Pasted numbers come formatted the way the language writes them: "1.234.567,89" in Italian, "1,234,567.89" in English.
 * The thousands separator goes away (with the spaces some formats use for it), and the decimal one becomes the
 * decimal separator of the calculator. Typing is different: a typed dot or comma is always the decimal separator.
 */
export function sanitizePasted(text: string, decimal: string) {
  const thousands = decimal === ',' ? '.' : ','
  return text
    .trim()
    .replaceAll(thousands, '')
    .replace(/(\d)[\s  ](?=\d)/g, '$1')
}
