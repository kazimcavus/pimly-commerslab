// pimly API client. In dev, BASE defaults to "/api" which Vite proxies to the
// Go backend on :8080 (see vite.config.js), keeping the browser same-origin.
const BASE = import.meta.env.VITE_API_BASE || '/api'

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
    const err = new Error((data && data.error && data.error.message) || res.statusText)
    err.code = (data && data.error && data.error.code) || 'error'
    err.status = res.status
    throw err
  }
  return data
}

function safeParse(t) {
  try {
    return JSON.parse(t)
  } catch {
    return null
  }
}

export const api = {
  // --- auth ---
  login: (email, password, tenant_slug) => req('POST', '/login', { body: { email, password, tenant_slug } }),

  // --- settings (sku generator, barcode config) ---
  getSettings: () => req('GET', '/settings'),
  putSetting: (key, value) => req('PUT', `/settings/${key}`, { body: value }),
  me: () => req('GET', '/me'),

  // --- categories ---
  listCategories: () => req('GET', '/categories'),
  createCategory: (b) => req('POST', '/categories', { body: b }),
  updateCategory: (id, b) => req('PATCH', `/categories/${id}`, { body: b }),
  deleteCategory: (id) => req('DELETE', `/categories/${id}`),
  listCategoryAttributes: (id) => req('GET', `/categories/${id}/attributes`),
  assignCategoryAttribute: (id, b) => req('POST', `/categories/${id}/attributes`, { body: b }),
  deleteCategoryAttribute: (id) => req('DELETE', `/category-attributes/${id}`),

  // --- attributes ---
  listAttributes: () => req('GET', '/attributes'),
  createAttribute: (b) => req('POST', '/attributes', { body: b }),
  updateAttribute: (id, b) => req('PATCH', `/attributes/${id}`, { body: b }),
  deleteAttribute: (id) => req('DELETE', `/attributes/${id}`),

  // --- metaobjects ---
  listMetaDefs: () => req('GET', '/metaobject-definitions'),
  createMetaDef: (b) => req('POST', '/metaobject-definitions', { body: b }),
  deleteMetaDef: (id) => req('DELETE', `/metaobject-definitions/${id}`),
  listMetaFields: (id) => req('GET', `/metaobject-definitions/${id}/fields`),
  createMetaField: (id, b) => req('POST', `/metaobject-definitions/${id}/fields`, { body: b }),
  deleteMetaField: (id) => req('DELETE', `/metaobject-fields/${id}`),
  listMetaEntries: (id) => req('GET', `/metaobject-definitions/${id}/entries`),
  createMetaEntry: (id, values) => req('POST', `/metaobject-definitions/${id}/entries`, { body: { values } }),
  deleteMetaEntry: (id) => req('DELETE', `/metaobject-entries/${id}`),

  // --- variant types & values (option axes: Renk, Beden, Ölçü) ---
  listVariantTypes: () => req('GET', '/variant-types'),
  createVariantType: (b) => req('POST', '/variant-types', { body: b }),
  updateVariantType: (id, b) => req('PATCH', `/variant-types/${id}`, { body: b }),
  deleteVariantType: (id) => req('DELETE', `/variant-types/${id}`),
  listVariantValues: (id) => req('GET', `/variant-types/${id}/values`),
  createVariantValue: (id, b) => req('POST', `/variant-types/${id}/values`, { body: b }),
  updateVariantValue: (id, b) => req('PATCH', `/variant-values/${id}`, { body: b }),
  deleteVariantValue: (id) => req('DELETE', `/variant-values/${id}`),

  // --- products ---
  productsBatch: (b) => req('POST', '/products:batch', { body: b }),
  listGroups: () => req('GET', '/groups'),
  getGroup: (id) => req('GET', `/groups/${id}`),
  updateGroup: (id, b) => req('PATCH', `/groups/${id}`, { body: b }),
  deleteGroup: (id) => req('DELETE', `/groups/${id}`),
  getProduct: (id) => req('GET', `/products/${id}`),
  getVariant: (id) => req('GET', `/variants/${id}`),

  // --- media ---
  listMedia: (productId) => req('GET', `/products/${productId}/media`),
  uploadMedia: (productId, file, altText) => {
    const fd = new FormData()
    fd.append('file', file)
    if (altText) fd.append('alt_text', altText)
    return req('POST', `/products/${productId}/media`, { form: fd })
  },
  bulkUploadMedia: (files) => {
    const fd = new FormData()
    for (const f of files) fd.append('files', f, f.name)
    return req('POST', '/media:bulk', { form: fd })
  },
  deleteMedia: (id) => req('DELETE', `/media/${id}`),
  uploadImage: (file) => {
    const fd = new FormData()
    fd.append('file', file)
    return req('POST', '/uploads', { form: fd })
  },

  // --- admin (X-Admin-Token) ---
  adminListApplications: (status) =>
    req('GET', '/admin/applications' + (status ? `?status=${status}` : ''), { admin: true }),
  adminCreateApplication: (b) => req('POST', '/admin/applications', { body: b, admin: true }),
  adminApprove: (id) => req('POST', `/admin/applications/${id}/approve`, { admin: true }),
  adminListTenants: () => req('GET', '/admin/tenants', { admin: true }),
  adminSetModule: (tenantId, module, enabled) =>
    req('POST', `/admin/tenants/${tenantId}/modules/${module}`, { body: { enabled }, admin: true }),
}
