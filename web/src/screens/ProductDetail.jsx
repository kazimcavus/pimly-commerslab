import React, { useEffect, useMemo, useState } from 'react'
import { Button, Field, Input, Select, Banner } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'

const STATUS_OPTIONS = [
  { value: 'active', label: 'Aktif' },
  { value: 'draft', label: 'Taslak' },
  { value: 'archived', label: 'Arşiv' },
]

// Ürün detayı: kodlar (model/stok), renk, görseller, özellikler ve kalem tablosu.
// Ad/durum ile kalem fiyat/stok düzenlenebilir; kalem ve ürün silinebilir.
export function ProductDetail({ productId, onNavigate, onToast }) {
  const [product, setProduct] = useState(null)
  const [error, setError] = useState('')
  const [name, setName] = useState('')
  const [status, setStatus] = useState('active')
  const [savingProduct, setSavingProduct] = useState(false)
  const [itemEdits, setItemEdits] = useState({})   // itemId -> { sku, barcode, price, stock }
  const [savingItem, setSavingItem] = useState(null)

  // Varyant Ekle formu
  const [adding, setAdding] = useState(false)
  const [axisValues, setAxisValues] = useState({})   // variantId -> [{id,label}]
  const [newItem, setNewItem] = useState({ selections: {}, sku: '', barcode: '', price: '', stock: '0' })
  const [savingNew, setSavingNew] = useState(false)

  const load = () => {
    if (!productId) return
    api.getProduct(productId)
      .then((p) => { setProduct(p); setName(p.name || ''); setStatus(p.status || 'active'); setItemEdits({}) })
      .catch((e) => setError(e.message || 'Ürün yüklenemedi'))
  }
  useEffect(() => { load() }, [productId])

  const items = useMemo(() => {
    const list = [...(product?.items || [])]
    const label = (it) => (it.variant_values || []).map((v) => v.name).join(' / ')
    return list.sort((a, b) => label(a).localeCompare(label(b), 'tr', { numeric: true }))
  }, [product])

  if (error) {
    return (
      <div className="page" style={{ maxWidth: 900 }}>
        <Banner tone="danger" title="Ürün açılamadı">{error}</Banner>
        <div style={{ marginTop: 14 }}><Button variant="secondary" onClick={() => onNavigate('products')}>Ürünlere dön</Button></div>
      </div>
    )
  }
  if (!product) return <div className="page"><div className="list-meta">Yükleniyor…</div></div>

  const images = product.images || []
  const dirtyProduct = name !== product.name || status !== product.status

  const saveProduct = async () => {
    setSavingProduct(true)
    try {
      const updated = await api.updateProduct(product.id, {
        category_id: product.category_id,
        name: name.trim(),
        status,
      })
      setProduct(updated)
      onToast?.({ tone: 'success', title: 'Ürün güncellendi' })
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Kaydedilemedi', body: e.message })
    } finally {
      setSavingProduct(false)
    }
  }

  const editOf = (it) => itemEdits[it.id] || {
    sku: it.sku || '', barcode: it.barcode || '', price: String(it.price ?? ''), stock: String(it.stock ?? 0),
  }
  const setEdit = (it, patch) => setItemEdits((cur) => ({ ...cur, [it.id]: { ...editOf(it), ...patch } }))
  const itemDirty = (it) => {
    const e = itemEdits[it.id]
    if (!e) return false
    return Number(e.price) !== Number(it.price)
      || Number(e.stock) !== Number(it.stock)
      || e.sku !== (it.sku || '')
      || e.barcode !== (it.barcode || '')
  }

  const saveItem = async (it) => {
    const e = editOf(it)
    const price = Number(String(e.price).replace(',', '.'))
    const stock = Math.max(0, Math.trunc(Number(e.stock)))
    if (!Number.isFinite(price) || price < 0) { onToast?.({ tone: 'danger', title: 'Geçersiz fiyat' }); return }
    if (!Number.isFinite(stock)) { onToast?.({ tone: 'danger', title: 'Geçersiz stok' }); return }
    if (!e.barcode.trim()) { onToast?.({ tone: 'danger', title: 'Barkod boş olamaz' }); return }
    setSavingItem(it.id)
    try {
      await api.updateItem(it.id, {
        gtin: it.gtin,
        mpn: it.mpn,
        axis_value_entry_id: it.axis_value_entry_id,
        axis_value: it.axis_value,
        price,
        compare_at_price: it.compare_at_price,
        stock,
        // null → koru; sku'da boş metin SKU'yu temizler.
        sku: e.sku !== (it.sku || '') ? e.sku : null,
        barcode: e.barcode !== (it.barcode || '') ? e.barcode.trim() : null,
      })
      onToast?.({ tone: 'success', title: 'Varyant güncellendi' })
      load()
    } catch (e2) {
      onToast?.({ tone: 'danger', title: 'Kaydedilemedi', body: e2.message })
    } finally {
      setSavingItem(null)
    }
  }

  // Varyant Ekle: ürünün eksen(ler)i için katalogdaki değerleri yükle.
  const openAdd = async () => {
    setAdding(true)
    setNewItem({ selections: {}, sku: '', barcode: '', price: '', stock: '0' })
    const types = product.variants || []
    const loaded = {}
    for (const t of types) {
      loaded[t.id] = await api.listVariantValues(t.id).catch(() => [])
    }
    setAxisValues(loaded)
  }

  const saveNewItem = async () => {
    const types = product.variants || []
    const selections = types.map((t) => ({ variant_id: t.id, variant_value_id: newItem.selections[t.id] || '' }))
    if (selections.some((s) => !s.variant_value_id)) { onToast?.({ tone: 'danger', title: 'Her eksen için bir değer seç' }); return }
    if (!newItem.barcode.trim()) { onToast?.({ tone: 'danger', title: 'Barkod gerekli' }); return }
    const price = Number(String(newItem.price).replace(',', '.'))
    const stock = Math.max(0, Math.trunc(Number(newItem.stock || 0)))
    if (!Number.isFinite(price) || price < 0) { onToast?.({ tone: 'danger', title: 'Geçersiz fiyat' }); return }
    setSavingNew(true)
    try {
      await api.createItem(product.id, {
        sku: newItem.sku.trim() || null,
        barcode: newItem.barcode.trim(),
        price,
        stock,
        variant_values: selections,
      })
      onToast?.({ tone: 'success', title: 'Varyant eklendi' })
      setAdding(false)
      load()
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Eklenemedi', body: e.message })
    } finally {
      setSavingNew(false)
    }
  }

  const removeItem = async (it) => {
    const label = (it.variant_values || []).map((v) => v.name).join(' / ') || it.barcode
    if (!confirm(`"${label}" varyantı silinecek. Emin misin?`)) return
    try {
      await api.deleteItem(it.id)
      onToast?.({ tone: 'success', title: 'Varyant silindi' })
      load()
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Silinemedi', body: e.message })
    }
  }

  const removeProduct = async () => {
    if (!confirm(`"${product.name}" ürünü ve tüm varyantları silinecek. Emin misin?`)) return
    try {
      await api.deleteProduct(product.id)
      onToast?.({ tone: 'success', title: 'Ürün silindi' })
      onNavigate('products')
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Silinemedi', body: e.message })
    }
  }

  return (
    <div className="page" style={{ maxWidth: 980 }}>
      <PageHeader
        crumbs={[{ label: 'Ürünler', onClick: () => onNavigate('products') }, { label: product.slicer_value || product.model_code }]}
        eyebrow="Katalog"
        title={product.name}
        sub={<>
          Model kodu <span className="mono pim-td-strong">{product.group_code || product.model_code}</span>
          {' · '}Stok kodu <span className="mono pim-td-strong">{product.model_code}</span>
          {product.slicer_value ? <> · Renk <span className="pim-td-strong">{product.slicer_value}</span></> : null}
        </>}
        actions={<StatusBadge status={product.status} />}
      />

      {/* Görseller */}
      {images.length > 0 && (
        <div className="hstack" style={{ gap: 8, marginBottom: 16, flexWrap: 'wrap' }}>
          {images.slice(0, 8).map((im) => (
            <a key={im.id} href={im.url} target="_blank" rel="noreferrer"
              style={{ width: 72, height: 72, borderRadius: 'var(--radius-md)', overflow: 'hidden', border: '1px solid var(--border-subtle)', flexShrink: 0 }}>
              <img src={im.url} alt={im.alt_text || ''} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
            </a>
          ))}
          {images.length > 8 && <span className="list-meta">+{images.length - 8} görsel</span>}
        </div>
      )}

      {/* Ürün bilgileri */}
      <div className="bnode">
        <div className="bnode__head">
          <span className="ic">{I('package')}</span>
          <div><div className="bnode__title">Ürün bilgileri</div>
            <div className="list-meta">Ad ve durum düzenlenebilir; kodlar import ile eşlendiği için sabittir.</div></div>
          <div className="hstack" style={{ marginLeft: 'auto' }}>
            <Button variant="primary" size="sm" loading={savingProduct} disabled={!dirtyProduct} onClick={saveProduct}>Kaydet</Button>
          </div>
        </div>
        <div className="bnode__body">
          <div className="fieldgrid">
            <Field label="Ürün adı" required>
              <Input value={name} onChange={(e) => setName(e.target.value)} />
            </Field>
            <Field label="Durum">
              <Select value={status} onChange={(e) => setStatus(e.target.value)} options={STATUS_OPTIONS} />
            </Field>
          </div>

          {(product.attribute_values || []).length > 0 && (
            <div style={{ marginTop: 16 }}>
              <div className="list-meta" style={{ fontWeight: 600, marginBottom: 8 }}>Özellikler</div>
              <div className="hstack" style={{ gap: 6, flexWrap: 'wrap' }}>
                {product.attribute_values.map((av) => (
                  <span key={av.id} className="pim-badge" style={{ background: 'var(--surface-subtle)', border: '1px solid var(--border-subtle)' }}>
                    <span className="list-meta">{av.attribute?.name}:</span>&nbsp;{av.name}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Varyant kalemleri */}
      <div className="bnode" style={{ marginTop: 14 }}>
        <div className="bnode__head">
          <span className="ic">{I('layers')}</span>
          <div><div className="bnode__title">Varyantlar</div>
            <div className="list-meta">{items.length} kalem · SKU, barkod, fiyat ve stok satır üzerinde düzenlenir.</div></div>
          <div className="hstack" style={{ marginLeft: 'auto' }}>
            <Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={openAdd} disabled={adding}>Varyant Ekle</Button>
          </div>
        </div>
        <div className="bnode__body" style={{ padding: 0 }}>
          {adding && (
            <div style={{ padding: '14px 16px', borderBottom: '1px solid var(--border-subtle)', background: 'var(--surface-subtle)' }}>
              <div className="list-meta" style={{ fontWeight: 600, marginBottom: 10 }}>Yeni varyant</div>
              <div className="hstack" style={{ gap: 10, flexWrap: 'wrap', alignItems: 'flex-end' }}>
                {(product.variants || []).map((t) => (
                  <Field key={t.id} label={t.name} required>
                    <Select value={newItem.selections[t.id] || ''} placeholder="Seç…"
                      onChange={(e2) => setNewItem((cur) => ({ ...cur, selections: { ...cur.selections, [t.id]: e2.target.value } }))}
                      options={(axisValues[t.id] || []).map((v) => ({ value: v.id, label: v.label || v.name }))} />
                  </Field>
                ))}
                <Field label="Barkod" required>
                  <Input mono value={newItem.barcode} onChange={(e2) => setNewItem((c) => ({ ...c, barcode: e2.target.value }))} style={{ width: 160 }} />
                </Field>
                <Field label="SKU">
                  <Input mono value={newItem.sku} onChange={(e2) => setNewItem((c) => ({ ...c, sku: e2.target.value }))} style={{ width: 150 }} />
                </Field>
                <Field label="Fiyat" required>
                  <Input mono value={newItem.price} onChange={(e2) => setNewItem((c) => ({ ...c, price: e2.target.value }))} style={{ width: 100 }} />
                </Field>
                <Field label="Stok">
                  <Input mono value={newItem.stock} onChange={(e2) => setNewItem((c) => ({ ...c, stock: e2.target.value }))} style={{ width: 80 }} />
                </Field>
                <div className="hstack" style={{ gap: 6 }}>
                  <Button variant="primary" size="sm" loading={savingNew} onClick={saveNewItem}>Ekle</Button>
                  <Button variant="ghost" size="sm" onClick={() => setAdding(false)}>Vazgeç</Button>
                </div>
              </div>
            </div>
          )}
          <div className="pim-table-wrap" style={{ border: 0, borderRadius: 0 }}>
            <table className="pim-table">
              <thead><tr>
                <th>Varyant</th><th>SKU</th><th>Barkod</th><th style={{ width: 120 }}>Fiyat</th><th style={{ width: 90 }}>Stok</th><th style={{ width: 90 }}></th>
              </tr></thead>
              <tbody>
                {items.map((it) => {
                  const e = editOf(it)
                  return (
                    <tr key={it.id}>
                      <td className="pim-td-strong">{(it.variant_values || []).map((v) => v.name).join(' / ') || '—'}</td>
                      <td>
                        <input className="pim-input pim-input--sm mono" style={{ width: 150 }} placeholder="—"
                          value={e.sku} onChange={(ev) => setEdit(it, { sku: ev.target.value })} />
                      </td>
                      <td>
                        <input className="pim-input pim-input--sm mono" style={{ width: 140 }}
                          value={e.barcode} onChange={(ev) => setEdit(it, { barcode: ev.target.value })} />
                      </td>
                      <td>
                        <input className="pim-input pim-input--sm mono" style={{ width: 100 }}
                          value={e.price} onChange={(ev) => setEdit(it, { price: ev.target.value })} />
                      </td>
                      <td>
                        <input className="pim-input pim-input--sm mono" style={{ width: 70 }}
                          value={e.stock} onChange={(ev) => setEdit(it, { stock: ev.target.value })} />
                      </td>
                      <td>
                        <div className="rowact" style={{ display: 'flex', gap: 4, justifyContent: 'flex-end' }}>
                          {itemDirty(it) && (
                            <button className="tb__icon" title="Kaydet" style={{ width: 28, height: 28, color: 'var(--accent, #4f7d5a)' }}
                              disabled={savingItem === it.id} onClick={() => saveItem(it)}>{I('check')}</button>
                          )}
                          <button className="tb__icon" title="Varyantı sil" style={{ width: 28, height: 28 }}
                            onClick={() => removeItem(it)}>{I('trash-2')}</button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
                {items.length === 0 && (
                  <tr><td colSpan={6} className="subtle" style={{ padding: 18 }}>Bu üründe kalem kalmadı.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Tehlikeli işlemler */}
      <div className="between" style={{ marginTop: 18 }}>
        <Button variant="secondary" iconLeft={I('arrow-left')} onClick={() => onNavigate('products')}>Ürünlere dön</Button>
        <Button variant="ghost" iconLeft={I('trash-2')} style={{ color: 'var(--danger-fg)' }} onClick={removeProduct}>Ürünü sil</Button>
      </div>
    </div>
  )
}
