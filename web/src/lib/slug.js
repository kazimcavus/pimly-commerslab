// Bir adı, anahtar/kod biçimine çevirir (Türkçe-duyarlı). Backend'deki
// AttributeKey.FromName ile aynı kuralı izler: küçült, Türkçe karakterleri
// sadeleştir, alfanümerik olmayanları tek '_' ayraca dönüştür.
// Örn. "Abiye Elbise" → "abiye_elbise", "V Yaka Tshirt" → "v_yaka_tshirt".
export function slugify(name) {
  const lower = String(name || '')
    .replace(/İ/g, 'i')
    .toLowerCase()
    .replace(/ı/g, 'i')
    .replace(/ş/g, 's')
    .replace(/ğ/g, 'g')
    .replace(/ü/g, 'u')
    .replace(/ö/g, 'o')
    .replace(/ç/g, 'c')

  let out = ''
  let pending = false
  for (const ch of lower) {
    if (/[a-z0-9]/.test(ch)) {
      if (pending && out.length) out += '_'
      out += ch
      pending = false
    } else if (!pending && out.length) {
      pending = true
    }
  }
  return out
}
