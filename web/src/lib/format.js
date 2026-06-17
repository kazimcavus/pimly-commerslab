export function relativeTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  const s = (Date.now() - d.getTime()) / 1000
  if (s < 60) return 'az önce'
  const m = s / 60
  if (m < 60) return `${Math.floor(m)} dk önce`
  const h = m / 60
  if (h < 24) return `${Math.floor(h)} saat önce`
  const days = h / 24
  if (days < 30) return `${Math.floor(days)} gün önce`
  return d.toLocaleDateString('tr-TR')
}

// Format a numeric (number | string) as Turkish money (1.299,90).
export function trMoney(n) {
  if (n == null || n === '') return ''
  const num = typeof n === 'number' ? n : Number(String(n).replace(/\./g, '').replace(',', '.'))
  if (Number.isNaN(num)) return String(n)
  return num.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// Parse a Turkish-formatted money string ("1.299,90") to a number.
export function parseTrMoney(s) {
  if (typeof s === 'number') return s
  if (!s) return 0
  const num = Number(String(s).replace(/\./g, '').replace(',', '.'))
  return Number.isNaN(num) ? 0 : num
}
