import React, { useEffect, useState } from 'react'
import { Button, Badge, Dialog, Field, Input, Select, Checkbox } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { slugify } from '../lib/slug.js'

const EXP_KEY = 'pimly_cat_expanded'
const readExpanded = () => { try { return JSON.parse(localStorage.getItem(EXP_KEY) || '{}') || {} } catch { return {} } }

export function Categories({ onToast }) {
  const [cats, setCats] = useState([])
  const [sel, setSel] = useState(null)
  const [attrs, setAttrs] = useState([])
  const [allAttrs, setAllAttrs] = useState([])
  const [dialog, setDialog] = useState(null) // { mode: 'add' | 'edit', initial }
  const [assignOpen, setAssignOpen] = useState(false)
  const [expanded, setExpanded] = useState(readExpanded)
  const [tyMapping, setTyMapping] = useState(null) // seçili kategorinin Trendyol eşlemesi

  const loadCats = () => api.listCategories().then((cs) => {
    setCats(cs)
    setSel((s) => s || (cs.length ? cs[0].id : null))
  }).catch(() => {})
  useEffect(() => { loadCats(); api.listAttributes().then(setAllAttrs).catch(() => {}) }, [])
  useEffect(() => { if (sel) api.listCategoryAttributes(sel).then(setAttrs).catch(() => setAttrs([])) }, [sel])
  // Seçili kategorinin Trendyol eşlemesi (import otomatik kurar; 404 = eşleme yok).
  useEffect(() => {
    setTyMapping(null)
    if (sel) api.getCategoryMapping('TY', sel).then(setTyMapping).catch(() => setTyMapping(null))
  }, [sel])

  const active = cats.find((c) => c.id === sel)
  const childrenOf = (id) => cats.filter((c) => (c.parent_id || null) === (id || null))
  const descendantsOf = (id) => {
    const out = []
    const walk = (pid) => cats.filter((c) => c.parent_id === pid).forEach((c) => { out.push(c.id); walk(c.id) })
    walk(id)
    return out
  }

  const persistExpanded = (next) => { try { localStorage.setItem(EXP_KEY, JSON.stringify(next)) } catch {} ; return next }
  const toggleNode = (id) => setExpanded((p) => persistExpanded({ ...p, [id]: p[id] === false ? true : false }))

  // Bir kategoriyi seçerken üst zincirini otomatik aç (görünür kalsın).
  const selectCat = (id) => {
    setSel(id)
    const byId = Object.fromEntries(cats.map((x) => [x.id, x]))
    const anc = {}
    let cur = byId[id]
    while (cur && cur.parent_id) { anc[cur.parent_id] = true; cur = byId[cur.parent_id] }
    if (Object.keys(anc).length) setExpanded((p) => persistExpanded({ ...p, ...anc }))
  }

  const isOpen = (id) => expanded[id] !== false

  // Özyinelemeli ağaç çizimi — her düğüm çocuklarını altında çizer.
  const renderNode = (c, depth) => {
    const kids = childrenOf(c.id)
    const open = isOpen(c.id)
    const icon = kids.length ? (open ? 'folder-open' : 'folder') : 'tag'
    return (
      <React.Fragment key={c.id}>
        <div className="tree__node" data-active={sel === c.id} onClick={() => selectCat(c.id)} style={{ paddingLeft: 8 + depth * 18 }}>
          {kids.length > 0 ? (
            <button className="tree__chev" data-open={open} title={open ? 'Kapat' : 'Aç'}
              onClick={(e) => { e.stopPropagation(); toggleNode(c.id) }}>{I('chevron-right', { size: 14 })}</button>
          ) : <span className="tree__chev-spacer" />}
          {I(icon)}
          <span className="tree__name">{c.name}</span>
          {kids.length > 0 && <span className="sb__count">{kids.length} alt</span>}
        </div>
        {open && kids.map((k) => renderNode(k, depth + 1))}
      </React.Fragment>
    )
  }

  const removeCat = async () => {
    if (!active) return
    if (!confirm(`"${active.name}" kategorisi silinecek. Emin misin?`)) return
    try {
      await api.deleteCategory(active.id)
      setSel(null)
      await loadCats()
      onToast?.({ tone: 'success', title: 'Kategori silindi' })
    } catch (e) { onToast?.({ tone: 'danger', title: 'Silinemedi', body: e.message }) }
  }

  const submitCategory = async (body) => {
    try {
      if (dialog?.mode === 'edit') {
        await api.updateCategory(dialog.initial.id, body)
        setDialog(null)
        await loadCats()
        onToast?.({ tone: 'success', title: 'Kategori güncellendi' })
      } else {
        const c = await api.createCategory(body)
        setDialog(null)
        await loadCats()
        selectCat(c.id)
        onToast?.({ tone: 'success', title: 'Kategori eklendi' })
      }
    } catch (e) { onToast?.({ tone: 'danger', title: dialog?.mode === 'edit' ? 'Güncellenemedi' : 'Eklenemedi', body: e.message }) }
  }

  const editExclude = dialog?.mode === 'edit' && dialog.initial ? [dialog.initial.id, ...descendantsOf(dialog.initial.id)] : []

  return (
    <div className="page">
      <PageHeader eyebrow="Tanımlar" title="Kategoriler" help="categories" sub="Ağaç yapısı. Seç → atanmış özellikler ve pazaryeri eşlemesi."
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={() => setDialog({ mode: 'add', initial: { parent_id: active?.id || null } })}>Kategori ekle</Button>} />
      <div className="split">
        <div className="tree">
          {childrenOf(null).map((r) => renderNode(r, 0))}
          {cats.length === 0 && <div className="list-meta" style={{ padding: 12 }}>Henüz kategori yok.</div>}
        </div>
        <div className="stack">
          {active && (
            <div className="pim-card">
              <div className="pim-card__header">
                <div className="hstack"><span className="pim-card__title">{active.name}</span>{active.code && <span className="typechip">{active.code}</span>}</div>
                <div className="hstack">
                  <Button variant="secondary" size="sm" iconLeft={I('pencil')} onClick={() => setDialog({ mode: 'edit', initial: active })}>Düzenle</Button>
                  <button className="tb__icon" style={{ width: 30, height: 30 }} title="Kategoriyi sil" onClick={removeCat}>{I('trash-2')}</button>
                </div>
              </div>
              <div className="pim-card__body">
                <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-strong)', marginBottom: 10 }} className="between">
                  <span>Atanmış özellikler</span>
                  <Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={() => setAssignOpen(true)}>Özellik ata</Button>
                </div>
                <div className="pim-table-wrap">
                  <table className="pim-table">
                    <thead><tr><th>Özellik</th><th>Anahtar</th><th>Zorunlu</th><th></th></tr></thead>
                    <tbody>
                      {attrs.map((a) => (
                        <tr key={a.category_attribute_id}>
                          <td className="pim-td-strong">{a.name}</td>
                          <td className="pim-td-mono">{a.key}</td>
                          <td>{a.required ? I('check') : <span className="subtle">—</span>}</td>
                          <td><div className="rowact"><button className="tb__icon" style={{ width: 28, height: 28 }} title="Kaldır" onClick={async () => { await api.deleteCategoryAttribute(a.category_attribute_id); setAttrs(attrs.filter((x) => x.category_attribute_id !== a.category_attribute_id)) }}>{I('trash-2')}</button></div></td>
                        </tr>
                      ))}
                      {attrs.length === 0 && <tr><td colSpan={4} className="subtle" style={{ padding: 14 }}>Bu kategoriye özellik atanmamış.</td></tr>}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}
          <div className="pim-card">
            <div className="pim-card__header">
              <span className="pim-card__title">Pazaryeri eşlemesi</span>
              <Badge status={tyMapping ? 'active' : 'draft'}>Trendyol</Badge>
            </div>
            <div className="pim-card__body">
              {tyMapping ? (
                <div className="stack" style={{ gap: 6 }}>
                  <div style={{ color: 'var(--text-strong)', fontWeight: 600 }}>
                    {tyMapping.external_category?.path || tyMapping.external_id}
                  </div>
                  <div className="list-meta">
                    Trendyol kategori ID: <span className="mono">{tyMapping.external_id}</span>
                    {' — '}gönderimde (v2) ürünler bu kategoriye açılır.
                  </div>
                </div>
              ) : (
                <div className="list-meta">
                  Bu kategori henüz Trendyol'a eşlenmedi. Import sırasında otomatik kurulur;
                  gönderim (v2) bu eşlemeyi kullanır.
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      <CategoryDialog open={!!dialog} mode={dialog?.mode} initial={dialog?.initial} cats={cats} excludeIds={editExclude}
        onClose={() => setDialog(null)} onSubmit={submitCategory} />

      <AssignAttrDialog open={assignOpen} onClose={() => setAssignOpen(false)} attrs={allAttrs.filter((a) => !attrs.some((x) => x.attribute_id === a.id))}
        onAssign={async (body) => { try { await api.assignCategoryAttribute(sel, body); setAssignOpen(false); api.listCategoryAttributes(sel).then(setAttrs); onToast?.({ tone: 'success', title: 'Özellik atandı' }) } catch (e) { onToast?.({ tone: 'danger', title: 'Atanamadı', body: e.message }) } }} />
    </div>
  )
}

// Ekle + Düzenle ortak modali. Kod alanı yok — ad'dan otomatik türetilir (slugify).
function CategoryDialog({ open, mode, initial, onClose, onSubmit, cats, excludeIds }) {
  const [name, setName] = useState('')
  const [parent, setParent] = useState('')
  useEffect(() => { if (open) { setName(initial?.name || ''); setParent(initial?.parent_id || '') } }, [open, initial])
  const parentOpts = cats.filter((c) => !(excludeIds || []).includes(c.id)).map((c) => ({ value: c.id, label: c.name }))
  const submit = () => { const n = name.trim(); if (n) onSubmit({ name: n, code: slugify(n), parent_id: parent || null }) }
  return (
    <Dialog open={open} title={mode === 'edit' ? 'Kategori düzenle' : 'Kategori ekle'} confirmLabel={mode === 'edit' ? 'Kaydet' : 'Ekle'} cancelLabel="İptal" onClose={onClose} onConfirm={submit}>
      <Field label="Ad" required help="Kod, ad'dan otomatik üretilir."><Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Tişört" /></Field>
      <Field label="Üst kategori" optional>
        <Select value={parent} placeholder="(kök)" onChange={(e) => setParent(e.target.value)} options={parentOpts} />
      </Field>
    </Dialog>
  )
}

function AssignAttrDialog({ open, onClose, onAssign, attrs }) {
  const [attrId, setAttrId] = useState('')
  const [required, setRequired] = useState(false)
  useEffect(() => { if (open) { setAttrId(''); setRequired(false) } }, [open])
  return (
    <Dialog open={open} title="Özellik ata" confirmLabel="Ata" cancelLabel="İptal" onClose={onClose}
      onConfirm={() => attrId && onAssign({ attribute_id: attrId, required, sort_order: 0 })}>
      <Field label="Özellik" required>
        <Select value={attrId} placeholder="Seç…" onChange={(e) => setAttrId(e.target.value)} options={attrs.map((a) => ({ value: a.id, label: a.name }))} />
      </Field>
      <div style={{ marginTop: 4 }}>
        <Checkbox label="Zorunlu" checked={required} onChange={(e) => setRequired(e.target.checked)} />
      </div>
    </Dialog>
  )
}
