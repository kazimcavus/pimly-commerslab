// API hatalarını kullanıcı dostu Türkçe metne çevirir (bkz. web/docs/ui-feedback.md).
// Toast'a ham `e.message` geçirme; `onToast({ tone: 'danger', title, error: e })`
// kullan — App, gövdeyi buradan üretir. Banner'lar için doğrudan friendlyError(e).

const STATUS_MESSAGES = {
  400: 'Girilen bilgilerde sorun var. Alanları kontrol edip tekrar dene.',
  401: 'Oturumun süresi dolmuş görünüyor. Lütfen yeniden giriş yap.',
  403: 'Bu işlem için yetkin yok.',
  404: 'Kayıt bulunamadı — silinmiş ya da taşınmış olabilir.',
  409: 'Bu kayıt mevcut bir kayıtla çakışıyor; aynı kod/barkod zaten kullanılıyor olabilir.',
  422: 'Girilen bilgiler doğrulanamadı. Alanları kontrol edip tekrar dene.',
  500: 'Sunucuda beklenmeyen bir sorun oluştu. Birazdan tekrar dene.',
  502: 'Sunucuya şu anda ulaşılamıyor. Birazdan tekrar dene.',
  503: 'Sunucu geçici olarak hizmet veremiyor. Birazdan tekrar dene.',
}

// fetch'in kendi hata metinleri / HTTP statusText'leri — kullanıcıya gösterilmez.
const GENERIC = /^(failed to fetch|load failed|networkerror|bad request|not found|internal server error|conflict|unauthorized|forbidden|unprocessable)/i

// Bilinen backend mesajları → Türkçe. Backend detayları İngilizce gelebilir;
// sık karşılaşılanları burada çevir (yenisini gördükçe ekle).
const KNOWN = [
  { test: /barcode.*already in use|barcode.*(exists|duplicate)/i, tr: 'Bu barkod başka bir varyantta zaten kullanılıyor.' },
  { test: /sku.*already in use|sku.*(exists|duplicate)/i, tr: 'Bu SKU başka bir varyantta zaten kullanılıyor.' },
  { test: /model.?code.*already|already.*model.?code/i, tr: 'Bu ürün kodu zaten kullanılıyor.' },
  { test: /category.*not found/i, tr: 'Kategori bulunamadı — silinmiş olabilir.' },
  { test: /product.*not found/i, tr: 'Ürün bulunamadı — silinmiş olabilir.' },
  { test: /required attribute/i, tr: 'Zorunlu özellikler eksik — Özellikler bölümünü kontrol et.' },
]

export function friendlyError(err) {
  if (!err) return 'Beklenmeyen bir sorun oluştu.'
  if (typeof err === 'string') return err

  // Ağ hatası: sunucuya hiç ulaşılamadı (fetch TypeError fırlatır, status yoktur).
  if (err.status === undefined) {
    return 'Sunucuya ulaşılamıyor. Bağlantını kontrol edip tekrar dene.'
  }

  // RFC7807 alan hataları: ilk birkaçını madde madde göster.
  const fields = err.fields ? Object.values(err.fields).flat().filter(Boolean) : []
  if (fields.length > 0) {
    const shown = fields.slice(0, 3).join(' · ')
    return fields.length > 3 ? `${shown} · (+${fields.length - 3} hata daha)` : shown
  }

  // Backend anlamlı bir detay verdiyse onu tercih et (en spesifik mesaj);
  // bilinen İngilizce mesajlar Türkçeye çevrilir, jenerik statusText ise
  // duruma göre eşlenmiş metin kullanılır.
  const backend = (err.message || '').trim()
  for (const k of KNOWN) if (k.test.test(backend)) return k.tr
  if (backend && !GENERIC.test(backend)) return backend
  return STATUS_MESSAGES[err.status] || 'Beklenmeyen bir sorun oluştu. Birazdan tekrar dene.'
}
