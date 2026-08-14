import React, { useEffect, useRef, useState } from 'react'
import { Button, Input, Banner } from '../../ds'
import { I } from '../icons.jsx'
import { api } from '../../lib/api.js'
import { askConfirm } from '../../lib/confirm.jsx'

// Kısıtlanmış kart yüksekliği ≈ 3 satır chip (3×32px chip + satır boşlukları).
const CLAMP_H = 118

// Izgara modunda özellik kartı: chip alanı ~3 satırla sınırlıdır; taşıyorsa
// "Devamını gör" çıkar. Genişleme kartı öne getirir (overlay) — ızgara hücresi
// kısıtlanmış yüksekliğini korur, yandaki kart uzamaz. Dışarı tıklayınca kapanır.
function AttrCard({ grid, head, children }) {
  const [expanded, setExpanded] = useState(false)
  const [overflowing, setOverflowing] = useState(false)
  const [slotH, setSlotH] = useState(null)
  const cardRef = useRef(null)
  const clipRef = useRef(null)

  useEffect(() => {
    // Eş boy uzatma clip kutusunu büyütebilir; taşma kararını iç chipset'in
    // doğal yüksekliği verir, yoksa kısa kartlar da "taşıyor" sanılır.
    const inner = clipRef.current?.firstElementChild
    if (!inner || !grid) return
    const check = () => setOverflowing(inner.scrollHeight > CLAMP_H + 2)
    check()
    const ro = new ResizeObserver(check)
    ro.observe(inner)
    return () => ro.disconnect()
  }, [grid, children])

  useEffect(() => {
    if (!expanded) return
    const onDoc = (e) => { if (!cardRef.current?.contains(e.target)) setExpanded(false) }
    document.addEventListener('mousedown', onDoc)
    return () => document.removeEventListener('mousedown', onDoc)
  }, [expanded])

  if (!grid) {
    return <div className="vtype">{head}{children}</div>
  }

  const toggle = () => {
    if (!expanded) setSlotH(cardRef.current?.offsetHeight || null)
    setExpanded((x) => !x)
  }

  return (
    <div className="attr-slot" style={expanded && slotH ? { height: slotH } : undefined}>
      <div ref={cardRef} className="vtype attr-card" data-expanded={expanded || undefined}>
        {head}
        <div ref={clipRef} className="attr-card__clip" data-clamped={(!expanded && overflowing) || undefined}>
          {children}
        </div>
        {overflowing && (
          <button type="button" className="attr-card__more" onClick={toggle}>
            {expanded ? <>Daralt {I('chevron-up', { size: 13 })}</> : <>Devamını gör {I('chevron-down', { size: 13 })}</>}
          </button>
        )}
      </div>
    </div>
  )
}

// Kategori özellikleri editörü (ürün ekleme + ürün detayında paylaşılır).
// Kategorinin TÜM atanmış özelliklerini (boşlar dâhil) listeler; değer chip'leriyle seçim,
// satır-içi değer ekle/sil, özellik ata/kaldır, yeni özellik oluştur+ata.
// pick controlled: { attribute_id: value_id }. onPickChange ile üst bileşene bildirilir.
// onAttrsLoaded(catAttrs) → üst bileşen zorunlu-alan doğrulaması yapabilsin diye.
// grid: özellik kartlarını 2 sütunlu ızgarada dizer (ürün detay/oluştur tasarımı).
export function AttributeEditor({ categoryId, pick = {}, onPickChange, onAttrsLoaded, onToast, grid = false }) {
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
    } catch (e) { onToast?.({ tone: 'danger', title: 'Değer eklenemedi', error: e }) }
  }

  const deleteValue = async (attrId, v) => {
    const ok = await askConfirm({
      title: 'Değeri sil',
      body: `"${v.name}" değeri bu özellikten kalıcı olarak silinecek.`,
      tone: 'danger', confirmLabel: 'Sil',
    })
    if (!ok) return
    try {
      await api.deleteAttributeValue(v.id)
      setAttrVals((m) => ({ ...m, [attrId]: (m[attrId] || []).filter((x) => x.id !== v.id) }))
      if (pick[attrId] === v.id) setVal(attrId, undefined)
    } catch (e) { onToast?.({ tone: 'danger', title: 'Silinemedi', error: e }) }
  }

  const assignAttribute = async (attrId) => {
    try {
      const ca = await api.assignCategoryAttribute(categoryId, { attribute_id: attrId, required: false, sort_order: catAttrs.length })
      const next = [...catAttrs, ca]; setCatAttrs(next); onAttrsLoaded?.(next)
      const vs = await api.listAttributeValues(attrId).catch(() => [])
      setAttrVals((m) => ({ ...m, [attrId]: vs }))
    } catch (e) { onToast?.({ tone: 'danger', title: 'Özellik atanamadı', error: e }) }
  }

  const unassign = async (ca) => {
    const ok = await askConfirm({
      title: 'Özelliği kaldır',
      body: `"${ca.name}" özelliği bu kategoriden kaldırılacak; üründeki seçimi de temizlenir.`,
      tone: 'danger', confirmLabel: 'Kaldır',
    })
    if (!ok) return
    try {
      await api.deleteCategoryAttribute(ca.category_attribute_id)
      const next = catAttrs.filter((x) => x.category_attribute_id !== ca.category_attribute_id)
      setCatAttrs(next); onAttrsLoaded?.(next)
      setVal(ca.attribute_id, undefined)
    } catch (e) { onToast?.({ tone: 'danger', title: 'Kaldırılamadı', error: e }) }
  }

  const createAndAssign = async (name) => {
    const n = name.trim(); if (!n) return
    try {
      const a = await api.createAttribute({ name: n })
      setAllAttrs((xs) => [...xs, a])
      await assignAttribute(a.id)
    } catch (e) { onToast?.({ tone: 'danger', title: 'Özellik oluşturulamadı', error: e }) }
  }

  if (!categoryId) {
    return <Banner tone="info" title="Önce kategori seç">Kategori seçilince o kategorinin özellikleri burada listelenir; değer seçebilir, yeni değer/özellik ekleyebilirsin.</Banner>
  }

  const unassigned = allAttrs.filter((a) => !catAttrs.some((ca) => ca.attribute_id === a.id))
  const commitVal = (attrId) => { addValue(attrId, valDraft); setValDraft(''); setAddingFor(null) }
  const commitNewAttr = () => { if (!newAttr.trim()) return; createAndAssign(newAttr); setNewAttr(''); setAssignOpen(false) }

  return (
    <div className={grid ? 'attr-grid' : 'stack'} style={grid ? undefined : { gap: 12 }}>
      {catAttrs.map((ca) => {
        const vals = attrVals[ca.attribute_id] || []
        return (
          <AttrCard key={ca.category_attribute_id} grid={grid} head={
            <div className="between" style={{ marginBottom: 8 }}>
              <span style={{ fontWeight: 600, color: 'var(--text-strong)' }}>
                {ca.name}{ca.required && <span style={{ color: 'var(--danger-fg)', marginLeft: 4 }}>*</span>}
              </span>
              <button className="tb__icon" style={{ width: 28, height: 28 }} title="Özelliği kategoriden kaldır" onClick={() => unassign(ca)}>{I('x')}</button>
            </div>
          }>
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
          </AttrCard>
        )
      })}

      {assignOpen ? (
        <div className="vtype" data-span="full">
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
        <div data-span="full"><Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={() => setAssignOpen(true)}>Özellik ekle</Button></div>
      )}
    </div>
  )
}
