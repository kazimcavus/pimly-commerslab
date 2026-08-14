import React, { useMemo, useRef, useState } from 'react'
import { I } from '../icons.jsx'
import { api } from '../../lib/api.js'
import { askConfirm } from '../../lib/confirm.jsx'

// Ürün foto galerisi: yükle / sil / kapak yap / sırala.
// Bağlı mod (productId verili): her işlem anında API'ye gider, sonra onChanged() ile
// ürün yeniden yüklenir. PATCH tüm alanları ezdiği için mevcut alanlar birlikte gönderilir.
export function PhotoGallery({ images = [], productId, onChanged, onToast }) {
  const fileRef = useRef(null)
  const [busy, setBusy] = useState(false)

  const sorted = useMemo(
    () => [...images].sort((a, b) => (a.sort_order ?? 0) - (b.sort_order ?? 0)),
    [images],
  )

  const patch = (img, changes) => api.updateProductImage(img.id, {
    url: img.url,
    sort_order: changes.sort_order ?? img.sort_order ?? 0,
    alt_text: changes.alt_text !== undefined ? changes.alt_text : (img.alt_text ?? null),
    is_primary: changes.is_primary !== undefined ? changes.is_primary : !!img.is_primary,
    variant_value_id: img.variant_value_id ?? null,
  })

  const onFiles = async (e) => {
    const files = Array.from(e.target.files || [])
    e.target.value = ''
    if (files.length === 0) return
    setBusy(true)
    try {
      let order = sorted.length
      for (const f of files) {
        const up = await api.uploadImage(f, 'product')
        await api.addProductImage(productId, {
          url: up.url, sort_order: order, alt_text: null,
          is_primary: order === 0 && sorted.length === 0, variant_value_id: null,
        })
        order += 1
      }
      onToast?.({ tone: 'success', title: files.length > 1 ? `${files.length} görsel eklendi` : 'Görsel eklendi' })
      await onChanged?.()
    } catch (err) {
      onToast?.({ tone: 'danger', title: 'Görsel yüklenemedi', error: err })
    } finally { setBusy(false) }
  }

  const makePrimary = async (img) => {
    if (img.is_primary) return
    setBusy(true)
    try { await patch(img, { is_primary: true }); await onChanged?.() }
    catch (e) { onToast?.({ tone: 'danger', title: 'Kapak yapılamadı', error: e }) }
    finally { setBusy(false) }
  }

  // Komşu görselle sort_order takas ederek sırala (iki PATCH).
  const move = async (idx, dir) => {
    const j = idx + dir
    if (j < 0 || j >= sorted.length) return
    const a = sorted[idx], b = sorted[j]
    setBusy(true)
    try {
      await Promise.all([patch(a, { sort_order: b.sort_order ?? j }), patch(b, { sort_order: a.sort_order ?? idx })])
      await onChanged?.()
    } catch (e) { onToast?.({ tone: 'danger', title: 'Sıralanamadı', error: e }) }
    finally { setBusy(false) }
  }

  const remove = async (img) => {
    const ok = await askConfirm({
      title: 'Görseli sil',
      body: 'Bu görsel üründen kalıcı olarak kaldırılacak.',
      tone: 'danger', confirmLabel: 'Sil',
    })
    if (!ok) return
    setBusy(true)
    try {
      await api.deleteProductImage(img.id)
      onToast?.({ tone: 'success', title: 'Görsel silindi' })
      await onChanged?.()
    } catch (e) { onToast?.({ tone: 'danger', title: 'Silinemedi', error: e }) }
    finally { setBusy(false) }
  }

  return (
    <div className="gallery">
      {sorted.map((img, idx) => (
        <div key={img.id} className="gallery__thumb" data-primary={img.is_primary || undefined}>
          {img.is_primary && <span className="gallery__badge">Kapak</span>}
          <img src={img.url} alt={img.alt_text || ''} loading="lazy" />
          <div className="gallery__actions">
            <button className="gallery__act" title="Kapak yap" disabled={busy || img.is_primary} onClick={() => makePrimary(img)}>{I('star', { size: 14 })}</button>
            <button className="gallery__act" title="Sola al" disabled={busy || idx === 0} onClick={() => move(idx, -1)}>{I('chevron-left', { size: 14 })}</button>
            <button className="gallery__act" title="Sağa al" disabled={busy || idx === sorted.length - 1} onClick={() => move(idx, 1)}>{I('chevron-right', { size: 14 })}</button>
            <button className="gallery__act" title="Sil" disabled={busy} onClick={() => remove(img)}>{I('trash-2', { size: 14 })}</button>
          </div>
        </div>
      ))}
      <button type="button" className="gallery__add" disabled={busy || !productId} onClick={() => fileRef.current?.click()}>
        {I(busy ? 'loader' : 'image-plus', { size: 18 })}
        <span>{busy ? 'Yükleniyor…' : 'Görsel ekle'}</span>
      </button>
      <input ref={fileRef} type="file" accept="image/*" multiple hidden onChange={onFiles} />
    </div>
  )
}
