// Parse a Turkish-formatted money string ("1.299,90") to a number.
export function parseTrMoney(s) {
  if (typeof s === 'number') return s
  if (!s) return 0
  const num = Number(String(s).replace(/\./g, '').replace(',', '.'))
  return Number.isNaN(num) ? 0 : num
}
