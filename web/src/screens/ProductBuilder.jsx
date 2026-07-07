import React, { useEffect, useMemo, useRef, useState } from 'react'
import { Button, Field, Input, Select, Banner, Textarea } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { parseTrMoney } from '../lib/format.js'
import { loadSkuConfig } from '../lib/skuConfig.js'

const MAX_TYPES = 3
const VAR_COLS = '1.5fr 0.9fr 0.9fr 0.6fr 1.1fr 1.1fr'
const SEG_NAME = { fixed: 'Sabit', counter: 'Sıra', year: 'Yıl', manual: 'Değer' }

// Cartesian product of arrays-of-values → array of combos (each combo an array).
function cartesian(arrays) {
  return arrays.reduce((acc, arr) => acc.flatMap((a) => arr.map((b) => [...a, b])), [[]])
}

const comboKey = (combo) => combo.map((v) => v.id).join('|')
const swatchOf = (v) => v.image_url ? { backgroundImage: `url(${v.image_url})`, backgroundSize: 'cover' } : { background: v.color || '#d3ccc1' }

export function ProductBuilder({ onNavigate, onToast, onSaved }) {
  const [categories, setCategories] = useState([])
  const [brands, setBrands] = useState([])
  const [types, setTypes] = useState([]) // [{id,name,selection_style,values:[]}]
  const [categoryId, setCategoryId] = useState('')
  const [brandId, setBrandId] = useState('')
  const [groupCode, setGroupCode] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [status, setStatus] = useState('draft')
  const [mode, setMode] = useState('variant') // 'simple' | 'variant'

  // Simple product single SKU/barcode.
  const [simple, setSimple] = useState({ sku: '', barcode: '', price: '', compareAt: '', stock: '0' })

  // Variant product: chosen types (ordered) + selected value ids + per-combo data.
  const [chosen, setChosen] = useState([]) // [{ typeId, valueIds: [] }]
  const [rowData, setRowData] = useState({}) // { comboKey: {price,compareAt,stock,sku} }
  const [adding, setAdding] = useState(false)
  // Ayraç değeri başına ürün adı: yalnızca elle değiştirilen girişler tutulur;
  // boş bırakılanlar ürün adından + katalog ayarındaki konum tercihinden türetilir.
  const [splitNames, setSplitNames] = useState({}) // { slicerValueId: text }
  const [namePos, setNamePos] = useState('suffix') // 'suffix' | 'prefix' — backend'den yüklenir

  // Tanımlı fiyat alanları (Tanımlar → Fiyatlar): ürün seviyesinde opsiyonel tutarlar.
  const [priceDefs, setPriceDefs] = useState([]) // [{id,name,code}]
  const [defPrices, setDefPrices] = useState({}) // { defId: text }

  // Tenant settings: SKU generator + barcode config.
  const [skuCfg, setSkuCfg] = useState({ enabled: false, segments: [] })
  const [bcOn, setBcOn] = useState(false)
  const [codeInputs, setCodeInputs] = useState({}) // { segIndex: value } for manual

  // Category attributes: assigned attrs of the chosen category + their values + selection.
  const [allAttrs, setAllAttrs] = useState([])     // [{id,name,key}] — full attribute list
  const [catAttrs, setCatAttrs] = useState([])     // [{category_attribute_id,attribute_id,name,required,...}]
  const [attrVals, setAttrVals] = useState({})     // { attribute_id: [{id,name}] }
  const [attrPick, setAttrPick] = useState({})     // { attribute_id: valueId }

  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => { api.listCategories().then(setCategories).catch(() => {}) }, [])
  useEffect(() => { api.listBrands().then(setBrands).catch(() => {}) }, [])
  useEffect(() => { api.listAttributes().then(setAllAttrs).catch(() => {}) }, [])
  useEffect(() => { api.listPriceDefinitions().then(setPriceDefs).catch(() => {}) }, [])
  useEffect(() => { api.getCatalogSettings().then((s) => setNamePos(s.slicer_name_position || 'suffix')).catch(() => {}) }, [])

  // Load the selected category's assigned attributes + each attribute's values.
  useEffect(() => {
    if (!categoryId) { setCatAttrs([]); setAttrVals({}); setAttrPick({}); return }
    let alive = true
    api.listCategoryAttributes(categoryId).then(async (cas) => {
      if (!alive) return
      setCatAttrs(cas)
      const entries = await Promise.all(cas.map((ca) =>
        api.listAttributeValues(ca.attribute_id).then((vs) => [ca.attribute_id, vs]).catch(() => [ca.attribute_id, []])))
      if (alive) setAttrVals(Object.fromEntries(entries))
    }).catch(() => { if (alive) { setCatAttrs([]); setAttrVals({}) } })
    return () => { alive = false }
  }, [categoryId])
  // SKU şablonu .NET Catalog'dan; barkod serisi .NET'ten.
  useEffect(() => {
    loadSkuConfig()
      .then((cfg) => { if (cfg.enabled && cfg.segments.length) setSkuCfg({ enabled: true, segments: cfg.segments }) })
      .catch(() => {})
  }, [])
  useEffect(() => {
    // Seri yapılandırılmış ve istemci tahsisi gerekmiyorsa barkod otomatik atanır.
    api.getBarcodeSequence().then((b) => { if (b && !b.client_allocation_required) setBcOn(true) }).catch(() => {})
  }, [])
  useEffect(() => {
    api.listVariantTypes().then(async (ts) => {
      const withVals = await Promise.all(ts.map((t) => api.listVariantValues(t.id).then((vs) => ({ ...t, values: vs })).catch(() => ({ ...t, values: [] }))))
      setTypes(withVals)
    }).catch(() => {})
  }, [])

  const typeById = useMemo(() => Object.fromEntries(types.map((t) => [t.id, t])), [types])
  const availableToAdd = types.filter((t) => !chosen.some((c) => c.typeId === t.id))

  // Types that actually contribute to combos (added AND have ≥1 selected value).
  const activeChosen = chosen.filter((c) => c.valueIds.length > 0)
  const combos = useMemo(() => {
    if (mode !== 'variant' || activeChosen.length === 0) return []
    const valueArrays = activeChosen.map((c) => {
      const t = typeById[c.typeId]
      return (t?.values || []).filter((v) => c.valueIds.includes(v.id))
    })
    return cartesian(valueArrays)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mode, chosen, typeById])

  // Barkod üretici açıksa alanlar gerçekten dolu görünsün: eksik satırlar için
  // sunucudan barkod tahsis edilir (tahsisler barcode_allocations'ta izlenir).
  const allocatingRef = useRef(false)
  useEffect(() => {
    if (!bcOn || allocatingRef.current) return
    if (mode === 'simple') {
      if (simple.barcode) return
      allocatingRef.current = true
      api.allocateBarcodes(1)
        .then((r) => { const b = r?.barcodes?.[0]; if (b) setSimple((s) => (s.barcode ? s : { ...s, barcode: b })) })
        .catch(() => {})
        .finally(() => { allocatingRef.current = false })
      return
    }
    const missing = combos.map(comboKey).filter((k) => !(rowData[k]?.barcode))
    if (missing.length === 0) return
    allocatingRef.current = true
    api.allocateBarcodes(missing.length)
      .then((r) => {
        const bs = r?.barcodes || []
        setRowData((d) => {
          const next = { ...d }
          missing.forEach((k, i) => { if (!next[k]?.barcode && bs[i]) next[k] = { ...next[k], barcode: bs[i] } })
          return next
        })
      })
      .catch(() => {})
      .finally(() => { allocatingRef.current = false })
  }, [bcOn, mode, combos, rowData, simple.barcode])

  // SKU template preview (mirrors the backend assembly).
  const skuOn = skuCfg.enabled
  const isVarSeg = (t) => t === 'color' || t === 'size'
  const skuToken = (seg, i) => {
    switch (seg.type) {
      case 'fixed': return (seg.value || '').toUpperCase()
      case 'manual': return (codeInputs[i] || '').toUpperCase()
      case 'counter': return String(seg.start ?? 1).padStart(seg.width || 4, '0')
      case 'year': { const yy = new Date().getFullYear(); return seg.digits === 4 ? String(yy) : String(yy % 100) }
      default: return ''
    }
  }
  const optToken = (v, source) => ((source === 'name' ? v.label : (v.key || v.label)) || '').toUpperCase()
  const productCodePreview = skuOn ? skuCfg.segments.map((s, i) => isVarSeg(s.type) ? '' : skuToken(s, i)).join('') : ''
  const variantSkuPreview = (combo) => {
    if (!skuOn) return ''
    let out = productCodePreview
    for (const s of skuCfg.segments) {
      if (s.type === 'color') { const c = combo.find((v) => v.color || v.image_url); if (c) out += optToken(c, s.source) }
      if (s.type === 'size') { combo.filter((v) => !v.color && !v.image_url).forEach((v) => { out += optToken(v, s.source) }) }
    }
    return out
  }

  const addType = (typeId) => { setChosen((c) => c.length < MAX_TYPES ? [...c, { typeId, valueIds: [] }] : c); setAdding(false) }
  const removeType = (typeId) => setChosen((c) => c.filter((x) => x.typeId !== typeId))
  const toggleValue = (typeId, valueId) => setChosen((c) => c.map((x) => x.typeId !== typeId ? x : {
    ...x, valueIds: x.valueIds.includes(valueId) ? x.valueIds.filter((v) => v !== valueId) : [...x.valueIds, valueId],
  }))

  const setRow = (key, patch) => setRowData((d) => ({ ...d, [key]: { ...d[key], ...patch } }))
  const rowOf = (key) => rowData[key] || { price: '', compareAt: '', stock: '0', sku: '', barcode: '' }

  // Add a brand-new value to a type inline (e.g. a color/size not yet defined),
  // persist it, then auto-select it for this product.
  const addValue = async (typeId, label) => {
    const l = label.trim()
    if (!l) return
    const t = typeById[typeId]
    try {
      const v = await api.createVariantValue(typeId, { label: l, color: t?.selection_style === 'color' ? '#d3ccc1' : null, sort_order: (t?.values || []).length })
      setTypes((ts) => ts.map((x) => x.id === typeId ? { ...x, values: [...(x.values || []), v] } : x))
      toggleValue(typeId, v.id)
    } catch (e) { setError(e.message || 'Değer eklenemedi') }
  }

  // --- category attribute handlers ---
  const selectAttrVal = (attrId, valId) => setAttrPick((p) => ({ ...p, [attrId]: valId }))
  const addAttrValue = async (attrId, name) => {
    const n = name.trim(); if (!n) return
    const v = await api.createAttributeValue(attrId, { name: n })
    setAttrVals((m) => ({ ...m, [attrId]: [...(m[attrId] || []), v] }))
    setAttrPick((p) => ({ ...p, [attrId]: v.id }))
  }
  const assignAttribute = async (attrId) => {
    const ca = await api.assignCategoryAttribute(categoryId, { attribute_id: attrId, required: false, sort_order: catAttrs.length })
    setCatAttrs((c) => [...c, ca])
    const vs = await api.listAttributeValues(attrId).catch(() => [])
    setAttrVals((m) => ({ ...m, [attrId]: vs }))
  }
  const createAndAssignAttribute = async (name) => {
    const n = name.trim(); if (!n) return
    const a = await api.createAttribute({ name: n })
    setAllAttrs((xs) => [...xs, a])
    await assignAttribute(a.id)
  }

  // Tanımlı fiyat alanları: dolu girilenleri oluşturulan TÜM kalemlere yaz
  // (ayraç birden çok ürün üretebilir). Yanıt kalem id'si içermiyorsa üründen
  // çekilir. Kısmi hata ürünü geri almaz — true dönerse çağıran uyarı gösterir.
  const writeDefPrices = async (created) => {
    const filled = priceDefs
      .filter((d) => (defPrices[d.id] || '').trim())
      .map((d) => ({ id: d.id, amount: parseTrMoney(defPrices[d.id]) }))
    if (filled.length === 0) return false
    let warn = false
    const itemIds = []
    for (const p of created) {
      let its = Array.isArray(p.items) && p.items.every((it) => it?.id) ? p.items : null
      if (!its) {
        its = (await api.getProduct(p.id).catch(() => null))?.items || []
        if (its.length === 0) warn = true
      }
      for (const it of its) if (it?.id) itemIds.push(it.id)
    }
    const results = await Promise.allSettled(itemIds.flatMap((itemId) =>
      filled.map((d) => api.putItemPrice(itemId, d.id, { amount: d.amount, currency: 'TRY' }))))
    return warn || results.some((r) => r.status === 'rejected')
  }

  const totalVariants = mode === 'variant' ? combos.length : 1
  // Slicer-aware summary: a slicer axis splits into one product per value.
  const usedTypes = activeChosen.map((c) => typeById[c.typeId])
  const slicerSel = mode === 'variant' ? usedTypes.findIndex((t) => t?.slicer) : -1
  const productCount = slicerSel !== -1 && combos.length ? new Set(combos.map((c) => c[slicerSel]?.id)).size : 1
  const buildSummary = slicerSel !== -1 && combos.length ? `${productCount} ürün · ${totalVariants} varyant` : `${totalVariants} varyant`

  // Ayraç türü + bu üründe seçili değerleri — "X bazında ad ve stok kodu" paneli için.
  const slicerType = slicerSel !== -1 ? usedTypes[slicerSel] : null
  // Ayraç değeri ürün adı: elle girilmişse o, değilse ürün adından türet (konum Ayarlar'dan).
  const derivedSplitName = (label) => {
    const t = title.trim()
    if (!t) return label
    return namePos === 'prefix' ? `${label} ${t}` : `${t} - ${label}`
  }
  const splitNameOf = (v) => splitNames[v.id] ?? derivedSplitName(v.label)
  const setSplitName = (id, text) =>
    setSplitNames((m) => { const n = { ...m }; if (text === '') delete n[id]; else n[id] = text; return n })
  const slicerValues = slicerType
    ? (slicerType.values || []).filter((v) => activeChosen[slicerSel].valueIds.includes(v.id))
    : []

  const save = async () => {
    setError('')
    if (!title.trim()) { setError('Ürün başlığı gerekli.'); return }
    if (!categoryId) { setError('Kategori seç — her ürün bir kategoriye bağlı olmalı.'); return }
    if (!skuOn && !groupCode.trim()) { setError('Ürün kodu gerekli — elle girin ya da Ayarlar\'dan ürün kodu üreticisini açın.'); return }
    const missingAttrs = catAttrs.filter((ca) => ca.required && !attrPick[ca.attribute_id])
    if (missingAttrs.length > 0) { setError(`Zorunlu özellikleri doldurun: ${missingAttrs.map((a) => a.name).join(', ')}`); return }

    let product
    if (mode === 'simple') {
      product = {
        title: title.trim(), variant_types: [],
        variants: [{
          sku: simple.sku.trim(), barcode: simple.barcode.trim(),
          price: parseTrMoney(simple.price || '0'),
          compare_at_price: simple.compareAt ? parseTrMoney(simple.compareAt) : null,
          stock: parseInt(simple.stock, 10) || 0, options: [],
        }],
      }
    } else {
      if (combos.length === 0) { setError('En az bir varyant türü ekleyip değer seçmelisin.'); return }
      const used = activeChosen.map((c) => typeById[c.typeId])
      product = {
        title: title.trim(),
        variant_types: used.map((t) => ({ id: t.id, name: t.name, selection_style: t.selection_style })),
        variants: combos.map((combo) => {
          const key = comboKey(combo)
          const r = rowOf(key)
          return {
            sku: (r.sku || '').trim(),
            barcode: (r.barcode || '').trim(),
            price: parseTrMoney(r.price || '0'),
            compare_at_price: r.compareAt ? parseTrMoney(r.compareAt) : null,
            stock: parseInt(r.stock, 10) || 0,
            options: combo.map((v, i) => ({
              type_id: used[i].id, type_name: used[i].name,
              value_id: v.id, value_label: v.label, color: v.color || '', image_url: v.image_url || '', key: v.key || '',
            })),
          }
        }),
      }
    }

    // Category attribute selections → product.attribute_values (used by products:batch).
    product.attribute_values = catAttrs
      .filter((ca) => attrPick[ca.attribute_id])
      .map((ca) => ({
        attribute_id: ca.attribute_id,
        attribute_value_id: attrPick[ca.attribute_id],
        value: (attrVals[ca.attribute_id] || []).find((x) => x.id === attrPick[ca.attribute_id])?.name || null,
      }))

    // SKU generator: send per-product manual inputs; require them.
    if (skuOn) {
      for (let i = 0; i < skuCfg.segments.length; i++) {
        const s = skuCfg.segments[i]
        if (s.type === 'manual' && !(codeInputs[i] || '').trim()) {
          setError(`Ürün kodu için "${s.label || 'değer'}" alanını doldur.`); return
        }
      }
      product.code_inputs = skuCfg.segments.map((s, i) => s.type === 'manual' ? (codeInputs[i] || '').trim() : '')

      // "Key" kaynaklı varyant segmentleri, seçili her değerde bir key gerektirir
      // (key boşsa backend addan otomatik üretir; yine de boş kalmasına izin verme).
      if (mode === 'variant') {
        for (const s of skuCfg.segments) {
          if (!isVarSeg(s.type) || s.source === 'name') continue
          const wantColor = s.type === 'color'
          for (const c of activeChosen) {
            const t = typeById[c.typeId]
            if ((t?.selection_style === 'color') !== wantColor) continue
            const missing = (t.values || []).filter((v) => c.valueIds.includes(v.id)).find((v) => !v.key)
            if (missing) { setError(`"${missing.label}" için key yok — Varyantlar'dan key ekleyin ya da Ayarlar'da kaynağı "Ad" yapın.`); return }
          }
        }
      }
    } else {
      product.product_sku = groupCode.trim() // generator off → the entered code is the SKU
    }

    // Barcode is required unless the generator is on (no silent auto anymore).
    if (!bcOn && product.variants.some((v) => !v.barcode)) {
      setError('Her varyanta barkod girin ya da Ayarlar\'dan barkod üreticisini açın.'); return
    }

    // Map the builder's internal product to the .NET products:batch shape.
    // group_id is a shared "model" id (not an FK); the backend splits by the
    // slicer variant type (read from the DB) into one product per slicer value.
    const netProduct = {
      category_id: categoryId,
      brand_id: brandId || null,
      model_code: skuOn ? '' : (product.product_sku || groupCode || '').trim(),
      code_inputs: skuOn
        ? skuCfg.segments.map((s, i) => s.type === 'manual' ? (codeInputs[i] || '').trim() : '')
        : undefined,
      name: title.trim(),
      description: description.trim() || null,
      status,
      attribute_values: (product.attribute_values || []).map((a) => ({ attribute_id: a.attribute_id, attribute_value_id: a.attribute_value_id })),
      variants: (product.variant_types || []).map((t) => ({ id: t.id, name: t.name, selection_style: t.selection_style })),
      items: product.variants.map((v) => ({
        sku: skuOn ? null : (v.sku || null),
        barcode: v.barcode,
        price: v.price,
        compare_at_price: v.compare_at_price,
        stock: v.stock,
        variant_values: (v.options || []).map((o) => ({ variant_id: o.type_id, variant_value_id: o.value_id })),
      })),
    }

    // Ayraç değeri başına ürün adı — ekranda görünen ad aynen gönderilir
    // (elle girilmiş ya da konum tercihine göre türetilmiş). Stok kodu backend'e bırakılır.
    if (mode === 'variant' && slicerType) {
      netProduct.splits = slicerValues.map((v) => ({
        value_name: v.label,
        name: splitNameOf(v).trim() || null,
        model_code: null,
      }))
    }

    const payload = { group_id: crypto.randomUUID(), products: [netProduct] }
    setSaving(true)
    try {
      const res = await api.productsBatch(payload)
      const created = res.products || []
      const itemCount = created.reduce((a, p) => a + (p.items?.length || p.variants?.length || 0), 0)
      // Tanımlı fiyat alanlarını navigasyondan ÖNCE yaz (onSaved ürün listesine götürür).
      const priceWarn = await writeDefPrices(created)
      onSaved?.(`${created.length} ürün · ${itemCount} varyant oluşturuldu.`)
      if (priceWarn) onToast?.({ tone: 'danger', title: 'Bazı fiyat alanları kaydedilemedi', body: 'Ürün oluşturuldu; eksik fiyatları ürün detayından girebilirsiniz.' })
    } catch (e) {
      setError(e.message || 'Kaydedilemedi')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="page" style={{ maxWidth: 1040 }}>
      <PageHeader
        crumbs={[{ label: 'Ürünler', onClick: () => onNavigate('products') }, { label: 'Ürün Oluştur' }]}
        eyebrow="Tek yazma yolu · products:batch"
        title="Ürün Oluştur"
        help="product-builder"
        sub="Basit ürün ya da varyantlı ürün (Varyantlar'dan tür seç, kombinasyonlar otomatik üretilir)."
        actions={<>
          <Button variant="secondary" onClick={() => onNavigate('products')}>İptal</Button>
          <Button variant="primary" iconLeft={I('check')} onClick={save} loading={saving}>Kaydet</Button>
        </>}
      />

      {error && <div style={{ marginBottom: 16 }}><Banner tone="danger" title="Kaydedilemedi">{error}</Banner></div>}

      <div className="builder">
        {/* 1 — TEMEL */}
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('folder')}</span>
            <div><div className="bnode__title">1 · Temel bilgiler</div><div className="list-meta">Başlık, kategori, açıklama, durum</div></div>
            <div style={{ marginLeft: 'auto' }}>
              <div className="seg">
                <button data-active={status === 'draft'} onClick={() => setStatus('draft')}>Taslak</button>
                <button data-active={status === 'active'} onClick={() => setStatus('active')}>Aktif</button>
              </div>
            </div>
          </div>
          <div className="bnode__body">
            <div className="fieldgrid">
              <Field label="Başlık" required>
                <Input value={title} onChange={(e) => setTitle(e.target.value)} />
              </Field>
              <Field label="Kategori" required>
                <Select placeholder="Seç…" value={categoryId} onChange={(e) => setCategoryId(e.target.value)}
                  options={categories.map((c) => ({ value: c.id, label: c.name }))} />
              </Field>
              <Field label="Marka">
                <Select placeholder="Seç…" value={brandId} onChange={(e) => setBrandId(e.target.value)}
                  options={brands.map((b) => ({ value: b.id, label: b.name }))} />
              </Field>
              {!skuOn && (
                <Field label="Ürün kodu" required auto="Ayarlar'dan üretici açılırsa otomatik gelir">
                  <Input mono value={groupCode} onChange={(e) => setGroupCode(e.target.value)} />
                </Field>
              )}
            </div>

            <div style={{ marginTop: 14 }}>
              <Field label="Ürün açıklaması" optional>
                <Textarea rows={4} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Ürün açıklaması…" />
              </Field>
            </div>

            {skuOn && (
              <div style={{ marginTop: 14, padding: 12, background: 'var(--surface-subtle)', border: '1px solid var(--border-subtle)', borderRadius: 'var(--radius-md)' }}>
                <div className="hstack" style={{ gap: 6, marginBottom: 10 }}>{I('wand-2', { size: 15 })}<span style={{ fontWeight: 600, color: 'var(--text-strong)' }}>Ürün kodu (Otomatik)</span><span className="list-meta">Ayarlar'daki şablona göre</span></div>
                <div className="hstack" style={{ gap: 12, flexWrap: 'wrap', alignItems: 'flex-end' }}>
                  {skuCfg.segments.map((s, i) => {
                    if (isVarSeg(s.type)) return null
                    const isManual = s.type === 'manual'
                    const caption = s.label || SEG_NAME[s.type] || 'Segment'
                    return (
                      <div key={i} style={{ width: isManual ? 140 : 'auto' }}>
                        <div className="list-meta" style={{ marginBottom: 4 }}>{caption}{isManual && <span style={{ color: 'var(--danger-fg)' }}> *</span>}</div>
                        {isManual
                          ? <Input size="sm" mono value={codeInputs[i] || ''} onChange={(e) => setCodeInputs((m) => ({ ...m, [i]: e.target.value }))} />
                          : <span className="typechip" style={{ display: 'inline-block' }}>{skuToken(s, i)}{s.type === 'counter' ? ' ↑' : ''}</span>}
                      </div>
                    )
                  })}
                </div>
                <div className="list-meta" style={{ marginTop: 10 }}>Önizleme: <span className="mono pim-td-strong">{productCodePreview || '—'}</span>{mode === 'variant' && <span> · varyant SKU'suna renk/beden kodu eklenir</span>}</div>
              </div>
            )}
          </div>
        </div>

        {/* 2 — TİP */}
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('package')}</span>
            <div><div className="bnode__title">2 · Ürün tipi</div><div className="list-meta">Basit tek SKU, varyantlı çoklu kombinasyon</div></div>
            <span className="pim-badge pim-badge--count" style={{ marginLeft: 'auto' }}>{buildSummary}</span>
          </div>
          <div className="bnode__body">
            <div className="style-seg" style={{ maxWidth: 420, marginBottom: 16 }}>
              <div className="style-card" data-active={mode === 'simple'} onClick={() => setMode('simple')}>{I('box')} Basit ürün</div>
              <div className="style-card" data-active={mode === 'variant'} onClick={() => setMode('variant')}>{I('layers')} Varyantlı ürün</div>
            </div>

            {mode === 'simple' ? (
              <>
                <div className="fieldgrid">
                  <Field label="SKU" optional={!skuOn} auto={skuOn ? 'şablondan otomatik üretilir' : undefined}><Input mono readOnly={skuOn} title={skuOn ? 'Şablondan otomatik üretilir (Ayarlar → Ürün Kodu Oluşturucu)' : undefined} value={skuOn ? (productCodePreview || '') : simple.sku} onChange={(e) => { if (!skuOn) setSimple((s) => ({ ...s, sku: e.target.value })) }} placeholder={skuOn ? 'otomatik' : 'opsiyonel'} /></Field>
                  <Field label="Barkod" required={!bcOn} auto={bcOn ? 'otomatik üretilir' : "elle gir ya da Ayarlar'dan üreticiyi aç"}><Input mono value={simple.barcode} onChange={(e) => setSimple((s) => ({ ...s, barcode: e.target.value }))} placeholder={bcOn ? 'otomatik' : 'zorunlu'} /></Field>
                  <Field label="Satış fiyatı" help="Kendi siteniz için genel fiyat. Pazaryeri (Trendyol, Hepsiburada…) fiyatlarını ürün detayından kanal bazında ekleyebilirsiniz."><Input mono suffix="₺" value={simple.price} onChange={(e) => setSimple((s) => ({ ...s, price: e.target.value }))} placeholder="0,00" /></Field>
                  <Field label="Karşılaştırma fiyatı" help="İndirim öncesi üstü çizili gösterilecek fiyat."><Input mono suffix="₺" value={simple.compareAt} onChange={(e) => setSimple((s) => ({ ...s, compareAt: e.target.value }))} placeholder="—" /></Field>
                  <Field label="Stok"><Input mono value={simple.stock} onChange={(e) => setSimple((s) => ({ ...s, stock: e.target.value }))} /></Field>
                </div>
                <DefPriceFields defs={priceDefs} values={defPrices} setValues={setDefPrices} />
              </>
            ) : (
              <>
                <VariantSection
                  types={types} chosen={chosen} typeById={typeById} availableToAdd={availableToAdd}
                  adding={adding} setAdding={setAdding} addType={addType} removeType={removeType} toggleValue={toggleValue} addValue={addValue}
                  combos={combos} activeChosen={activeChosen} rowOf={rowOf} setRow={setRow}
                  skuOn={skuOn} bcOn={bcOn} variantSkuPreview={variantSkuPreview}
                  splitNameOf={splitNameOf} setSplitName={setSplitName}
                />
                <DefPriceFields defs={priceDefs} values={defPrices} setValues={setDefPrices} />
              </>
            )}
          </div>
        </div>

        {/* 3 — ÖZELLİKLER (kategoriye göre) */}
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('tags')}</span>
            <div><div className="bnode__title">3 · Özellikler</div><div className="list-meta">Seçilen kategorinin özellikleri — değer seç ya da yeni ekle</div></div>
            {categoryId && <span className="pim-badge pim-badge--count" style={{ marginLeft: 'auto' }}>{catAttrs.length} özellik</span>}
          </div>
          <div className="bnode__body">
            <CategoryAttributesSection
              categoryId={categoryId} catAttrs={catAttrs} attrVals={attrVals} attrPick={attrPick} allAttrs={allAttrs}
              selectAttrVal={selectAttrVal} addAttrValue={addAttrValue} assignAttribute={assignAttribute} createAndAssignAttribute={createAndAssignAttribute}
              onError={setError}
            />
          </div>
        </div>
      </div>

      <div className="between" style={{ marginTop: 18, padding: '14px 16px', background: 'var(--surface)', border: '1px solid var(--border-default)', borderRadius: 'var(--radius-lg)' }}>
        <div className="list-meta">
          <span style={{ color: 'var(--text-strong)', fontWeight: 600 }}>{buildSummary}</span> oluşturulacak · durum: <StatusBadge status={status} />
        </div>
        <div className="hstack">
          <Button variant="secondary" onClick={() => onNavigate('products')}>İptal</Button>
          <Button variant="primary" iconLeft={I('check')} onClick={save} loading={saving}>Kaydet</Button>
        </div>
      </div>
    </div>
  )
}

// Tanımlı fiyat alanları (Tanımlar → Fiyatlar): ürün seviyesinde opsiyonel tutarlar;
// kayıttan sonra oluşturulan TÜM kalemlere yazılır. Tanım yoksa hiçbir şey çizilmez.
function DefPriceFields({ defs, values, setValues }) {
  if (defs.length === 0) return null
  return (
    <div style={{ marginTop: 14, padding: 12, background: 'var(--surface-subtle)', border: '1px solid var(--border-subtle)', borderRadius: 'var(--radius-md)' }}>
      <div className="hstack" style={{ gap: 6, marginBottom: 10 }}>{I('banknote', { size: 15 })}<span style={{ fontWeight: 600, color: 'var(--text-strong)' }}>Diğer fiyat alanları</span><span className="list-meta">Tanımlar → Fiyatlar</span></div>
      <div className="hstack" style={{ gap: 12, flexWrap: 'wrap', alignItems: 'flex-end' }}>
        {defs.map((d) => (
          <Field key={d.id} label={d.name}>
            <Input mono suffix="₺" value={values[d.id] || ''} placeholder="—" style={{ width: 130 }}
              onChange={(e) => setValues((m) => ({ ...m, [d.id]: e.target.value }))} />
          </Field>
        ))}
      </div>
      <div className="list-meta" style={{ marginTop: 10 }}>Bu alanlar tüm varyantlara uygulanır; varyant bazında ürün detayından değiştirebilirsiniz.</div>
    </div>
  )
}

// Ayraç değeri başına ad/stok kodu override'ları: ayraç her değeri ayrı ürüne
// böldüğünden, oluşacak her ürünün adı ve stok kodu burada özelleştirilebilir.
// Boş bırakılanları backend türetir; üretici açıkken stok kodu şablondan gelir.
function CategoryAttributesSection({ categoryId, catAttrs, attrVals, attrPick, allAttrs, selectAttrVal, addAttrValue, assignAttribute, createAndAssignAttribute, onError }) {
  const [addingFor, setAddingFor] = useState(null) // attribute_id whose value-add input is open
  const [valDraft, setValDraft] = useState('')
  const [assignOpen, setAssignOpen] = useState(false)
  const [newAttr, setNewAttr] = useState('')

  if (!categoryId) {
    return <Banner tone="info" title="Önce kategori seç">1 · Temel bilgiler'den kategori seçince o kategorinin özellikleri burada listelenir; değer seçebilir, yeni değer/özellik ekleyebilirsin.</Banner>
  }
  const unassigned = allAttrs.filter((a) => !catAttrs.some((ca) => ca.attribute_id === a.id))
  const commitVal = async (attrId) => { try { await addAttrValue(attrId, valDraft) } catch (e) { onError?.(e.message) } setValDraft(''); setAddingFor(null) }
  const commitNewAttr = async () => { if (!newAttr.trim()) return; try { await createAndAssignAttribute(newAttr) } catch (e) { onError?.(e.message) } setNewAttr(''); setAssignOpen(false) }

  return (
    <div className="stack" style={{ gap: 14 }}>
      {catAttrs.length === 0 && <div className="list-meta">Bu kategoride henüz özellik yok — aşağıdan ekleyebilirsin.</div>}

      {catAttrs.map((ca) => {
        const vals = attrVals[ca.attribute_id] || []
        return (
          <div key={ca.category_attribute_id} className="vtype">
            <div className="between" style={{ marginBottom: 8 }}>
              <span style={{ fontWeight: 600, color: 'var(--text-strong)' }}>{ca.name}{ca.required && <span style={{ color: 'var(--danger-fg)' }}> *</span>}</span>
            </div>
            <div className="chipset" style={{ alignItems: 'center' }}>
              {vals.map((v) => (
                <span key={v.id} className="sizechip" data-on={attrPick[ca.attribute_id] === v.id}
                  onClick={() => selectAttrVal(ca.attribute_id, attrPick[ca.attribute_id] === v.id ? '' : v.id)}>{v.name}</span>
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
              <span key={a.id} className="sizechip" onClick={async () => { try { await assignAttribute(a.id) } catch (e) { onError?.(e.message) } setAssignOpen(false) }}>{a.name}</span>
            ))}
            {unassigned.length === 0 && <span className="list-meta">Tüm mevcut özellikler atanmış — aşağıdan yeni oluştur.</span>}
          </div>
          <div className="hstack" style={{ marginTop: 10, gap: 8 }}>
            <span className="enter-field" style={{ flex: 1, maxWidth: 260 }}>
              <Input size="sm" value={newAttr} onChange={(e) => setNewAttr(e.target.value)} placeholder="Yeni özellik adı (örn. Kumaş)"
                onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); commitNewAttr() } }} />
            </span>
            <Button size="sm" variant="secondary" onClick={commitNewAttr}>Oluştur & ekle</Button>
            <Button size="sm" variant="ghost" onClick={() => { setAssignOpen(false); setNewAttr('') }}>Kapat</Button>
          </div>
        </div>
      ) : (
        <div><Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={() => setAssignOpen(true)}>Özellik ekle</Button></div>
      )}
    </div>
  )
}

function VariantSection({ types, chosen, typeById, availableToAdd, adding, setAdding, addType, removeType, toggleValue, addValue, combos, activeChosen, rowOf, setRow, skuOn, bcOn, variantSkuPreview, splitNameOf, setSplitName }) {
  const [newValFor, setNewValFor] = useState(null)
  const [valDraft, setValDraft] = useState('')
  if (types.length === 0) {
    return <Banner tone="info" title="Varyant türü yok">Önce <strong>Varyantlar</strong> ekranından tür (Renk, Beden…) ve değerlerini ekle.</Banner>
  }
  const commitVal = (typeId) => { addValue(typeId, valDraft); setValDraft(''); setNewValFor(null) }
  return (
    <div className="stack" style={{ gap: 14 }}>
      {chosen.map((c) => {
        const t = typeById[c.typeId]
        if (!t) return null
        const isColor = t.selection_style === 'color'
        return (
          <div key={c.typeId} className="vtype">
            <div className="between" style={{ marginBottom: 8 }}>
              <span style={{ fontWeight: 600, color: 'var(--text-strong)' }}>{t.name}</span>
              <button className="tb__icon" style={{ width: 28, height: 28 }} title="Türü kaldır" onClick={() => removeType(c.typeId)}>{I('trash-2')}</button>
            </div>
            <div className="chipset" style={{ alignItems: 'center' }}>
              {(t.values || []).map((v) => (
                <span key={v.id} className="sizechip" data-on={c.valueIds.includes(v.id)} onClick={() => toggleValue(c.typeId, v.id)}
                  style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
                  {isColor && <span className="swatch-sm" style={swatchOf(v)} />}{v.label}
                </span>
              ))}
              {newValFor === c.typeId ? (
                <span className="enter-field" style={{ display: 'inline-block', width: 150 }}>
                  <Input size="sm" autoFocus value={valDraft} onChange={(e) => setValDraft(e.target.value)}
                    onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); commitVal(c.typeId) } if (e.key === 'Escape') { setNewValFor(null); setValDraft('') } }}
                    onBlur={() => commitVal(c.typeId)} placeholder="Yeni değer" />
                </span>
              ) : (
                <span className="sizechip sizechip--add" onClick={() => { setNewValFor(c.typeId); setValDraft('') }}>{I('plus', { size: 13 })} Ekle</span>
              )}
            </div>
          </div>
        )
      })}

      {chosen.length < MAX_TYPES && (
        adding ? (
          <div className="vtype">
            <div className="list-meta" style={{ marginBottom: 8 }}>Varyant türü seç</div>
            <div className="chipset">
              {availableToAdd.map((t) => (
                <span key={t.id} className="sizechip" onClick={() => addType(t.id)}>{t.name}</span>
              ))}
              {availableToAdd.length === 0 && <span className="list-meta">Eklenecek başka tür yok.</span>}
            </div>
          </div>
        ) : (
          <div><Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={() => setAdding(true)} disabled={availableToAdd.length === 0}>Varyant ekle</Button>
            <span className="list-meta" style={{ marginLeft: 10 }}>en fazla {MAX_TYPES} tür</span></div>
        )
      )}

      {combos.length > 0 && (() => {
        const used = activeChosen.map((c) => typeById[c.typeId])
        const slicerIdx = used.findIndex((t) => t?.slicer)

        const renderRow = (combo) => {
          const key = comboKey(combo)
          const r = rowOf(key)
          const labels = combo.filter((_, i) => i !== slicerIdx)
          return (
            <div className="variant-row" key={key} style={{ gridTemplateColumns: VAR_COLS, minWidth: 720 }}>
              <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
                {labels.length === 0
                  ? <span className="list-meta">tek varyant</span>
                  : labels.map((v) => {
                    const ti = combo.indexOf(v)
                    return (
                      <span key={v.id} className="pim-badge" style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                        {used[ti]?.selection_style === 'color' && <span className="swatch-sm" style={swatchOf(v)} />}{v.label}
                      </span>
                    )
                  })}
              </span>
              <Input size="sm" mono suffix="₺" value={r.price} onChange={(e) => setRow(key, { price: e.target.value })} placeholder="0,00" />
              <Input size="sm" mono suffix="₺" value={r.compareAt} onChange={(e) => setRow(key, { compareAt: e.target.value })} placeholder="—" />
              <Input size="sm" mono value={r.stock} onChange={(e) => setRow(key, { stock: e.target.value })} />
              <Input size="sm" mono readOnly={skuOn} title={skuOn ? 'Şablondan otomatik üretilir (Ayarlar → Ürün Kodu Oluşturucu)' : undefined}
                value={skuOn ? (variantSkuPreview(combo) || '') : r.sku}
                onChange={(e) => { if (!skuOn) setRow(key, { sku: e.target.value }) }} placeholder={skuOn ? 'otomatik' : 'opsiyonel'} />
              <Input size="sm" mono value={r.barcode} onChange={(e) => setRow(key, { barcode: e.target.value })} placeholder={bcOn ? 'ayrılıyor…' : 'zorunlu'} />
            </div>
          )
        }

        let body
        if (slicerIdx === -1) {
          body = combos.map(renderRow)
        } else {
          const slicerType = used[slicerIdx]
          const groupVals = (slicerType.values || []).filter((v) => combos.some((c) => c[slicerIdx]?.id === v.id))
          body = groupVals.map((sv, gi) => {
            const rows = combos.filter((c) => c[slicerIdx]?.id === sv.id)
            return (
              <div key={sv.id} style={{ borderTop: gi > 0 ? '1px solid var(--border-subtle)' : 'none', marginTop: gi > 0 ? 6 : 0, paddingTop: gi > 0 ? 8 : 0 }}>
                <div className="hstack" style={{ gap: 8, padding: '4px 2px 8px' }}>
                  {slicerType.selection_style === 'color' && <span className="swatch-sm" style={swatchOf(sv)} />}
                  <span style={{ fontWeight: 700, color: 'var(--text-strong)' }}>{sv.label}</span>
                  <span className="pim-badge" style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 11 }}>{I('scissors', { size: 11 })} ayrı ürün</span>
                  {/* Bu değere ait ürünün adı — ürün adından canlı türetilir, elle değiştirilebilir. */}
                  <span className="enter-field" style={{ flex: 1, maxWidth: 380, marginLeft: 8 }}>
                    <Input size="sm" value={splitNameOf(sv)} onChange={(e) => setSplitName(sv.id, e.target.value)}
                      title="Bu ürünün adı — ürün adına göre otomatik güncellenir; elle değiştirebilirsin (konum tercihi: Ayarlar)" />
                  </span>
                  <span className="list-meta" style={{ marginLeft: 'auto' }}>{rows.length} varyant</span>
                </div>
                {rows.map(renderRow)}
              </div>
            )
          })
        }

        return (
          <div style={{ border: '1px solid var(--border-subtle)', borderRadius: 'var(--radius-md)', padding: '4px 12px 10px', overflowX: 'auto' }}>
            <div className="variant-row variant-row__head" style={{ gridTemplateColumns: VAR_COLS, minWidth: 720 }}>
              <span>Varyant</span><span title="Kendi siteniz için genel fiyat; pazaryeri fiyatları ürün detayından eklenir">Satış ₺ (genel)</span><span>Karşılaştırma</span><span>Stok</span><span>SKU</span><span>Barkod</span>
            </div>
            {body}
            <div className="list-meta" style={{ marginTop: 8 }}>{I('info', { size: 13 })} {slicerIdx !== -1 ? `"${used[slicerIdx].name}" ayraç — her değer ayrı ürün olarak kaydedilir. ` : ''}{bcOn ? 'Barkod otomatik üretilir (Ayarlar).' : 'Barkod zorunlu — elle gir ya da Ayarlar\'dan üreticiyi aç.'} {skuOn ? 'SKU şablona göre otomatik.' : 'SKU opsiyonel.'}</div>
          </div>
        )
      })()}
    </div>
  )
}
