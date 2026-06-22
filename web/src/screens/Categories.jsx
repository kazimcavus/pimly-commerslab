import React, { useEffect, useState } from 'react'
import { Button, Badge, Dialog, Field, Input, Select, Checkbox } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'

export function Categories({ onToast }) {
  const [cats, setCats] = useState([])
  const [sel, setSel] = useState(null)
  const [attrs, setAttrs] = useState([])
  const [allAttrs, setAllAttrs] = useState([])
  const [addOpen, setAddOpen] = useState(false)
  const [assignOpen, setAssignOpen] = useState(false)

  const loadCats = () => api.listCategories().then((cs) => {
    setCats(cs)
    if (!sel && cs.length) setSel(cs[0].id)
  }).catch(() => {})
  useEffect(() => { loadCats(); api.listAttributes().then(setAllAttrs).catch(() => {}) }, [])
  useEffect(() => { if (sel) api.listCategoryAttributes(sel).then(setAttrs).catch(() => setAttrs([])) }, [sel])

  const active = cats.find((c) => c.id === sel)
  const childCount = (id) => cats.filter((c) => c.parent_id === id).length
  const depthOf = (c) => {
    let d = 0, cur = c
    const byId = Object.fromEntries(cats.map((x) => [x.id, x]))
    while (cur && cur.parent_id) { d++; cur = byId[cur.parent_id] }
    return d
  }

  return (
    <div className="page">
      <PageHeader eyebrow="Tanımlar" title="Kategoriler" sub="Ağaç yapısı. Seç → atanmış özellikler ve pazaryeri eşlemesi."
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={() => setAddOpen(true)}>Kategori ekle</Button>} />
      <div className="split">
        <div className="tree">
          {cats.map((c) => (
            <div key={c.id} className="tree__node" data-active={sel === c.id} onClick={() => setSel(c.id)} style={{ paddingLeft: 9 + depthOf(c) * 18 }}>
              {I(childCount(c.id) ? 'folder' : 'folder-open')}
              <span>{c.name}</span>
              {childCount(c.id) > 0 && <span className="sb__count">{childCount(c.id)} alt</span>}
            </div>
          ))}
          {cats.length === 0 && <div className="list-meta" style={{ padding: 12 }}>Henüz kategori yok.</div>}
        </div>
        <div className="stack">
          {active && (
            <div className="pim-card">
              <div className="pim-card__header">
                <div className="hstack"><span className="pim-card__title">{active.name}</span>{active.code && <span className="typechip">{active.code}</span>}</div>
              </div>
              <div className="pim-card__body">
                <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-strong)', marginBottom: 10 }} className="between">
                  <span>Atanmış özellikler</span>
                  <Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={() => setAssignOpen(true)}>Özellik ata</Button>
                </div>
                <div className="pim-table-wrap">
                  <table className="pim-table">
                    <thead><tr><th>Özellik</th><th>Anahtar</th><th>Zorunlu</th><th>MP zorunlu</th><th></th></tr></thead>
                    <tbody>
                      {attrs.map((a) => (
                        <tr key={a.category_attribute_id}>
                          <td className="pim-td-strong">{a.name}</td>
                          <td className="pim-td-mono">{a.key}</td>
                          <td>{a.required ? I('check') : <span className="subtle">—</span>}</td>
                          <td>{a.marketplace_required ? I('check') : <span className="subtle">—</span>}</td>
                          <td><div className="rowact"><button className="tb__icon" style={{ width: 28, height: 28 }} title="Kaldır" onClick={async () => { await api.deleteCategoryAttribute(a.category_attribute_id); setAttrs(attrs.filter((x) => x.category_attribute_id !== a.category_attribute_id)) }}>{I('trash-2')}</button></div></td>
                        </tr>
                      ))}
                      {attrs.length === 0 && <tr><td colSpan={5} className="subtle" style={{ padding: 14 }}>Bu kategoriye özellik atanmamış.</td></tr>}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}
          <div className="pim-card">
            <div className="pim-card__header"><span className="pim-card__title">Pazaryeri eşlemesi</span><Badge status="draft">Trendyol</Badge></div>
            <div className="pim-card__body"><div className="list-meta">Eşleme tabloları hazır; gönderim v2'de.</div></div>
          </div>
        </div>
      </div>

      <AddCategoryDialog open={addOpen} onClose={() => setAddOpen(false)} cats={cats}
        onCreate={async (body) => { try { const c = await api.createCategory(body); setAddOpen(false); await loadCats(); setSel(c.id); onToast?.({ tone: 'success', title: 'Kategori eklendi' }) } catch (e) { onToast?.({ tone: 'danger', title: 'Eklenemedi', body: e.message }) } }} />

      <AssignAttrDialog open={assignOpen} onClose={() => setAssignOpen(false)} attrs={allAttrs.filter((a) => !attrs.some((x) => x.attribute_id === a.id))}
        onAssign={async (body) => { try { await api.assignCategoryAttribute(sel, body); setAssignOpen(false); api.listCategoryAttributes(sel).then(setAttrs); onToast?.({ tone: 'success', title: 'Özellik atandı' }) } catch (e) { onToast?.({ tone: 'danger', title: 'Atanamadı', body: e.message }) } }} />
    </div>
  )
}

function AddCategoryDialog({ open, onClose, onCreate, cats }) {
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [parent, setParent] = useState('')
  useEffect(() => { if (open) { setName(''); setCode(''); setParent('') } }, [open])
  return (
    <Dialog open={open} title="Kategori ekle" confirmLabel="Ekle" cancelLabel="İptal" onClose={onClose}
      onConfirm={() => name.trim() && onCreate({ name: name.trim(), code: code.trim() || null, parent_id: parent || null })}>
      <Field label="Ad" required><Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Tişört" /></Field>
      <Field label="Kod" optional><Input mono value={code} onChange={(e) => setCode(e.target.value)} placeholder="TS" /></Field>
      <Field label="Üst kategori" optional>
        <Select value={parent} placeholder="(kök)" onChange={(e) => setParent(e.target.value)} options={cats.map((c) => ({ value: c.id, label: c.name }))} />
      </Field>
    </Dialog>
  )
}

function AssignAttrDialog({ open, onClose, onAssign, attrs }) {
  const [attrId, setAttrId] = useState('')
  const [required, setRequired] = useState(false)
  const [mpReq, setMpReq] = useState(false)
  useEffect(() => { if (open) { setAttrId(''); setRequired(false); setMpReq(false) } }, [open])
  return (
    <Dialog open={open} title="Özellik ata" confirmLabel="Ata" cancelLabel="İptal" onClose={onClose}
      onConfirm={() => attrId && onAssign({ attribute_id: attrId, required, marketplace_required: mpReq, sort_order: 0 })}>
      <Field label="Özellik" required>
        <Select value={attrId} placeholder="Seç…" onChange={(e) => setAttrId(e.target.value)} options={attrs.map((a) => ({ value: a.id, label: a.name }))} />
      </Field>
      <div style={{ display: 'flex', gap: 18, marginTop: 4 }}>
        <Checkbox label="Zorunlu" checked={required} onChange={(e) => setRequired(e.target.checked)} />
        <Checkbox label="MP zorunlu" checked={mpReq} onChange={(e) => setMpReq(e.target.checked)} />
      </div>
    </Dialog>
  )
}
