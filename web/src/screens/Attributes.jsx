import React, { useEffect, useRef, useState } from 'react'
import { Button, Drawer, Field, Input } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { askConfirm } from '../lib/confirm.jsx'

// Özellikler (.NET Catalog): bir özellik = ad + otomatik anahtar (key). Her özelliğin
// değerleri ayrı uçtan yönetilir (örn. Kumaş → Pamuk, Polyester). Kategorilere atanıp
// ürün açarken seçilir.
export function Attributes({ onToast }) {
  const [attrs, setAttrs] = useState([])
  const [sel, setSel] = useState(null)
  const [values, setValues] = useState([])
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [editing, setEditing] = useState(null) // {id,name,key,_values} or null = create

  const loadAttrs = () => api.listAttributes().then((a) => { setAttrs(a); if (!sel && a.length) setSel(a[0].id); return a }).catch(() => [])
  useEffect(() => { loadAttrs() }, [])

  const loadValues = (id) => api.listAttributeValues(id).then(setValues).catch(() => setValues([]))
  useEffect(() => { if (!sel) { setValues([]); return } loadValues(sel) }, [sel])

  const active = attrs.find((a) => a.id === sel)

  const openCreate = () => { setEditing(null); setDrawerOpen(true) }
  const openEdit = async (attr) => {
    try {
      const vals = await api.listAttributeValues(attr.id)
      setEditing({ ...attr, _values: vals })
      setDrawerOpen(true)
    } catch (e) { onToast?.({ tone: 'danger', title: 'Açılamadı', error: e }) }
  }

  const submit = async ({ name, rows }) => {
    let attrId
    if (editing) {
      // Düzenlemede kaldırılan değerler kayıtta silinir — önce kullanıcıya sor.
      const orig = editing._values || []
      const removed = orig.filter((o) => !rows.some((r) => r.id === o.id))
      if (removed.length > 0) {
        const ok = await askConfirm({
          title: removed.length === 1 ? 'Değer silinecek' : `${removed.length} değer silinecek`,
          body: `${removed.map((v) => `"${v.name}"`).join(', ')} bu özellikten kalıcı olarak silinecek; ürünlerdeki seçimleri etkilenebilir.`,
          tone: 'danger', confirmLabel: 'Sil ve kaydet',
        })
        if (!ok) return
      }
      await api.updateAttribute(editing.id, { name })
      attrId = editing.id
      for (const o of removed) await api.deleteAttributeValue(o.id)
      for (const r of rows) {
        if (r.id) { const o = orig.find((x) => x.id === r.id); if (o && o.name !== r.name) await api.updateAttributeValue(r.id, { name: r.name }) }
        else await api.createAttributeValue(attrId, { name: r.name })
      }
    } else {
      const a = await api.createAttribute({ name })
      attrId = a.id
      for (const r of rows) await api.createAttributeValue(attrId, { name: r.name })
    }
    setDrawerOpen(false)
    const wasEdit = !!editing
    setEditing(null)
    await loadAttrs()
    setSel(attrId)
    loadValues(attrId)
    onToast?.({ tone: 'success', title: wasEdit ? 'Özellik güncellendi' : 'Özellik eklendi' })
  }

  return (
    <div className="page">
      <PageHeader eyebrow="Tanımlar" title="Özellikler" help="attributes"
        sub="Ürün özellikleri ve değerleri — örn. Kumaş → Pamuk, Polyester. Kategorilere atanır, ürün açarken seçilir."
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={openCreate}>Özellik ekle</Button>} />
      <div className="split">
        <div className="tree">
          {attrs.map((a) => (
            <div key={a.id} className="tree__node" data-active={sel === a.id} onClick={() => setSel(a.id)}>
              {I('tag')}<span className="tree__name">{a.name}</span>
            </div>
          ))}
          {attrs.length === 0 && <div className="list-meta" style={{ padding: 12 }}>Henüz özellik yok. Sağ üstten ekleyin.</div>}
        </div>
        {active && (
          <div className="pim-card">
            <div className="pim-card__header">
              <div className="hstack" style={{ gap: 8 }}>
                <span className="pim-card__title">{active.name}</span>
                <span className="list-meta pim-td-mono" title="Otomatik üretilen anahtar">{active.key}</span>
              </div>
              <div className="hstack">
                <Button variant="secondary" size="sm" iconLeft={I('pencil')} onClick={() => openEdit(active)}>Düzenle</Button>
                <button className="tb__icon" style={{ width: 30, height: 30 }} title="Özelliği sil"
                  onClick={async () => {
                    const ok = await askConfirm({
                      title: 'Özelliği sil',
                      body: `"${active.name}" özelliği ve tüm değerleri kalıcı olarak silinecek.`,
                      tone: 'danger', confirmLabel: 'Sil',
                    })
                    if (!ok) return
                    try { await api.deleteAttribute(active.id); setSel(null); loadAttrs(); onToast?.({ tone: 'success', title: 'Özellik silindi' }) }
                    catch (e) { onToast?.({ tone: 'danger', title: 'Silinemedi', error: e }) }
                  }}>{I('trash-2')}</button>
              </div>
            </div>
            <div className="pim-card__body">
              {values.length === 0 ? (
                <div className="subtle" style={{ padding: 14, border: '1px dashed var(--border-default)', borderRadius: 'var(--radius-md)' }}>
                  Bu özelliğin henüz değeri yok. <strong>Düzenle</strong> ile değer ekleyin (örn. Pamuk, Polyester).
                </div>
              ) : (
                <div className="chipset">
                  {values.map((v) => (
                    <span key={v.id} className="sizechip" data-on="true" style={{ cursor: 'default' }}>{v.name}</span>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      <AttrDrawer key={editing ? editing.id : 'new'} open={drawerOpen} editing={editing} onClose={() => { setDrawerOpen(false); setEditing(null) }} onSubmit={submit} onToast={onToast} />
    </div>
  )
}

function AttrDrawer({ open, editing, onClose, onSubmit, onToast }) {
  const keyRef = useRef(1)
  const [name, setName] = useState('')
  const [rows, setRows] = useState([]) // [{_k, id?, name}]
  const [draft, setDraft] = useState('')
  const [editKey, setEditKey] = useState(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!open) return
    if (editing) {
      setName(editing.name)
      setRows((editing._values || []).map((v) => ({ _k: keyRef.current++, id: v.id, name: v.name })))
    } else {
      setName(''); setRows([])
    }
    setDraft(''); setEditKey(null)
  }, [open, editing])

  const addDraft = () => {
    const n = draft.trim()
    if (!n) return
    setRows((rs) => [...rs, { _k: keyRef.current++, name: n }])
    setDraft('')
  }
  const removeRow = (k) => setRows((rs) => rs.filter((r) => r._k !== k))
  const patchRow = (k, patch) => setRows((rs) => rs.map((r) => r._k === k ? { ...r, ...patch } : r))

  const save = async () => {
    if (!name.trim()) { onToast?.({ tone: 'danger', title: 'Özellik adı gerekli' }); return }
    setBusy(true)
    try { await onSubmit({ name: name.trim(), rows: rows.map((r) => ({ ...r, name: (r.name || '').trim() })).filter((r) => r.name) }) }
    catch (e) { onToast?.({ tone: 'danger', title: 'Kaydedilemedi', error: e }) }
    finally { setBusy(false) }
  }

  return (
    <Drawer open={open} title={editing ? 'Özelliği düzenle' : 'Özellik oluştur'} busy={busy} onClose={onClose} onConfirm={save}>
      <Field label="Özellik adı" required auto="Görünen ad — örn. Kumaş, Desen, Yaka tipi. Anahtar (key) ad'dan otomatik üretilir.">
        <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Örneğin: Kumaş" />
      </Field>

      <Field label="Değerler" auto="Bu özelliğin seçenekleri — yaz ve Enter'a bas. Örn. Pamuk, Polyester. Ürün açarken bu değerlerden seçilir.">
        <div className="enter-field">
          <Input
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addDraft() } }}
            placeholder="Örneğin: Pamuk"
          />
          {draft.trim() && <span className="enter-hint">{I('corner-down-left', { size: 13 })} Enter</span>}
        </div>
      </Field>

      {rows.length > 0 && (
        <div className="stack" style={{ gap: 8 }}>
          <span className="list-meta">{rows.length} değer</span>
          {rows.map((r) => (
            <div key={r._k} className="vrow">
              {editKey === r._k ? (
                <Input size="sm" autoFocus value={r.name} style={{ flex: 1 }}
                  onChange={(e) => patchRow(r._k, { name: e.target.value })}
                  onKeyDown={(e) => { if (e.key === 'Enter' || e.key === 'Escape') { e.preventDefault(); setEditKey(null) } }}
                  onBlur={() => setEditKey(null)} />
              ) : (
                <span className="vrow__label" onDoubleClick={() => setEditKey(r._k)}>{r.name}</span>
              )}
              <button className="tb__icon" style={{ width: 26, height: 26 }} title="Adı düzenle" onClick={() => setEditKey(editKey === r._k ? null : r._k)}>{I('pencil', { size: 14 })}</button>
              <button className="tb__icon" style={{ width: 26, height: 26 }} title="Kaldır" onClick={() => removeRow(r._k)}>{I('trash-2')}</button>
            </div>
          ))}
        </div>
      )}
    </Drawer>
  )
}
