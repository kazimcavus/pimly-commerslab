import React, { useEffect, useState } from 'react'
import { Button, Drawer, Field, Input } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { askConfirm } from '../lib/confirm.jsx'

// Fiyat tanımları (.NET Catalog): bir tanım = ad + opsiyonel kod. Her varyanta
// tanım başına bir tutar girilir (ürün ekleme + ürün detayı). Trendyol import'u
// "TY Satış" / "TY Karşılaştırma" tanımlarını otomatik oluşturup doldurur.
export function PriceDefinitions({ onToast }) {
  const [defs, setDefs] = useState([])
  const [sel, setSel] = useState(null)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [editing, setEditing] = useState(null) // {id,name,code} or null = create

  const loadDefs = () => api.listPriceDefinitions().then((d) => { setDefs(d); if (!sel && d.length) setSel(d[0].id); return d }).catch(() => [])
  useEffect(() => { loadDefs() }, [])

  const active = defs.find((d) => d.id === sel)

  const openCreate = () => { setEditing(null); setDrawerOpen(true) }
  const openEdit = (def) => { setEditing({ ...def }); setDrawerOpen(true) }

  const submit = async ({ name, code }) => {
    let defId
    if (editing) {
      const updated = await api.updatePriceDefinition(editing.id, { name, code })
      defId = updated?.id || editing.id
    } else {
      const d = await api.createPriceDefinition({ name, code })
      defId = d.id
    }
    setDrawerOpen(false)
    const wasEdit = !!editing
    setEditing(null)
    await loadDefs()
    setSel(defId)
    onToast?.({ tone: 'success', title: wasEdit ? 'Fiyat tanımı güncellendi' : 'Fiyat tanımı eklendi' })
  }

  return (
    <div className="page">
      <PageHeader eyebrow="Tanımlar" title="Fiyatlar" help="prices"
        sub="Burada tanımladığınız fiyat alanları ürün ekleme ve ürün detayında görünür. Trendyol import'u TY Satış / TY Karşılaştırma alanlarını otomatik ekler."
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={openCreate}>Fiyat tanımı ekle</Button>} />
      <div className="split">
        <div className="tree">
          {defs.map((d) => (
            <div key={d.id} className="tree__node" data-active={sel === d.id} onClick={() => setSel(d.id)}>
              {I('banknote')}<span className="tree__name">{d.name}</span>
            </div>
          ))}
          {defs.length === 0 && <div className="list-meta" style={{ padding: 12 }}>Henüz fiyat tanımı yok. Sağ üstten ekleyin.</div>}
        </div>
        {active && (
          <div className="pim-card">
            <div className="pim-card__header">
              <div className="hstack" style={{ gap: 8 }}>
                <span className="pim-card__title">{active.name}</span>
                {active.code && <span className="list-meta pim-td-mono" title="Tanım kodu">{active.code}</span>}
              </div>
              <div className="hstack">
                <Button variant="secondary" size="sm" iconLeft={I('pencil')} onClick={() => openEdit(active)}>Düzenle</Button>
                <button className="tb__icon" style={{ width: 30, height: 30 }} title="Fiyat tanımını sil"
                  onClick={async () => {
                    const ok = await askConfirm({
                      title: 'Fiyat tanımını sil',
                      body: `"${active.name}" tanımı silinecek; ürünlerde bu alana girilmiş fiyatlar da silinir.`,
                      tone: 'danger', confirmLabel: 'Sil',
                    })
                    if (!ok) return
                    try { await api.deletePriceDefinition(active.id); setSel(null); loadDefs(); onToast?.({ tone: 'success', title: 'Fiyat tanımı silindi' }) }
                    catch (e) { onToast?.({ tone: 'danger', title: 'Silinemedi', error: e }) }
                  }}>{I('trash-2')}</button>
              </div>
            </div>
            <div className="pim-card__body">
              <div className="subtle" style={{ padding: 14, border: '1px dashed var(--border-default)', borderRadius: 'var(--radius-md)' }}>
                {active.code
                  ? <>Tanım kodu: <strong>{active.code}</strong></>
                  : <>Bu tanımın kodu yok. <strong>Düzenle</strong> ile kod ekleyebilirsiniz (opsiyonel).</>}
              </div>
            </div>
          </div>
        )}
      </div>

      <PriceDefinitionDrawer key={editing ? editing.id : 'new'} open={drawerOpen} editing={editing} onClose={() => { setDrawerOpen(false); setEditing(null) }} onSubmit={submit} onToast={onToast} />
    </div>
  )
}

function PriceDefinitionDrawer({ open, editing, onClose, onSubmit, onToast }) {
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!open) return
    if (editing) {
      setName(editing.name || '')
      setCode(editing.code || '')
    } else {
      setName(''); setCode('')
    }
  }, [open, editing])

  const save = async () => {
    if (!name.trim()) { onToast?.({ tone: 'danger', title: 'Tanım adı gerekli' }); return }
    setBusy(true)
    try { await onSubmit({ name: name.trim(), code: code.trim() || null }) }
    catch (e) { onToast?.({ tone: 'danger', title: 'Kaydedilemedi', error: e }) }
    finally { setBusy(false) }
  }

  return (
    <Drawer open={open} title={editing ? 'Fiyat tanımını düzenle' : 'Fiyat tanımı oluştur'} busy={busy} onClose={onClose} onConfirm={save}>
      <Field label="Ad" required auto='Görünen ad — örn. "Trendyol Satış".'>
        <Input value={name} onChange={(e) => setName(e.target.value)} placeholder='örn. "Trendyol Satış"' />
      </Field>

      <Field label="Kod (opsiyonel)" auto="Tanımın kısa kodu — örn. ty_sale. İstemezsen boş bırak.">
        <Input mono value={code} onChange={(e) => setCode(e.target.value)} placeholder='örn. "ty_sale"' />
      </Field>
    </Drawer>
  )
}
