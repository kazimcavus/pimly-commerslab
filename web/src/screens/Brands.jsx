import React, { useEffect, useState } from 'react'
import { Button, Drawer, Field, Input } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'

// Markalar (.NET Catalog): bir marka = ad + opsiyonel kod. Ürün açarken/detayında
// seçilir. Düz bir tanım listesi — sol tarafta liste, sağda seçili markanın kartı.
export function Brands({ onToast }) {
  const [brands, setBrands] = useState([])
  const [sel, setSel] = useState(null)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [editing, setEditing] = useState(null) // {id,name,code} or null = create

  const loadBrands = () => api.listBrands().then((b) => { setBrands(b); if (!sel && b.length) setSel(b[0].id); return b }).catch(() => [])
  useEffect(() => { loadBrands() }, [])

  const active = brands.find((b) => b.id === sel)

  const openCreate = () => { setEditing(null); setDrawerOpen(true) }
  const openEdit = (brand) => { setEditing({ ...brand }); setDrawerOpen(true) }

  const submit = async ({ name, code }) => {
    let brandId
    if (editing) {
      const updated = await api.updateBrand(editing.id, { name, code })
      brandId = updated?.id || editing.id
    } else {
      const b = await api.createBrand({ name, code })
      brandId = b.id
    }
    setDrawerOpen(false)
    const wasEdit = !!editing
    setEditing(null)
    await loadBrands()
    setSel(brandId)
    onToast?.({ tone: 'success', title: wasEdit ? 'Marka güncellendi' : 'Marka eklendi' })
  }

  return (
    <div className="page">
      <PageHeader eyebrow="Tanımlar" title="Markalar" help="brands"
        sub="Ürünlerinizin markaları — örn. Nike, Adidas. Ürün açarken ya da ürün detayında seçilir."
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={openCreate}>Marka ekle</Button>} />
      <div className="split">
        <div className="tree">
          {brands.map((b) => (
            <div key={b.id} className="tree__node" data-active={sel === b.id} onClick={() => setSel(b.id)}>
              {I('award')}<span className="tree__name">{b.name}</span>
            </div>
          ))}
          {brands.length === 0 && <div className="list-meta" style={{ padding: 12 }}>Henüz marka yok. Sağ üstten ekleyin.</div>}
        </div>
        {active && (
          <div className="pim-card">
            <div className="pim-card__header">
              <div className="hstack" style={{ gap: 8 }}>
                <span className="pim-card__title">{active.name}</span>
                {active.code && <span className="list-meta pim-td-mono" title="Marka kodu">{active.code}</span>}
              </div>
              <div className="hstack">
                <Button variant="secondary" size="sm" iconLeft={I('pencil')} onClick={() => openEdit(active)}>Düzenle</Button>
                <button className="tb__icon" style={{ width: 30, height: 30 }} title="Markayı sil"
                  onClick={async () => {
                    if (!confirm('Bu markayı silmek istediğinize emin misiniz?')) return
                    try { await api.deleteBrand(active.id); setSel(null); loadBrands(); onToast?.({ tone: 'success', title: 'Marka silindi' }) }
                    catch (e) { onToast?.({ tone: 'danger', title: 'Silinemedi', body: e.message }) }
                  }}>{I('trash-2')}</button>
              </div>
            </div>
            <div className="pim-card__body">
              <div className="subtle" style={{ padding: 14, border: '1px dashed var(--border-default)', borderRadius: 'var(--radius-md)' }}>
                {active.code
                  ? <>Marka kodu: <strong>{active.code}</strong></>
                  : <>Bu markanın kodu yok. <strong>Düzenle</strong> ile kod ekleyebilirsiniz (opsiyonel).</>}
              </div>
            </div>
          </div>
        )}
      </div>

      <BrandDrawer key={editing ? editing.id : 'new'} open={drawerOpen} editing={editing} onClose={() => { setDrawerOpen(false); setEditing(null) }} onSubmit={submit} onToast={onToast} />
    </div>
  )
}

function BrandDrawer({ open, editing, onClose, onSubmit, onToast }) {
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
    if (!name.trim()) { onToast?.({ tone: 'danger', title: 'Marka adı gerekli' }); return }
    setBusy(true)
    try { await onSubmit({ name: name.trim(), code: code.trim() || null }) }
    catch (e) { onToast?.({ tone: 'danger', title: 'Kaydedilemedi', body: e.message }) }
    finally { setBusy(false) }
  }

  return (
    <Drawer open={open} title={editing ? 'Markayı düzenle' : 'Marka oluştur'} busy={busy} onClose={onClose} onConfirm={save}>
      <Field label="Marka adı" required auto="Görünen ad — örn. Nike, Adidas.">
        <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Örneğin: Nike" />
      </Field>

      <Field label="Kod (opsiyonel)" auto="Markanın kısa kodu — örn. NKE. İstemezsen boş bırak.">
        <Input mono value={code} onChange={(e) => setCode(e.target.value)} placeholder="Örneğin: NKE" />
      </Field>
    </Drawer>
  )
}
