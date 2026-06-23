import React, { useEffect, useState } from 'react'
import { Button, Field, Input, Select, Switch, Banner } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { loadSkuConfig, saveSkuConfig } from '../lib/skuConfig.js'
import { HelpHint } from '../help/Help.jsx'

// Generic, company-agnostic building blocks. Each segment carries a
// user-defined title (Başlık) so firms label them as they like (e.g. a
// "Elle girilir" segment titled "Sezon", a "Sabit metin" titled "Firma kodu").
const SEG_TYPES = [
  { value: 'fixed', label: 'Sabit metin' },
  { value: 'manual', label: 'Elle girilir' },
  { value: 'counter', label: 'Sıralı sayaç (Otomatik)' },
  { value: 'year', label: 'Yıl (Otomatik)' },
  { value: 'color', label: 'Renk (Varyant)' },
  { value: 'size', label: 'Beden / ölçü (Varyant)' },
]

// A sample token for the live preview of each segment.
const sampleToken = (s) => {
  const yy = new Date().getFullYear()
  switch (s.type) {
    case 'fixed': return (s.value || '').toUpperCase() || '··'
    case 'counter': return String(s.start ?? 1).padStart(s.width || 4, '0')
    case 'year': return s.digits === 4 ? String(yy) : String(yy % 100)
    case 'manual': return '...'
    case 'color': return s.source === 'name' ? 'KIRMIZI' : 'R08'
    case 'size': return s.source === 'code' ? 'B36' : '36'
    default: return ''
  }
}
const isVariantSeg = (t) => t === 'color' || t === 'size'

const labelPh = (t) => ({
  fixed: 'Başlık (örn. Firma kodu)', manual: 'Başlık (örn. Sezon)', counter: 'Başlık (örn. Ürün No)',
  year: 'Başlık (örn. Yıl)', color: 'Başlık (örn. Renk)', size: 'Başlık (örn. Beden)',
}[t] || 'Başlık')

export function Settings({ onToast }) {
  const [sku, setSku] = useState({ enabled: false, segments: [] })
  // Barkod serisi (.NET Catalog): { next_value, client_allocation_required, next_preview }.
  const [barcode, setBarcode] = useState({ nextValue: '', clientAllocationRequired: false, nextPreview: '' })
  const [loaded, setLoaded] = useState(false)
  const [savingSku, setSavingSku] = useState(false)
  const [savingBc, setSavingBc] = useState(false)
  const [dragIdx, setDragIdx] = useState(null)
  const [insertAt, setInsertAt] = useState(null)

  useEffect(() => {
    // SKU config frontend-only (localStorage).
    setSku(loadSkuConfig())
    // Barkod serisi .NET'ten; yapılandırılmamışsa (404) varsayılan kalır.
    api.getBarcodeSequence()
      .then((b) => setBarcode({
        nextValue: b.next_value != null ? String(b.next_value) : '',
        clientAllocationRequired: !!b.client_allocation_required,
        nextPreview: b.next_preview || '',
      }))
      .catch(() => {})
      .finally(() => setLoaded(true))
  }, [])

  // --- SKU segment editing ---
  const addSeg = () => setSku((s) => ({ ...s, segments: [...s.segments, { type: 'fixed', value: '' }] }))
  const setSeg = (i, patch) => setSku((s) => ({ ...s, segments: s.segments.map((x, j) => j === i ? { ...x, ...patch } : x) }))
  const removeSeg = (i) => setSku((s) => ({ ...s, segments: s.segments.filter((_, j) => j !== i) }))
  const reorderSeg = (from, insertIndex) => setSku((s) => {
    if (from == null) return s
    const segs = [...s.segments]; const [m] = segs.splice(from, 1)
    let t = insertIndex; if (from < insertIndex) t = insertIndex - 1
    segs.splice(t, 0, m)
    return { ...s, segments: segs }
  })

  const productPreview = sku.segments.filter((s) => !isVariantSeg(s.type)).map(sampleToken).join('')
  const variantPreview = sku.segments.map(sampleToken).join('')

  const saveSku = () => {
    setSavingSku(true)
    try {
      const segments = sku.segments.map((s) => {
        const o = { type: s.type, label: (s.label || '').trim() }
        if (s.type === 'fixed') o.value = (s.value || '').trim()
        if (s.type === 'counter') { o.start = parseInt(s.start, 10) || 1; o.width = parseInt(s.width, 10) || 4 }
        if (s.type === 'year') o.digits = s.digits === 4 ? 4 : 2
        if (isVariantSeg(s.type)) o.source = s.source === 'name' ? 'name' : 'code'
        return o
      })
      saveSkuConfig({ enabled: sku.enabled, segments })
      onToast?.({ tone: 'success', title: 'Ürün kodu ayarı kaydedildi' })
    } catch (e) { onToast?.({ tone: 'danger', title: 'Kaydedilemedi', body: e.message }) }
    finally { setSavingSku(false) }
  }

  const saveBarcode = async () => {
    setSavingBc(true)
    try {
      await api.putBarcodeSequence({
        next_value: parseInt(barcode.nextValue, 10) || 0,
        client_allocation_required: barcode.clientAllocationRequired,
      })
      // Güncel önizlemeyi geri çek.
      const fresh = await api.getBarcodeSequence().catch(() => null)
      if (fresh) setBarcode((b) => ({ ...b, nextValue: String(fresh.next_value), nextPreview: fresh.next_preview || '' }))
      onToast?.({ tone: 'success', title: 'Barkod ayarı kaydedildi' })
    } catch (e) { onToast?.({ tone: 'danger', title: 'Kaydedilemedi', body: e.message }) }
    finally { setSavingBc(false) }
  }

  const startsWith2 = barcode.nextValue.trim().startsWith('2')

  if (!loaded) return <div className="page"><div className="list-meta">Yükleniyor…</div></div>

  return (
    <div className="page" style={{ maxWidth: 860 }}>
      <PageHeader eyebrow="Platform" title="Ayarlar" sub="Ürün kodu (SKU) oluşturucu ve barkod serisi. Kod üretici tarayıcıda saklanır; barkod serisi backend'de tutulur." />

      {/* SKU GENERATOR — frontend-only (localStorage) */}
      <div className="pim-card" style={{ marginBottom: 18 }}>
        <div className="pim-card__header">
          <div className="hstack">{I('wand-2')}<span className="pim-card__title">Ürün Kodu Oluşturucu</span><HelpHint topic="sku-generator" /></div>
          <Switch checked={sku.enabled} onChange={(e) => setSku((s) => ({ ...s, enabled: e.target.checked }))} label={sku.enabled ? 'Açık' : 'Kapalı'} />
        </div>
        <div className="pim-card__body">
          {!sku.enabled ? (
            <div className="subtle">Kapalı. Ürün eklerken kodları elle girersiniz. Otomatik üretim için aç.</div>
          ) : (
            <>
              <div className="list-meta" style={{ marginBottom: 10 }}>Segmentleri sırayla diz. Renk/Beden segmentleri yalnızca varyant SKU'suna eklenir.</div>
              <div className="stack" style={{ gap: 8, marginBottom: 12 }}>
                {sku.segments.map((s, i) => (
                  <div key={i} className="vtype" data-drag={dragIdx === i}
                    data-before={dragIdx !== null && insertAt === i}
                    data-after={dragIdx !== null && insertAt === sku.segments.length && i === sku.segments.length - 1}
                    style={{ padding: 10 }}
                    onDragOver={(e) => { e.preventDefault(); const r = e.currentTarget.getBoundingClientRect(); setInsertAt(e.clientY > r.top + r.height / 2 ? i + 1 : i) }}
                    onDrop={() => { reorderSeg(dragIdx, insertAt); setDragIdx(null); setInsertAt(null) }}>
                    <div className="hstack" style={{ gap: 8 }}>
                      <span className="drag-handle" draggable title="Sürükle"
                        onDragStart={(e) => { setDragIdx(i); const c = e.currentTarget.closest('.vtype'); if (c) e.dataTransfer.setDragImage(c, 20, 20); e.dataTransfer.effectAllowed = 'move' }}
                        onDragEnd={() => { setDragIdx(null); setInsertAt(null) }}>{I('grip-vertical', { size: 16 })}</span>
                      <div style={{ width: 200 }}>
                        <Select value={s.type} onChange={(e) => setSeg(i, { type: e.target.value })} options={SEG_TYPES} />
                      </div>
                      <span className="typechip" style={{ marginLeft: 'auto' }}>{sampleToken(s)}</span>
                      <button className="tb__icon" style={{ width: 26, height: 26 }} title="Kaldır" onClick={() => removeSeg(i)}>{I('trash-2')}</button>
                    </div>
                    <div className="hstack" style={{ gap: 8, marginTop: 8, paddingLeft: 56, flexWrap: 'wrap', alignItems: 'flex-start' }}>
                      {/* Başlık — available for every segment type */}
                      <Field label="Başlık (Opsiyonel)"><Input size="sm" value={s.label || ''} onChange={(e) => setSeg(i, { label: e.target.value })} placeholder={labelPh(s.type)} style={{ width: 180 }} /></Field>
                      {s.type === 'fixed' && <Field label="Değer"><Input size="sm" mono value={s.value || ''} onChange={(e) => setSeg(i, { value: e.target.value })} placeholder="26" style={{ width: 130 }} /></Field>}
                      {s.type === 'counter' && <>
                        <Field label="Başlangıç"><Input size="sm" mono value={s.start ?? ''} onChange={(e) => setSeg(i, { start: e.target.value })} placeholder="1" style={{ width: 110 }} /></Field>
                        <Field label="Hane"><Input size="sm" mono value={s.width ?? ''} onChange={(e) => setSeg(i, { width: e.target.value })} placeholder="4" style={{ width: 70 }} /></Field>
                      </>}
                      {s.type === 'year' && <Field label="Hane"><Select value={String(s.digits || 2)} onChange={(e) => setSeg(i, { digits: parseInt(e.target.value, 10) })} options={[{ value: '2', label: '2 hane (25)' }, { value: '4', label: '4 hane (2025)' }]} /></Field>}
                      {isVariantSeg(s.type) && <Field label="Kaynak"><Select value={s.source || (s.type === 'color' ? 'code' : 'name')} onChange={(e) => setSeg(i, { source: e.target.value })} options={[{ value: 'code', label: 'Kod' }, { value: 'name', label: 'Ad' }]} /></Field>}
                    </div>
                  </div>
                ))}
                {sku.segments.length === 0 && <div className="subtle">Segment yok. Aşağıdan ekle.</div>}
              </div>
              <Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={addSeg}>Segment ekle</Button>

              <div style={{ marginTop: 16, padding: 12, background: 'var(--surface-subtle)', border: '1px solid var(--border-subtle)', borderRadius: 'var(--radius-md)' }}>
                <div className="list-meta">Önizleme</div>
                <div className="hstack" style={{ gap: 16, marginTop: 6 }}>
                  <span>Ürün kodu: <span className="mono pim-td-strong">{productPreview || '—'}</span></span>
                  <span>Varyant SKU: <span className="mono pim-td-strong">{variantPreview || '—'}</span></span>
                </div>
              </div>
            </>
          )}
        </div>
        <div className="pim-card__footer" style={{ display: 'flex', justifyContent: 'flex-end', padding: '12px 16px', borderTop: '1px solid var(--border-subtle)' }}>
          <Button variant="primary" loading={savingSku} onClick={saveSku}>Kaydet</Button>
        </div>
      </div>

      {/* BARCODE — .NET barkod serisi */}
      <div className="pim-card">
        <div className="pim-card__header">
          <div className="hstack">{I('barcode')}<span className="pim-card__title">Barkod (EAN-13)</span><HelpHint topic="barcode" /></div>
        </div>
        <div className="pim-card__body">
          <Field label="Sonraki numara" auto="Her tahsiste +1 artar, 13 haneye tamamlanıp kontrol hanesi eklenir">
            <Input mono value={barcode.nextValue} onChange={(e) => setBarcode((b) => ({ ...b, nextValue: e.target.value.replace(/[^0-9]/g, '') }))} style={{ maxWidth: 220 }} />
          </Field>
          {startsWith2 && <div style={{ marginTop: 8 }}><Banner tone="warning" title="Öneri">2 ile başlayan barkodlar GS1'de mağaza-içi/dahili banttır. Engel değil ama tavsiye etmeyiz.</Banner></div>}
          <div style={{ marginTop: 12 }}>
            <Switch
              checked={barcode.clientAllocationRequired}
              onChange={(e) => setBarcode((b) => ({ ...b, clientAllocationRequired: e.target.checked }))}
              label="İstemci tahsisi zorunlu"
            />
            <div className="list-meta" style={{ marginTop: 6 }}>
              Açıkken barkodlar ürün oluştururken otomatik atanmaz; önceden tahsis edilip elle girilir.
            </div>
          </div>
          {barcode.nextPreview && (
            <div className="list-meta" style={{ marginTop: 12 }}>Sıradaki barkod: <span className="mono pim-td-strong">{barcode.nextPreview}</span></div>
          )}
        </div>
        <div className="pim-card__footer" style={{ display: 'flex', justifyContent: 'flex-end', padding: '12px 16px', borderTop: '1px solid var(--border-subtle)' }}>
          <Button variant="primary" loading={savingBc} onClick={saveBarcode}>Kaydet</Button>
        </div>
      </div>
    </div>
  )
}
