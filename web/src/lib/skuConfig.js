// Ürün kodu (SKU) oluşturucu yapılandırması — frontend-only, localStorage'da tutulur.
// Backend yok; mantık ileride .NET'e taşınacak (bkz. docs/product-code-generator.md).
// Şekil: { enabled: bool, segments: Segment[] }
const KEY = 'pimly_sku_config'

export function loadSkuConfig() {
  try {
    const raw = localStorage.getItem(KEY)
    if (!raw) return { enabled: false, segments: [] }
    const cfg = JSON.parse(raw)
    return { enabled: !!cfg.enabled, segments: Array.isArray(cfg.segments) ? cfg.segments : [] }
  } catch {
    return { enabled: false, segments: [] }
  }
}

export function saveSkuConfig(cfg) {
  localStorage.setItem(KEY, JSON.stringify({ enabled: !!cfg.enabled, segments: cfg.segments || [] }))
}
