// Ürün kodu (SKU) oluşturucu yapılandırması — .NET Catalog API.
// Şekil: { enabled, segments[], counterNextValue? }
// Eski localStorage verisi ilk yüklemede sunucuya taşınır.
import { api } from './api.js'

const KEY = 'pimly_sku_config'
const MIGRATED_KEY = 'pimly_sku_config_migrated'

function loadLocalSkuConfig() {
  try {
    const raw = localStorage.getItem(KEY)
    if (!raw) return { enabled: false, segments: [] }
    const cfg = JSON.parse(raw)
    return { enabled: !!cfg.enabled, segments: Array.isArray(cfg.segments) ? cfg.segments : [] }
  } catch {
    return { enabled: false, segments: [] }
  }
}

function mapFromApi(cfg) {
  if (!cfg) return { enabled: false, segments: [] }
  return {
    enabled: !!cfg.enabled,
    segments: Array.isArray(cfg.segments) ? cfg.segments : [],
    counterNextValue: cfg.counter_next_value ?? null,
  }
}

async function migrateLocalIfNeeded() {
  if (localStorage.getItem(MIGRATED_KEY)) return
  const local = loadLocalSkuConfig()
  if (!local.enabled && local.segments.length === 0) {
    localStorage.setItem(MIGRATED_KEY, '1')
    return
  }
  try {
    await api.putSkuConfig({ enabled: local.enabled, segments: local.segments })
    localStorage.setItem(MIGRATED_KEY, '1')
    localStorage.removeItem(KEY)
  } catch {
    // Sunucu yoksa localStorage'da kalır.
  }
}

export async function loadSkuConfig() {
  await migrateLocalIfNeeded()
  try {
    return mapFromApi(await api.getSkuConfig())
  } catch {
    return loadLocalSkuConfig()
  }
}

export async function saveSkuConfig(cfg) {
  const body = {
    enabled: !!cfg.enabled,
    segments: cfg.segments || [],
  }
  if (cfg.counterNextValue != null && cfg.counterNextValue !== '') {
    body.counter_next_value = parseInt(cfg.counterNextValue, 10) || undefined
  }
  const saved = await api.putSkuConfig(body)
  return mapFromApi(saved)
}

// Senkron önizleme / test için (tercihen loadSkuConfig kullanın).
export function loadSkuConfigSync() {
  return loadLocalSkuConfig()
}
