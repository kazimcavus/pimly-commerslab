import React, { useEffect, useState } from 'react'
import { Button, Input, Banner } from '../../ds'
import { I } from '../icons.jsx'
import { api } from '../../lib/api.js'

// Kategori özellikleri editörü (ürün ekleme + ürün detayında paylaşılır).
// Kategorinin TÜM atanmış özelliklerini (boşlar dâhil) listeler; değer chip'leriyle seçim,
// satır-içi değer ekle/sil, özellik ata/kaldır, yeni özellik oluştur+ata.
// pick controlled: { attribute_id: value_id }. onPickChange ile üst bileşene bildirilir.
// onAttrsLoaded(catAttrs) → üst bileşen zorunlu-alan doğrulaması yapabilsin diye.
export function AttributeEditor({ categoryId, pick = {}, onPickChange, onAttrsLoaded, onToast }) {
  const [catAttrs, setCatAttrs] = useState([])
  const [attrVals, setAttrVals] = useState({})   // { attribute_id: [{id,name}] }
  const [allAttrs, setAllAttrs] = useState([])
  const [addingFor, setAddingFor] = useState(null)
  const [valDraft, setValDraft] = useState('')
  const [assignOpen, setAssignOpen] = useState(false)
  const [newAttr, setNewAttr] = useState('')

  useEffect(() => { api.listAttributes().then(setAllAttrs).catch(() => {}) }, [])

  useEffect(() => {
    if (!categoryId) { setCatAttrs([]); setAttrVals({}); onAttrsLoaded?.([]); return }
    let alive = true
    api.listCategoryAttributes(categoryId).then(async (cas) => {
      if (!alive) return
      setCatAttrs(cas); onAttrsLoaded?.(cas)
      const entries = await Promise.all(cas.map((ca) =>
        api.listAttributeValues(ca.attribute_id).then((vs) => [ca.attribute_id, vs]).catch(() => [ca.attribute_id, []])))
      if (alive) setAttrVals(Object.fromEntries(entries))
    }).catch(() => { if (alive) { setCatAttrs([]); setAttrVals({}); onAttrsLoaded?.([]) } })
    return () => { alive = false }
  }, [categoryId])

  const setVal = (attrId, valId) => onPickChange?.({ ...pick, [attrId]: valId })

  const addValue = async (attrId, name) => {
    const n = name.trim(); if (!n) return
    try {
      const v = await api.createAttributeValue(attrId, { name: n })
      setAttrVals((m) => ({ ...m, [attrId]: [...(m[attrId] || []), v] }))
      setVal(attrId, v.id)
    } catch (e) { onToast?.({ tone: 'danger', title: 'Değer eklenemedi', body: e.message }) }
  }

  const deleteValue = async (attrId, v) => {
    if (!confirm(`"${v.name}" değeri silinecek. Emin misin?`)) return
    try {
      await api.deleteAttributeValue(v.id)
      setAttrVals((m) => ({ ...m, [attrId]: (m[attrId] || []).filter((x) => x.id !== v.id) }))
      if (pick[attrId] === v.id) setVal(attrId, undefined)
    } catch (e) { onToast?.({ tone: 'danger', title: 'Silinemedi', body: e.message }) }
  }

  const assignAttribute = async (attrId) => {
    try {
      const ca = await api.assignCategoryAttribute(categoryId, { attribute_id: attrId, required: false, sort_order: catAttrs.length })
      const next = [...catAttrs, ca]; setCatAttrs(next); onAttrsLoaded?.(next)
      const vs = await api.listAttributeValues(attrId).catch(() => [])
      setAttrVals((m) => ({ ...m, [attrId]: vs }))
    } catch (e) { onToast?.({ tone: 'danger', title: 'Özellik atanamadı', body: e.message }) }
  }

  const unassign = async (ca) => {
    if (!confirm(`"${ca.name}" özelliği bu kategoriden kaldırılacak. Emin misin?`)) return
    try {
      await api.deleteCategoryAttribute(ca.category_attribute_id)
      const next = catAttrs.filter((x) => x.category_attribute_id !== ca.category_attribute_id)
      setCatAttrs(next); onAttrsLoaded?.(next)
      setVal(ca.attribute_id, undefined)
    } catch (e) { onToast?.({ tone: 'danger', title: 'Kaldırılamadı', body: e.message }) }
  }

  const createAndAssign = async (name) => {
    const n = name.trim(); if (!n) return
    try {
      const a = await api.createAttribute({ name: n })
      setAllAttrs((xs) => [...xs, a])
      await assignAttribute(a.id)
    } catch (e) { onToast?.({ tone: 'danger', title: 'Özellik oluşturulamadı', body: e.message }) }
  }

  if (!categoryId) {
    return <Banner tone="info" title="Önce kategori seç">Kategori seçilince o kategorinin özellikleri burada listelenir; değer seçebilir, yeni değer/özellik ekleyebilirsin.</Banner>
  }

  const unassigned = allAttrs.filter((a) => !catAttrs.some((ca) => ca.attribute_id === a.id))
  const commitVal = (attrId) => { addValue(attrId, valDraft); setValDraft(''); setAddingFor(null) }
  const commitNewAttr = () => { if (!newAttr.trim()) return; createAndAssign(newAttr); setNewAttr(''); setAssignOpen(false) }

  return (
    <div className="stack" style={{ gap: 12 }}>
      {catAttrs.map((ca) => {
        const vals = attrVals[ca.attribute_id] || []
        return (
          <div key={ca.category_attribute_id} className="vtype">
            <div className="between" style={{ marginBottom: 8 }}>
              <span style={{ fontWeight: 600, color: 'var(--text-strong)' }}>
                {ca.name}{ca.required && <span style={{ color: 'var(--danger-fg)', marginLeft: 4 }}>*</span>}
              </span>
              <button className="tb__icon" style={{ width: 28, height: 28 }} title="Özelliği kategoriden kaldır" onClick={() => unassign(ca)}>{I('x')}</button>
            </div>
            <div className="chipset" style={{ alignItems: 'center' }}>
              {vals.map((v) => (
                <span key={v.id} className="sizechip" data-on={pick[ca.attribute_id] === v.id}
                  style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}
                  onClick={() => setVal(ca.attribute_id, pick[ca.attribute_id] === v.id ? undefined : v.id)}>
                  {v.name}
                  <span role="button" title="Değeri sil" onClick={(e) => { e.stopPropagation(); deleteValue(ca.attribute_id, v) }}
                    style={{ display: 'inline-flex', opacity: 0.5 }}>{I('x', { size: 12 })}</span>
                </span>
              ))}
              {addingFor === ca.attribute_id ? (
                <span className="enter-field" style={{ display: 'inline-block', width: 160 }}>
                  <Input size="sm" autoFocus value={valDraft} onChange={(e) => setValDraft(e.target.value)}
                    onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); commitVal(ca.attribute_id) } if (e.key === 'Escape') { setAddingFor(null); setValDraft('') } }}
                    onBlur={() => commitVal(ca.attribute_id)} placeholder="Yeni değer" />
                </span>
              ) : (
                <span className="sizechip sizechip--add" onClick={() => { setAddingFor(ca.attribute_id); setValDraft('') }}>{I('plus', { size: 13 })} Değer ekle</span>
              )}
            </div>
          </div>
        )
      })}

      {assignOpen ? (
        <div className="vtype">
          <div className="list-meta" style={{ marginBottom: 8 }}>Bu kategoriye özellik ekle</div>
          <div className="chipset">
            {unassigned.map((a) => (
              <span key={a.id} className="sizechip" onClick={() => { assignAttribute(a.id); setAssignOpen(false) }}>{a.name}</span>
            ))}
            {unassigned.length === 0 && <span className="list-meta">Tüm mevcut özellikler atanmış — aşağıdan yeni oluştur.</span>}
          </div>
          <div className="hstack" style={{ marginTop: 10, gap: 8 }}>
            <span className="enter-field" style={{ flex: 1, maxWidth: 260 }}>
              <Input size="sm" value={newAttr} onChange={(e) => setNewAttr(e.target.value)} placeholder="Yeni özellik adı (örn. Kumaş)"
                onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); commitNewAttr() } }} />
            </span>
            <Button size="sm" variant="secondary" onClick={commitNewAttr}>Oluştur &amp; ekle</Button>
            <Button size="sm" variant="ghost" onClick={() => { setAssignOpen(false); setNewAttr('') }}>Kapat</Button>
          </div>
        </div>
      ) : (
        <div><Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={() => setAssignOpen(true)}>Özellik ekle</Button></div>
      )}
    </div>
  )
}
