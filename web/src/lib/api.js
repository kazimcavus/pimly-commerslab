// pimly API client. Backend .NET (Pimly.Api). Uçlar modül başına versiyonlu
// prefix altında: Identity → /api/v1/identity, Catalog → /api/v1/catalog.
// Dev'de Vite, /api isteklerini .NET sunucusuna (:7000) proxy'ler (bkz. vite.config.js),
// böylece tarayıcı same-origin kalır.
const BASE = import.meta.env.VITE_API_BASE || ''
const IDENTITY = '/api/v1/identity'
const CATALOG = '/api/v1/catalog'
const CHANNELS = '/api/v1/channels'
const MEDIA = '/api/v1/media'

let token = localStorage.getItem('pimly_token') || ''

export function setToken(t) {
  token = t || ''
  if (t) localStorage.setItem('pimly_token', t)
  else localStorage.removeItem('pimly_token')
}
export function getToken() {
  return token
}

async function req(method, path, { body, form } = {}) {
  const headers = {}
  if (token) headers['Authorization'] = 'Bearer ' + token
  let payload
  if (form) {
    payload = form // FormData — browser sets multipart boundary
  } else if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
    payload = JSON.stringify(body)
  }
  const res = await fetch(BASE + path, { method, headers, body: payload })
  if (res.status === 204) return null
  const text = await res.text()
  const data = text ? safeParse(text) : null
  if (!res.ok) {
    const err = new Error(errorMessage(data) || res.statusText)
    err.status = res.status
    err.fields = data && data.errors // RFC7807 ProblemDetails alan hataları
    throw err
  }
  return data
}

// .NET RFC7807 ProblemDetails: { status, title, detail, errors }.
function errorMessage(data) {
  if (!data) return ''
  return data.detail || data.title || ''
}

function safeParse(t) {
  try {
    return JSON.parse(t)
  } catch {
    return null
  }
}

// Backend sayfa boyutu üst sınırı (SharedKernel/Pagination.MaxPageSize).
const MAX_PAGE_SIZE = 100

function withPage(path, page, pageSize) {
  const sep = path.includes('?') ? '&' : '?'
  return `${path}${sep}page=${page}&page_size=${pageSize}`
}

// .NET liste uçları sayfalı zarf döndürür ({ items, page, page_size, total_count, ... });
// frontend'de sayfalama UI'ı yok — hep tam liste beklenir. Zarfı açıp TÜM sayfaları
// dolaşarak birleştir; endpoint sayfasız düz dizi dönerse olduğu gibi bırak.
async function reqList(path) {
  const first = await req('GET', withPage(path, 1, MAX_PAGE_SIZE))
  if (Array.isArray(first)) return first // sayfasız uç
  if (!first || !Array.isArray(first.items)) return []
  const items = first.items.slice()
  const total = first.total_count ?? items.length
  const pageSize = first.page_size || MAX_PAGE_SIZE
  let page = first.page || 1
  // total'a ulaşana kadar sonraki sayfaları çek; boş sayfa gelirse güvenlik için dur.
  while (items.length < total) {
    page += 1
    const next = await req('GET', withPage(path, page, pageSize))
    const chunk = Array.isArray(next?.items) ? next.items : []
    if (chunk.length === 0) break
    items.push(...chunk)
  }
  return items
}

export const api = {
  // --- auth (Identity modülü) ---
  // .NET LoginResult: { token, expires_at, user: { id, email, name }, tenant: { id, name } }
  login: (email, password) => req('POST', `${IDENTITY}/login`, { body: { email, password } }),
  // Kayıt: yeni kullanıcı + tenant oluşturur, otomatik login (LoginResult) döner.
  register: (b) => req('POST', `${IDENTITY}/register`, { body: b }),
  // MeDto: { user: {...}, tenant: {...} }
  me: () => req('GET', `${IDENTITY}/me`),

  // --- pazaryerleri (Channels modülü) ---
  listMarketplaces: () => reqList(`${CHANNELS}/marketplaces`),
  getConnection: (code) => req('GET', `${CHANNELS}/marketplaces/${code}/connection`),
  putConnection: (code, b) => req('PUT', `${CHANNELS}/marketplaces/${code}/connection`, { body: b }),
  getTaxonomyStatus: (code) => req('GET', `${CHANNELS}/marketplaces/${code}/taxonomy/status`),
  enqueueTaxonomySync: (code) => req('POST', `${CHANNELS}/marketplaces/${code}/taxonomy/sync-runs`),
  // Ürün import'u: 202 + run döner; ilerleme getImportRun ile izlenir.
  // Kategori ↔ pazaryeri kategorisi eşlemesi (404 = eşleme yok).
  getCategoryMapping: (code, categoryId) => req('GET', `${CHANNELS}/marketplaces/${code}/category-mappings/${categoryId}`),
  startImport: (code) => req('POST', `${CHANNELS}/marketplaces/${code}/imports`),
  getImportRun: (code, runId) => req('GET', `${CHANNELS}/marketplaces/${code}/imports/${runId}`),
  listImportRuns: (code, limit = 20) => req('GET', `${CHANNELS}/marketplaces/${code}/imports?limit=${limit}`),

  // --- fiyat tanımları & kalem fiyatları (Catalog modülü) ---
  // Kullanıcı tanımlı fiyat alanları (örn. "TY Satış"); her kaleme tanım başına bir tutar girilir.
  listPriceDefinitions: () => reqList(`${CATALOG}/price-definitions`),
  createPriceDefinition: (b) => req('POST', `${CATALOG}/price-definitions`, { body: b }),
  updatePriceDefinition: (id, b) => req('PATCH', `${CATALOG}/price-definitions/${id}`, { body: b }),
  deletePriceDefinition: (id) => req('DELETE', `${CATALOG}/price-definitions/${id}`),
  // ItemPriceDto[]: { id, product_item_id, price_definition_id, definition_name, amount, currency, updated_at }
  listItemPrices: (itemId) => req('GET', `${CATALOG}/items/${itemId}/prices`),
  putItemPrice: (itemId, defId, b) => req('PUT', `${CATALOG}/items/${itemId}/prices/${defId}`, { body: b }),
  deleteItemPrice: (itemId, defId) => req('DELETE', `${CATALOG}/items/${itemId}/prices/${defId}`),

  // --- categories (Catalog modülü) ---
  listCategories: () => reqList(`${CATALOG}/categories`),
  createCategory: (b) => req('POST', `${CATALOG}/categories`, { body: b }),
  updateCategory: (id, b) => req('PATCH', `${CATALOG}/categories/${id}`, { body: b }),
  deleteCategory: (id) => req('DELETE', `${CATALOG}/categories/${id}`),
  listCategoryAttributes: (id) => reqList(`${CATALOG}/categories/${id}/attributes`),
  assignCategoryAttribute: (id, b) => req('POST', `${CATALOG}/categories/${id}/attributes`, { body: b }),
  updateCategoryAttribute: (id, b) => req('PATCH', `${CATALOG}/category-attributes/${id}`, { body: b }),
  deleteCategoryAttribute: (id) => req('DELETE', `${CATALOG}/category-attributes/${id}`),

  // --- brands (Catalog modülü) ---
  listBrands: () => reqList(`${CATALOG}/brands`),
  createBrand: (b) => req('POST', `${CATALOG}/brands`, { body: b }),
  updateBrand: (id, b) => req('PATCH', `${CATALOG}/brands/${id}`, { body: b }),
  deleteBrand: (id) => req('DELETE', `${CATALOG}/brands/${id}`),

  // --- attributes (Catalog modülü) ---
  listAttributes: () => reqList(`${CATALOG}/attributes`),
  createAttribute: (b) => req('POST', `${CATALOG}/attributes`, { body: b }),
  updateAttribute: (id, b) => req('PATCH', `${CATALOG}/attributes/${id}`, { body: b }),
  deleteAttribute: (id) => req('DELETE', `${CATALOG}/attributes/${id}`),
  listAttributeValues: (id) => reqList(`${CATALOG}/attributes/${id}/values`),
  createAttributeValue: (id, b) => req('POST', `${CATALOG}/attributes/${id}/values`, { body: b }),
  updateAttributeValue: (id, b) => req('PATCH', `${CATALOG}/attribute-values/${id}`, { body: b }),
  deleteAttributeValue: (id) => req('DELETE', `${CATALOG}/attribute-values/${id}`),

  // --- variant types & values (Catalog: option axes — Renk, Beden, Ölçü) ---
  // .NET'te "variant type" route'u /variants altında yaşar.
  listVariantTypes: () => reqList(`${CATALOG}/variants`),
  createVariantType: (b) => req('POST', `${CATALOG}/variants`, { body: b }),
  updateVariantType: (id, b) => req('PATCH', `${CATALOG}/variants/${id}`, { body: b }),
  deleteVariantType: (id) => req('DELETE', `${CATALOG}/variants/${id}`),
  listVariantValues: (id) => reqList(`${CATALOG}/variants/${id}/values`),
  createVariantValue: (id, b) => req('POST', `${CATALOG}/variants/${id}/values`, { body: b }),
  updateVariantValue: (id, b) => req('PATCH', `${CATALOG}/variant-values/${id}`, { body: b }),
  deleteVariantValue: (id) => req('DELETE', `${CATALOG}/variant-values/${id}`),

  // --- products (Catalog modülü) ---
  productsBatch: (b) => req('POST', `${CATALOG}/products:batch`, { body: b }),
  listProducts: () => reqList(`${CATALOG}/products`),
  getProduct: (id) => req('GET', `${CATALOG}/products/${id}`),
  updateProduct: (id, b) => req('PATCH', `${CATALOG}/products/${id}`, { body: b }),
  deleteProduct: (id) => req('DELETE', `${CATALOG}/products/${id}`),
  // .NET "item" — ürün altı SKU satırı.
  getItem: (id) => req('GET', `${CATALOG}/items/${id}`),
  createItem: (productId, b) => req('POST', `${CATALOG}/products/${productId}/items`, { body: b }),
  updateItem: (id, b) => req('PATCH', `${CATALOG}/items/${id}`, { body: b }),
  deleteItem: (id) => req('DELETE', `${CATALOG}/items/${id}`),

  // --- barkod serisi (Catalog modülü) ---
  // BarcodeSequenceDto: { next_value, client_allocation_required, next_preview }.
  // Seri yapılandırılmamışsa GET 404 döner; çağıran tarafta yakalanır.
  getBarcodeSequence: () => req('GET', `${CATALOG}/barcode-sequence`),
  putBarcodeSequence: (b) => req('PUT', `${CATALOG}/barcode-sequence`, { body: b }),
  allocateBarcodes: (count) => req('POST', `${CATALOG}/barcodes:allocate`, { body: { count } }),
  listBarcodeAllocations: () => reqList(`${CATALOG}/barcode-allocations`),

  // --- SKU oluşturucu (Catalog modülü) ---
  getSkuConfig: () => req('GET', `${CATALOG}/sku-config`),
  putSkuConfig: (b) => req('PUT', `${CATALOG}/sku-config`, { body: b }),

  // --- Katalog ayarları (tenant tercihleri) ---
  getCatalogSettings: () => req('GET', `${CATALOG}/settings`),
  putCatalogSettings: (b) => req('PUT', `${CATALOG}/settings`, { body: b }),

  // --- Medya + ürün görselleri ---
  // Dosyayı medya deposuna yükler → { url, content_type, size_bytes }. url = /media/{tenant}/…
  uploadImage: (file, purpose = 'product') => {
    const fd = new FormData()
    fd.append('file', file)
    return req('POST', `${MEDIA}/uploads?purpose=${purpose}`, { form: fd })
  },
  // Yüklenmiş bir /media url'ini ürüne görsel olarak ekler.
  addProductImage: (productId, b) => req('POST', `${CATALOG}/products/${productId}/images`, { body: b }),
  updateProductImage: (imageId, b) => req('PATCH', `${CATALOG}/product-images/${imageId}`, { body: b }),
  deleteProductImage: (imageId) => req('DELETE', `${CATALOG}/product-images/${imageId}`),
}
