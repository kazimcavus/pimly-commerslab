import React, { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { Button, Drawer, Field, Input, ColorPicker, Switch } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'

// Swatch background: an uploaded image wins over a hex color (image is just a
// nicer preview of the same value).
const swatchBg = (v) => v.image_url
  ? { backgroundImage: `url(${v.image_url})`, backgroundSize: 'cover', backgroundPosition: 'center' }
  : { background: v.color || '#d3ccc1' }

export function Variants({ onToast }) {
  const [types, setTypes] = useState([])
  const [sel, setSel] = useState(null)
  const [values, setValues] = useState([])
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [editing, setEditing] = useState(null) // {id, name, selection_style, _values} or null = create

  const loadTypes = () => api.listVariantTypes().then((t) => { setTypes(t); if (!sel && t.length) setSel(t[0].id); return t }).catch(() => [])
  useEffect(() => { loadTypes() }, [])

  const loadValues = (id) => api.listVariantValues(id).then(setValues).catch(() => setValues([]))
  useEffect(() => { if (!sel) { setValues([]); return } loadValues(sel) }, [sel])

  const active = types.find((t) => t.id === sel)
  const isColor = active?.selection_style === 'color'

  const openCreate = () => { setEditing(null); setDrawerOpen(true) }
  const openEdit = async (type) => {
    try {
      const vals = await api.listVariantValues(type.id)
      setEditing({ ...type, _values: vals })
      setDrawerOpen(true)
    } catch (e) { onToast?.({ tone: 'danger', title: 'Açılamadı', body: e.message }) }
  }

  const submit = async ({ name, style, slicer, rows }) => {
    let typeId
    const body = (r, i) => ({ label: r.label, color: style === 'color' ? r.color : null, image_url: style === 'color' ? (r.image_url || null) : null, key: r.key ? r.key.trim() : null, sort_order: i })
    if (editing) {
      await api.updateVariantType(editing.id, { name, selection_style: style, slicer })
      typeId = editing.id
      const orig = editing._values || []
      for (const o of orig) if (!rows.some((r) => r.id === o.id)) await api.deleteVariantValue(o.id)
      for (let i = 0; i < rows.length; i++) {
        const r = rows[i]
        if (r.id) await api.updateVariantValue(r.id, body(r, i))
        else await api.createVariantValue(typeId, body(r, i))
      }
    } else {
      const t = await api.createVariantType({ name, selection_style: style, slicer, sort_order: types.length })
      typeId = t.id
      for (let i = 0; i < rows.length; i++) await api.createVariantValue(typeId, body(rows[i], i))
    }
    setDrawerOpen(false)
    const wasEdit = !!editing
    setEditing(null)
    await loadTypes()
    setSel(typeId)
    loadValues(typeId)
    onToast?.({ tone: 'success', title: wasEdit ? 'Varyant türü güncellendi' : 'Varyant türü eklendi' })
  }

  return (
    <div className="page">
      <PageHeader eyebrow="Tanımlar" title="Varyantlar" help="variants" sub="Varyant türleri ve değerleri — Renk, Beden, Ölçü. Ürün oluştururken buradan seçilir (en fazla 3 tür)."
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={openCreate}>Varyant türü ekle</Button>} />
      <div className="split">
        <div className="tree">
          {types.map((t) => (
            <div key={t.id} className="tree__node" data-active={sel === t.id} onClick={() => setSel(t.id)}>
              {I('layers')}<span className="tree__name">{t.name}</span>
              {t.slicer && <span className="badge" title="Ürün ayracı — her değer ayrı ürün olur" style={{ marginLeft: 'auto', fontSize: 11, display: 'inline-flex', alignItems: 'center', gap: 4, color: 'var(--text-muted)' }}>{I('scissors', { size: 12 })} Ayraç</span>}
            </div>
          ))}
          {types.length === 0 && <div className="list-meta" style={{ padding: 12 }}>Varyant türü yok.</div>}
        </div>
        {active && (
          <div className="pim-card">
            <div className="pim-card__header">
              <div className="hstack"><span className="pim-card__title">{active.name}</span>{active.key && <span className="typechip" title="Otomatik üretilen tür anahtarı">{active.key}</span>}</div>
              <div className="hstack">
                <Button variant="secondary" size="sm" iconLeft={I('pencil')} onClick={() => openEdit(active)}>Düzenle</Button>
                <button className="tb__icon" style={{ width: 30, height: 30 }} title="Türü sil"
                  onClick={async () => {
                    if (!confirm(`"${active.name}" türünü ve tüm değerlerini sil?`)) return
                    try { await api.deleteVariantType(active.id); setSel(null); loadTypes(); onToast?.({ tone: 'success', title: 'Tür silindi' }) }
                    catch (e) { onToast?.({ tone: 'danger', title: 'Silinemedi', body: e.message }) }
                  }}>{I('trash-2')}</button>
              </div>
            </div>
            <div className="pim-card__body">
              {values.length === 0 ? (
                <div className="subtle" style={{ padding: 14, border: '1px dashed var(--border-default)', borderRadius: 'var(--radius-md)' }}>
                  Bu türün henüz değeri yok. <strong>Düzenle</strong> ile {isColor ? 'renk / görsel' : 'değer'} ekleyin.
                </div>
              ) : (
                <div className="chipset">
                  {values.map((v) => (
                    <span key={v.id} className="sizechip" data-on="true" style={{ display: 'inline-flex', alignItems: 'center', gap: 6, cursor: 'default' }}>
                      {isColor && <span className="swatch-sm" style={swatchBg(v)}></span>}
                      {v.label}
                      {v.key && <span className="typechip" title="Varyant key (SKU'da kullanılır)" style={{ marginLeft: 2 }}>{v.key}</span>}
                    </span>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      <TypeDrawer key={editing ? editing.id : 'new'} open={drawerOpen} editing={editing} onClose={() => { setDrawerOpen(false); setEditing(null) }} onSubmit={submit} onToast={onToast} />
    </div>
  )
}

function TypeDrawer({ open, editing, onClose, onSubmit, onToast }) {
  const keyRef = useRef(1)
  const [name, setName] = useState('')
  const [style, setStyle] = useState('list')
  const [slicer, setSlicer] = useState(false)
  const [rows, setRows] = useState([])
  const [draft, setDraft] = useState('')
  const [editKey, setEditKey] = useState(null)
  const [editLabelKey, setEditLabelKey] = useState(null)
  const [anchor, setAnchor] = useState(null)
  const [tab, setTab] = useState('color')
  const [busy, setBusy] = useState(false)
  const [manualOrder, setManualOrder] = useState(false)
  const [dragIdx, setDragIdx] = useState(null)
  const [insertAt, setInsertAt] = useState(null)

  const azSort = (arr) => [...arr].sort((a, b) => a.label.localeCompare(b.label, 'tr'))

  useEffect(() => {
    if (!open) return
    if (editing) {
      setName(editing.name)
      setStyle(editing.selection_style)
      setSlicer(!!editing.slicer)
      setRows((editing._values || []).map((v) => ({ _k: keyRef.current++, id: v.id, label: v.label, color: v.color || '#d3ccc1', image_url: v.image_url || '', key: v.key || '' })))
      setManualOrder(true) // preserve saved order on edit
    } else {
      setName(''); setStyle('list'); setSlicer(false); setRows([]); setManualOrder(false)
    }
    setDraft(''); setEditKey(null); setEditLabelKey(null); setAnchor(null); setTab('color'); setDragIdx(null)
  }, [open, editing])

  const openEditor = (e, r) => {
    setEditKey(editKey === r._k ? null : r._k)
    setAnchor(e.currentTarget.getBoundingClientRect())
    setTab(r.image_url ? 'image' : 'color')
  }

  const addDraft = () => {
    const label = draft.trim()
    if (!label) return
    setRows((rs) => {
      const next = [...rs, { _k: keyRef.current++, label, color: '#d3ccc1', image_url: '', key: '' }]
      return manualOrder ? next : azSort(next)
    })
    setDraft('')
  }
  const removeRow = (k) => setRows((rs) => rs.filter((r) => r._k !== k))
  const patchRow = (k, patch) => setRows((rs) => rs.map((r) => r._k === k ? { ...r, ...patch } : r))
  const reorderRows = (from, insertIndex) => {
    if (from == null) return
    setRows((rs) => { const n = [...rs]; const [m] = n.splice(from, 1); let t = insertIndex; if (from < insertIndex) t = insertIndex - 1; n.splice(t, 0, m); return n })
    setManualOrder(true)
  }

  const save = async () => {
    if (!name.trim()) { onToast?.({ tone: 'danger', title: 'Ad gerekli' }); return }
    setBusy(true)
    try { await onSubmit({ name: name.trim(), style, slicer, rows }) }
    catch (e) { onToast?.({ tone: 'danger', title: 'Kaydedilemedi', body: e.message }) }
    finally { setBusy(false) }
  }

  const isColor = style === 'color'
  const editRow = rows.find((r) => r._k === editKey)

  return (
    <Drawer open={open} title={editing ? 'Varyant türünü düzenle' : 'Varyant türü oluştur'} busy={busy} onClose={onClose} onConfirm={save}>
      <Field label="Varyant türü adı" required>
        <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Örneğin: Renk, Beden" />
      </Field>

      <Field label="Seçim stili" required>
        <div className="style-seg">
          <div className="style-card" data-active={style === 'list'} onClick={() => setStyle('list')}>{I('list')} Liste</div>
          <div className="style-card" data-active={style === 'color'} onClick={() => setStyle('color')}>{I('palette')} Renk / Görsel</div>
        </div>
      </Field>

      <Field label="Ürün ayracı (slicer)" auto="Açıkken bu türün her değeri ayrı bir ürün/kart olur — örn. her renk ayrı ürün, ortak model kodu. Kapalıyken değerler tek ürünün varyantı kalır. Bir üründe yalnızca bir ayraç tür kullanılabilir.">
        <Switch checked={slicer} onChange={(e) => setSlicer(e.target.checked)}
          label={slicer ? 'Her değer ayrı ürün olur (örn. renk renk)' : 'Varyant olarak kalır (ayırma kapalı)'} />
      </Field>

      <Field label="Değerler" auto="Yaz ve Enter'a bas — istediğin kadar ekleyebilirsin">
        <div className="enter-field">
          <Input
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addDraft() } }}
            placeholder="Örneğin: Kırmızı, S, 36"
          />
          {draft.trim() && <span className="enter-hint">{I('corner-down-left', { size: 13 })} Enter</span>}
        </div>
      </Field>

      {rows.length > 0 && (
        <div className="hstack" style={{ gap: 6, alignItems: 'flex-start', color: 'var(--text-muted)', fontSize: 12.5, lineHeight: 1.45 }}>
          <span style={{ color: 'var(--accent)', flex: '0 0 auto', marginTop: 1 }}>{I('info', { size: 14 })}</span>
          <span>Sağdaki <strong>key</strong> opsiyoneldir — istersen kısa bir kod ver (örn. Kırmızı → <span className="mono">R08</span>). Ürün kodu üreticisi bu key'i kullanır; <strong>boş bırakırsan addan otomatik</strong> üretilir (gri ipucu).</span>
        </div>
      )}

      {rows.length > 0 && (
        <div className="stack" style={{ gap: 8 }}>
          <div className="between">
            <span className="list-meta">{rows.length} değer · sürükleyip sıralayabilirsin</span>
            <Button variant="ghost" size="sm" iconLeft={I('arrow-down-a-z', { size: 14 })} onClick={() => { setRows((rs) => azSort(rs)); setManualOrder(false) }}>A→Z</Button>
          </div>
          {rows.map((r, idx) => (
            <div key={r._k} className="vrow" data-drag={dragIdx === idx}
              data-before={dragIdx !== null && insertAt === idx}
              data-after={dragIdx !== null && insertAt === rows.length && idx === rows.length - 1}
              onDragOver={(e) => { e.preventDefault(); const r2 = e.currentTarget.getBoundingClientRect(); setInsertAt(e.clientY > r2.top + r2.height / 2 ? idx + 1 : idx) }}
              onDrop={() => { reorderRows(dragIdx, insertAt); setDragIdx(null); setInsertAt(null) }}>
              <span className="drag-handle" draggable title="Sürükle"
                onDragStart={(e) => { setDragIdx(idx); const c = e.currentTarget.closest('.vrow'); if (c) e.dataTransfer.setDragImage(c, 20, 20); e.dataTransfer.effectAllowed = 'move' }}
                onDragEnd={() => { setDragIdx(null); setInsertAt(null) }}>{I('grip-vertical', { size: 16 })}</span>
              {isColor && (
                <button className="swatch-btn" style={swatchBg(r)} title="Renk / görsel seç" onClick={(e) => openEditor(e, r)} />
              )}
              {editLabelKey === r._k ? (
                <Input size="sm" autoFocus value={r.label} style={{ flex: 1 }}
                  onChange={(e) => patchRow(r._k, { label: e.target.value })}
                  onKeyDown={(e) => { if (e.key === 'Enter' || e.key === 'Escape') { e.preventDefault(); setEditLabelKey(null) } }}
                  onBlur={() => setEditLabelKey(null)} />
              ) : (
                <span className="vrow__label" onDoubleClick={() => setEditLabelKey(r._k)}>{r.label}</span>
              )}
              <Input size="sm" mono value={r.key || ''} onChange={(e) => patchRow(r._k, { key: e.target.value })}
                placeholder={r.label ? r.label.toLocaleUpperCase('tr') : 'KEY'}
                title="Varyant key — opsiyonel. Doluysa SKU'da kullanılır; boşsa addan otomatik üretilir."
                style={{ width: 100, flex: '0 0 auto' }} />
              <button className="tb__icon" style={{ width: 26, height: 26 }} title="Adı düzenle" onClick={() => setEditLabelKey(editLabelKey === r._k ? null : r._k)}>{I('pencil', { size: 14 })}</button>
              <button className="tb__icon" style={{ width: 26, height: 26 }} title="Kaldır" onClick={() => removeRow(r._k)}>{I('trash-2')}</button>
            </div>
          ))}
        </div>
      )}

      {isColor && editRow && anchor && (
        <ValueEditorPopover
          anchor={anchor}
          tab={tab}
          setTab={setTab}
          row={editRow}
          onColor={(c) => patchRow(editRow._k, { color: c })}
          onImage={(url) => patchRow(editRow._k, { image_url: url })}
          onClearImage={() => patchRow(editRow._k, { image_url: '' })}
          onClose={() => setEditKey(null)}
          onToast={onToast}
        />
      )}
    </Drawer>
  )
}

// Floating Renk / Görsel editor anchored to the clicked swatch.
function ValueEditorPopover({ anchor, tab, setTab, row, onColor, onImage, onClearImage, onClose, onToast }) {
  const W = 300
  let left = anchor.left - W - 12
  if (left < 12) left = anchor.right + 12
  let top = anchor.top
  const maxTop = window.innerHeight - 380
  if (top > maxTop) top = Math.max(12, maxTop)

  return createPortal(
    <>
      <div className="pim-pop__backdrop" onMouseDown={onClose} />
      <div className="pim-pop" style={{ top, left, width: W }} onMouseDown={(e) => e.stopPropagation()}>
        <div className="vtabs">
          <button data-active={tab === 'color'} onClick={() => setTab('color')}>Renk</button>
          <button data-active={tab === 'image'} onClick={() => setTab('image')}>Görsel</button>
        </div>
        {tab === 'color'
          ? <ColorPicker value={row.color} onChange={onColor} />
          : <ImageUpload value={row.image_url} onUpload={onImage} onClear={onClearImage} onToast={onToast} />}
        <div className="list-meta" style={{ marginTop: 10 }}>Key artık satırda — listede her değerin yanından girilir.</div>
      </div>
    </>,
    document.body,
  )
}

function ImageUpload({ value, onUpload, onClear, onToast }) {
  const inputRef = useRef(null)
  const [busy, setBusy] = useState(false)
  const pick = async (file) => {
    if (!file) return
    setBusy(true)
    try { const res = await api.uploadImage(file); onUpload(res.url) }
    catch (e) { onToast?.({ tone: 'danger', title: 'Yüklenemedi', body: e.message }) }
    finally { setBusy(false) }
  }
  return (
    <div>
      <input ref={inputRef} type="file" accept="image/*" hidden onChange={(e) => pick(e.target.files?.[0])} />
      {value ? (
        <div className="vdrop" onClick={() => inputRef.current?.click()}>
          <img src={value} alt="" />
          <div className="list-meta" style={{ marginTop: 8 }}>Değiştirmek için tıkla · <span style={{ color: 'var(--danger, #d7382b)', cursor: 'pointer' }} onClick={(e) => { e.stopPropagation(); onClear() }}>Kaldır</span></div>
        </div>
      ) : (
        <div className="vdrop" onClick={() => inputRef.current?.click()}>
          {I('image')}
          <div style={{ marginTop: 6 }}>{busy ? 'Yükleniyor…' : 'JPG / PNG / WEBP · + Resim Ekle'}</div>
        </div>
      )}
    </div>
  )
}
