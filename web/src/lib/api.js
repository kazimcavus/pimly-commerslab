// pimly API client. Backend artık .NET (Pimly.Api). Uçlar modül başına
// versiyonlu prefix altında: Identity → /api/v1/identity, Catalog → /api/v1/catalog.
// Dev'de Vite, /api isteklerini .NET sunucusuna (:7000) proxy'ler (bkz. vite.config.js),
// böylece tarayıcı same-origin kalır.
const BASE = import.meta.env.VITE_API_BASE || ''
const IDENTITY = '/api/v1/identity'
const CATALOG = '/api/v1/catalog'

let token = localStorage.getItem('pimly_token') || ''
let adminToken = localStorage.getItem('pimly_admin_token') || ''

export function setToken(t) {
  token = t || ''
  if (t) localStorage.setItem('pimly_token', t)
  else localStorage.removeItem('pimly_token')
}
export function getToken() {
  return token
}
export function setAdminToken(t) {
  adminToken = t || ''
  if (t) localStorage.setItem('pimly_admin_token', t)
  else localStorage.removeItem('pimly_admin_token')
}
export function getAdminToken() {
  return adminToken
}

async function req(method, path, { body, form, admin } = {}) {
  const headers = {}
  if (token) headers['Authorization'] = 'Bearer ' + token
  if (admin && adminToken) headers['X-Admin-Token'] = adminToken
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
    err.code = errorCode(data)
    err.status = res.status
    err.fields = data && data.errors // RFC7807 ProblemDetails alan hataları
    throw err
  }
  return data
}

// .NET RFC7807 ProblemDetails: { status, title, detail, errors }. Eski Go formatı
// { error: { code, message } } için de geriye dönük destek.
function errorMessage(data) {
  if (!data) return ''
  return (data.error && data.error.message) || data.detail || data.title || ''
}
function errorCode(data) {
  if (!data) return 'error'
  return (data.error && data.error.code) || data.title || 'error'
}

function safeParse(t) {
  try {
    return JSON.parse(t)
  } catch {
    return null
  }
}

// .NET liste uçları sayfalı zarf döndürür ({ items, page, total_count, ... });
// frontend düz dizi bekliyor. Zarfı açıp diziyi döndür, değilse olduğu gibi bırak.
async function reqList(path) {
  const data = await req('GET', path)
  if (data && Array.isArray(data.items)) return data.items
  return Array.isArray(data) ? data : []
}

// Henüz .NET backend'e taşınmamış uçlar için açık hata döndüren yer tutucu.
function pending(name) {
  return Promise.reject(
    Object.assign(new Error(`"${name}" özelliği henüz .NET backend'e taşınmadı`), {
      code: 'not_migrated',
      status: 501,
    }),
  )
}

export const api = {
  // --- auth (Identity modülü) ---
  // .NET LoginResult: { token, expiresAt, user: { id, email, name } }
  login: (email, password) => req('POST', `${IDENTITY}/login`, { body: { email, password } }),
  me: () => req('GET', `${IDENTITY}/me`),

  // --- categories (Catalog modülü) ---
  listCategories: () => reqList(`${CATALOG}/categories`),
  createCategory: (b) => req('POST', `${CATALOG}/categories`, { body: b }),
  updateCategory: (id, b) => req('PATCH', `${CATALOG}/categories/${id}`, { body: b }),
  deleteCategory: (id) => req('DELETE', `${CATALOG}/categories/${id}`),
  listCategoryAttributes: (id) => reqList(`${CATALOG}/categories/${id}/attributes`),
  assignCategoryAttribute: (id, b) => req('POST', `${CATALOG}/categories/${id}/attributes`, { body: b }),
  updateCategoryAttribute: (id, b) => req('PATCH', `${CATALOG}/category-attributes/${id}`, { body: b }),
  deleteCategoryAttribute: (id) => req('DELETE', `${CATALOG}/category-attributes/${id}`),

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
  // Go'daki "variant" ≈ .NET'teki "item" (ürün altı SKU satırı).
  getItem: (id) => req('GET', `${CATALOG}/items/${id}`),
  updateItem: (id, b) => req('PATCH', `${CATALOG}/items/${id}`, { body: b }),
  deleteItem: (id) => req('DELETE', `${CATALOG}/items/${id}`),
  getVariant: (id) => req('GET', `${CATALOG}/items/${id}`), // geriye dönük ad

  // --- henüz .NET'e taşınmamış uçlar (adım adım eklenecek) ---
  // settings
  getSettings: () => pending('Ayarlar'),
  putSetting: () => pending('Ayarlar'),
  // metaobjects
  listMetaDefs: () => pending('Metaobjeler'),
  createMetaDef: () => pending('Metaobjeler'),
  deleteMetaDef: () => pending('Metaobjeler'),
  listMetaFields: () => pending('Metaobjeler'),
  createMetaField: () => pending('Metaobjeler'),
  deleteMetaField: () => pending('Metaobjeler'),
  listMetaEntries: () => pending('Metaobjeler'),
  createMetaEntry: () => pending('Metaobjeler'),
  deleteMetaEntry: () => pending('Metaobjeler'),
  // groups (Go ürün gruplama katmanı — .NET'te products/items modeli geliyor)
  listGroups: () => pending('Ürün grupları'),
  getGroup: () => pending('Ürün grupları'),
  updateGroup: () => pending('Ürün grupları'),
  deleteGroup: () => pending('Ürün grupları'),
  // media
  listMedia: () => pending('Medya'),
  uploadMedia: () => pending('Medya'),
  bulkUploadMedia: () => pending('Medya'),
  deleteMedia: () => pending('Medya'),
  uploadImage: () => pending('Medya'),
  // admin
  adminListApplications: () => pending('Yönetim'),
  adminCreateApplication: () => pending('Yönetim'),
  adminApprove: () => pending('Yönetim'),
  adminListTenants: () => pending('Yönetim'),
  adminSetModule: () => pending('Yönetim'),
}
