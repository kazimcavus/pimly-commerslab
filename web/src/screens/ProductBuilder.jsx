import React, { useEffect, useMemo, useRef, useState } from 'react'
import { Button, Field, Input, Select, Banner, RichText } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { friendlyError } from '../lib/errors.js'
import { parseTrMoney } from '../lib/format.js'
import { registerNavGuard } from '../lib/navGuard.js'
import { isHtmlEmpty } from '../lib/sanitizeHtml.js'
import { loadSkuConfig } from '../lib/skuConfig.js'
import { AttributeEditor } from './parts/AttributeEditor.jsx'

const MAX_TYPES = 3
const SEG_NAME = { fixed: 'Sabit', counter: 'Sıra', year: 'Yıl', manual: 'Değer' }
const EMPTY_CHAN = { amount: '', compareAt: '' }

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
  const [rowData, setRowData] = useState({}) // { comboKey: {price,compareAt,stock,sku,barcode} }
  const [adding, setAdding] = useState(false)
  // Ayraç değeri başına ürün adı: yalnızca elle değiştirilen girişler tutulur;
  // boş bırakılanlar ürün adından + katalog ayarındaki konum tercihinden türetilir.
  const [splitNames, setSplitNames] = useState({}) // { slicerValueId: text }
  const [namePos, setNamePos] = useState('suffix') // 'suffix' | 'prefix' — backend'den yüklenir

  // Tanımlı fiyat alanları (Tanımlar → Fiyatlar) — tabloda birer sütun.
  const [priceDefs, setPriceDefs] = useState([]) // [{id,name,code}]
  const [defPrices, setDefPrices] = useState({}) // basit ürün: { defId: text }
  const [itemDefPrices, setItemDefPrices] = useState({}) // varyant: { comboKey: { defId: text } }

  // Kanal (pazaryeri) fiyat sütunları — bağlı pazaryerleri; create sonrası
  // Pricing channel-price uçlarına yazılır.
  const [marketplaces, setMarketplaces] = useState([])
  const [itemChannels, setItemChannels] = useState({}) // { comboKey: { code: {amount, compareAt} } }
  const [simpleChannels, setSimpleChannels] = useState({}) // { code: {amount, compareAt} }

  // Tenant settings: SKU generator + barcode config.
  const [skuCfg, setSkuCfg] = useState({ enabled: false, segments: [] })
  const [bcOn, setBcOn] = useState(false)
  const [codeInputs, setCodeInputs] = useState({}) // { segIndex: value } for manual

  // Kategori özellikleri: AttributeEditor yükler, zorunlu doğrulaması için
  // meta buraya bildirilir; seçim attrPick'te tutulur.
  const [catAttrs, setCatAttrs] = useState([])     // [{category_attribute_id,attribute_id,name,required,...}]
  const [attrPick, setAttrPick] = useState({})     // { attribute_id: valueId }

  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  // Kaydedilmemiş değişiklik koruması: kullanıcı forma bir şey girdiyse
  // ekrandan ayrılmadan önce sorulur (barkod oto-tahsisi kirlilik sayılmaz).
  const dirtyRef = useRef(false)
  useEffect(() => registerNavGuard(() => dirtyRef.current), [])
  dirtyRef.current = !!(
    title.trim() || !isHtmlEmpty(description) || groupCode.trim()
    || chosen.some((c) => c.valueIds.length > 0)
    || Object.values(attrPick).some(Boolean)
    || (mode === 'simple' && (simple.sku || simple.price || simple.compareAt || (simple.stock && simple.stock !== '0')))
  )

  useEffect(() => { api.listCategories().then(setCategories).catch(() => {}) }, [])
  useEffect(() => { api.listBrands().then(setBrands).catch(() => {}) }, [])
  useEffect(() => { api.listPriceDefinitions().then(setPriceDefs).catch(() => {}) }, [])
  useEffect(() => { api.getCatalogSettings().then((s) => setNamePos(s.slicer_name_position || 'suffix')).catch(() => {}) }, [])
  useEffect(() => {
    api.listMarketplaces()
      .then((mps) => setMarketplaces((mps || []).filter((m) => m.is_configured)))
      .catch(() => {})
  }, [])
  // Kategori değişince seçimler sıfırlanır (özellik listesi AttributeEditor'da yüklenir).
  useEffect(() => { setAttrPick({}) }, [categoryId])
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
  // Barkod tahsisi satırı kısmi yaratabilir ({barcode} tek başına) — varsayılanlarla birleştir,
  // yoksa kontrollü input'lar undefined değere düşer.
  const rowOf = (key) => ({ price: '', compareAt: '', stock: '0', sku: '', barcode: '', ...(rowData[key] || {}) })

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
    } catch (e) { setError(friendlyError(e)) }
  }

  // Catalog artık saf PIM: fiyat/stok kaleme inline yazılmaz. Kalemler
  // products:batch ile oluşturulduktan SONRA temel fiyat → Pricing (base-price),
  // stok → Inventory, tanımlı fiyatlar → Pricing (item-price), kanal fiyatları →
  // Pricing (channel-price) uçlarına yazılır. Kalemler BARKOD ile eşlenir (ayraç
  // birden çok ürün üretebilir; yanıt kalem id'si içermiyorsa üründen çekilir).
  // Kısmi hata ürünü geri almaz — true dönerse çağıran uyarı gösterir.
  const writeItemPricingAndStock = async (created, byBarcode) => {
    if (!byBarcode || Object.keys(byBarcode).length === 0) return false
    let warn = false
    const calls = []
    for (const p of created) {
      let its = Array.isArray(p.items) && p.items.every((it) => it?.id) ? p.items : null
      if (!its) {
        its = (await api.getProduct(p.id).catch(() => null))?.items || []
        if (its.length === 0) warn = true
      }
      for (const it of its) {
        const data = byBarcode[(it.barcode || '').trim()]
        if (!data || !it?.id) continue
        calls.push(api.putBasePrice(it.id, { amount: data.price, compare_at_amount: data.compareAt, currency: 'TRY' }))
        calls.push(api.putStock(it.id, { quantity: data.stock }))
        for (const d of data.defs) calls.push(api.putItemPrice(it.id, d.id, { amount: d.amount, currency: 'TRY' }))
        for (const c of data.channels) calls.push(api.putChannelPrice(it.id, c.code, { amount: c.amount, compare_at_amount: c.compareAt, currency: 'TRY' }))
      }
    }
    const results = await Promise.allSettled(calls)
    return warn || results.some((r) => r.status === 'rejected')
  }

  // Barkod → { price, compareAt, stock, defs, channels } haritası (create sonrası yazım için).
  const channelList = (per) => marketplaces
    .filter((m) => (per?.[m.code]?.amount || '').trim())
    .map((m) => ({
      code: m.code,
      amount: parseTrMoney(per[m.code].amount),
      compareAt: (per[m.code].compareAt || '').trim() ? parseTrMoney(per[m.code].compareAt) : null,
    }))
  const buildItemDataByBarcode = () => {
    const map = {}
    if (mode === 'variant') {
      for (const combo of combos) {
        const key = comboKey(combo)
        const r = rowOf(key)
        const bc = (r.barcode || '').trim()
        if (!bc) continue
        const defs = priceDefs
          .filter((d) => (itemDefPrices[key]?.[d.id] || '').trim())
          .map((d) => ({ id: d.id, amount: parseTrMoney(itemDefPrices[key][d.id]) }))
        map[bc] = {
          price: parseTrMoney(r.price || '0'),
          compareAt: r.compareAt ? parseTrMoney(r.compareAt) : null,
          stock: parseInt(r.stock, 10) || 0,
          defs,
          channels: channelList(itemChannels[key]),
        }
      }
    } else {
      const bc = (simple.barcode || '').trim()
      if (bc) {
        const defs = priceDefs
          .filter((d) => (defPrices[d.id] || '').trim())
          .map((d) => ({ id: d.id, amount: parseTrMoney(defPrices[d.id]) }))
        map[bc] = {
          price: parseTrMoney(simple.price || '0'),
          compareAt: simple.compareAt ? parseTrMoney(simple.compareAt) : null,
          stock: parseInt(simple.stock, 10) || 0,
          defs,
          channels: channelList(simpleChannels),
        }
      }
    }
    return map
  }

  // Varyant başına tanımlı fiyat / kanal fiyatı setter'ları + gruba toplu doldurma.
  const setItemDef = (key, defId, value) =>
    setItemDefPrices((m) => ({ ...m, [key]: { ...(m[key] || {}), [defId]: value } }))
  const fillGroupDef = (keys, defId, value) =>
    setItemDefPrices((m) => { const n = { ...m }; keys.forEach((k) => { n[k] = { ...(n[k] || {}), [defId]: value } }); return n })
  const fillGroupRow = (keys, field, value) =>
    setRowData((d) => { const n = { ...d }; keys.forEach((k) => { n[k] = { ...(n[k] || rowOf(k)), [field]: value } }); return n })
  const chanOf = (key, code) => itemChannels[key]?.[code] || EMPTY_CHAN
  const setItemChannel = (key, code, patch) =>
    setItemChannels((m) => ({ ...m, [key]: { ...(m[key] || {}), [code]: { ...(m[key]?.[code] || EMPTY_CHAN), ...patch } } }))
  const fillGroupChannel = (keys, code, field, value) =>
    setItemChannels((m) => { const n = { ...m }; keys.forEach((k) => { n[k] = { ...(n[k] || {}), [code]: { ...(n[k]?.[code] || EMPTY_CHAN), [field]: value } } }); return n })

  const totalVariants = mode === 'variant' ? combos.length : 1
  // Slicer-aware summary: a slicer axis splits into one product per value.
  const usedTypes = activeChosen.map((c) => typeById[c.typeId])
  const slicerSel = mode === 'variant' ? usedTypes.findIndex((t) => t?.slicer) : -1
  const productCount = slicerSel !== -1 && combos.length ? new Set(combos.map((c) => c[slicerSel]?.id)).size : 1

  // Ayraç türü + bu üründe seçili değerleri — renk başına ürün kartları için.
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
          options: [],
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
            options: combo.map((v, i) => ({
              type_id: used[i].id, type_name: used[i].name,
              value_id: v.id, value_label: v.label, color: v.color || '', image_url: v.image_url || '', key: v.key || '',
            })),
          }
        }),
      }
    }

    // SKU generator: send per-product manual inputs; require them.
    if (skuOn) {
      for (let i = 0; i < skuCfg.segments.length; i++) {
        const s = skuCfg.segments[i]
        if (s.type === 'manual' && !(codeInputs[i] || '').trim()) {
          setError(`Ürün kodu için "${s.label || 'değer'}" alanını doldur.`); return
        }
      }

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
      description: isHtmlEmpty(description) ? null : description,
      status,
      attribute_values: catAttrs
        .filter((ca) => attrPick[ca.attribute_id])
        .map((ca) => ({ attribute_id: ca.attribute_id, attribute_value_id: attrPick[ca.attribute_id] })),
      variants: (product.variant_types || []).map((t) => ({ id: t.id, name: t.name, selection_style: t.selection_style })),
      // Catalog kalemi saf PIM: fiyat/stok göndermeyiz (backend yok sayar).
      // Temel fiyat + stok + tanımlı/kanal fiyatları create sonrası ayrı uçlara yazılır.
      items: product.variants.map((v) => ({
        sku: skuOn ? null : (v.sku || null),
        barcode: v.barcode,
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
      // Fiyat + stok yazımını navigasyondan ÖNCE yap (onSaved ürün listesine götürür).
      const priceWarn = await writeItemPricingAndStock(created, buildItemDataByBarcode())
      dirtyRef.current = false // kayıt tamam; ayrılma korumasını bırak
      onSaved?.(`${created.length} ürün · ${itemCount} varyant oluşturuldu.`)
      if (priceWarn) onToast?.({ tone: 'danger', title: 'Bazı fiyat/stok alanları kaydedilemedi', body: 'Ürün oluşturuldu; eksik değerleri ürün detayından girebilirsiniz.' })
    } catch (e) {
      setError(friendlyError(e))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="page" style={{ maxWidth: 1060 }}>
      <PageHeader
        crumbs={[{ label: 'Ürünler', onClick: () => onNavigate('products') }, { label: 'Ürün Oluştur' }]}
        eyebrow="Tek yazma yolu · products:batch"
        title="Ürün Oluştur"
        help="product-builder"
        sub="Basit ya da varyantlı ürün — grup, ürünler ve varyantlar tek kayıtta oluşturulur. Kombinasyonlar seçtiğin değerlerden otomatik üretilir."
      />

      {error && <div style={{ marginBottom: 16 }}><Banner tone="danger" title="Kaydedilemedi">{error}</Banner></div>}

      <div className="builder">
        {/* 1 — TEMEL */}
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('folder')}</span>
            <div><div className="bnode__title">1 · Temel bilgiler</div><div className="list-meta">Başlık, kategori, marka ve ürün kodu</div></div>
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
                <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Örn. El dokuması kilim" />
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
                  <Input mono value={groupCode} onChange={(e) => setGroupCode(e.target.value)} placeholder="TS-0001" />
                </Field>
              )}
            </div>

            <div style={{ marginTop: 14 }}>
              <Field label="Ürün açıklaması" optional>
                <RichText value={description} onChange={setDescription} placeholder="Ürün açıklaması…"
                  uploadImage={(f) => api.uploadImage(f, 'product').then((r) => r.url)} />
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
            <div><div className="bnode__title">2 · Ürün tipi</div><div className="list-meta">Basit tek SKU ya da varyantlı çoklu kombinasyon</div></div>
            <span className="pim-badge pim-badge--count" style={{ marginLeft: 'auto' }}>{productCount} ürün · {totalVariants} varyant</span>
          </div>
          <div className="bnode__body">
            <div className="typetoggle" style={{ marginBottom: 20 }}>
              <button type="button" className="typetoggle__btn" data-active={mode === 'simple'} onClick={() => setMode('simple')}>{I('box', { size: 18 })} Basit ürün</button>
              <button type="button" className="typetoggle__btn" data-active={mode === 'variant'} onClick={() => setMode('variant')}>{I('layers', { size: 18 })} Varyantlı ürün</button>
            </div>

            {mode === 'simple' ? (
              <SimpleTable
                simple={simple} setSimple={setSimple} skuOn={skuOn} bcOn={bcOn} productCodePreview={productCodePreview}
                priceDefs={priceDefs} defPrices={defPrices} setDefPrices={setDefPrices}
                marketplaces={marketplaces} simpleChannels={simpleChannels} setSimpleChannels={setSimpleChannels}
              />
            ) : (
              <VariantSection
                types={types} chosen={chosen} typeById={typeById} availableToAdd={availableToAdd}
                adding={adding} setAdding={setAdding} addType={addType} removeType={removeType} toggleValue={toggleValue} addValue={addValue}
                combos={combos} activeChosen={activeChosen} rowOf={rowOf} setRow={setRow}
                skuOn={skuOn} bcOn={bcOn} variantSkuPreview={variantSkuPreview}
                splitNameOf={splitNameOf} setSplitName={setSplitName}
                priceDefs={priceDefs} itemDefPrices={itemDefPrices} setItemDef={setItemDef}
                fillGroupDef={fillGroupDef} fillGroupRow={fillGroupRow}
                marketplaces={marketplaces} chanOf={chanOf} setItemChannel={setItemChannel} fillGroupChannel={fillGroupChannel}
              />
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
            <AttributeEditor grid categoryId={categoryId} pick={attrPick} onPickChange={setAttrPick}
              onAttrsLoaded={setCatAttrs} onToast={onToast} />
          </div>
        </div>
      </div>

      {/* Sticky Kaydet/İptal */}
      <div className="savebar">
        <div className="savebar__meta">
          <b>{productCount} ürün</b> · <b>{totalVariants} varyant</b> oluşturulacak · durum: <StatusBadge status={status} />
        </div>
        <div className="hstack">
          <Button variant="secondary" onClick={() => onNavigate('products')}>İptal</Button>
          <Button variant="primary" iconLeft={I('check')} onClick={save} loading={saving}>Kaydet</Button>
        </div>
      </div>
    </div>
  )
}

// Fiyat kanalı sütun başlıkları: Genel satış/karş. + her fiyat tanımı + bağlı
// pazaryerlerinin satış/karşılaştırma çiftleri. İki tablo (basit + varyant) paylaşır.
function priceColumns(priceDefs, marketplaces) {
  return [
    { key: 'price', label: 'Genel satış ₺', ph: '0,00' },
    { key: 'compareAt', label: 'Genel karş. ₺', ph: '—' },
    ...priceDefs.map((d) => ({ key: `def:${d.id}`, label: `${d.name} ₺`, ph: '—', def: d })),
    ...marketplaces.flatMap((m) => [
      { key: `mp:${m.code}:amount`, label: `${m.name} satış ₺`, ph: '0,00', mp: m, field: 'amount' },
      { key: `mp:${m.code}:compareAt`, label: `${m.name} karş. ₺`, ph: '—', mp: m, field: 'compareAt' },
    ]),
  ]
}

// Basit ürün: tek SKU satırı — stok, barkod, SKU ve tüm fiyat kanalları tek tabloda.
function SimpleTable({ simple, setSimple, skuOn, bcOn, productCodePreview, priceDefs, defPrices, setDefPrices, marketplaces, simpleChannels, setSimpleChannels }) {
  const numStyle = { textAlign: 'right' }
  const cols = priceColumns(priceDefs, marketplaces)
  const colVal = (c) => {
    if (c.def) return defPrices[c.def.id] || ''
    if (c.mp) return simpleChannels[c.mp.code]?.[c.field] || ''
    return simple[c.key]
  }
  const setCol = (c, v) => {
    if (c.def) setDefPrices((m) => ({ ...m, [c.def.id]: v }))
    else if (c.mp) setSimpleChannels((m) => ({ ...m, [c.mp.code]: { ...(m[c.mp.code] || EMPTY_CHAN), [c.field]: v } }))
    else setSimple((s) => ({ ...s, [c.key]: v }))
  }
  return (
    <div style={{ border: '1px solid var(--border-subtle)', borderRadius: 'var(--radius-md)', overflow: 'hidden' }}>
      <div style={{ padding: '12px 15px', background: 'var(--surface-subtle)', borderBottom: '1px solid var(--border-subtle)', fontSize: 13, fontWeight: 600, color: 'var(--text-strong)' }}>
        Tek SKU — stok, barkod ve fiyat kanalları
      </div>
      <div className="vmx-wrap" style={{ border: 'none', borderRadius: 0 }}>
        <table className="vmx">
          <thead>
            <tr>
              <th data-num="true" style={{ width: 110 }}>Stok</th>
              <th style={{ width: 160 }}>Barkod</th>
              <th className="vmx__sep" style={{ width: 170 }}>SKU</th>
              {cols.map((c) => <th key={c.key} data-num="true" style={{ minWidth: 128 }}>{c.label}</th>)}
            </tr>
          </thead>
          <tbody>
            <tr>
              <td><Input size="sm" mono value={simple.stock} style={numStyle} onChange={(e) => setSimple((s) => ({ ...s, stock: e.target.value }))} /></td>
              <td><Input size="sm" mono value={simple.barcode} placeholder={bcOn ? 'otomatik' : 'zorunlu'} onChange={(e) => setSimple((s) => ({ ...s, barcode: e.target.value }))} /></td>
              <td className="vmx__sep">
                <Input size="sm" mono readOnly={skuOn} title={skuOn ? 'Şablondan otomatik üretilir (Ayarlar → Ürün Kodu Oluşturucu)' : undefined}
                  value={skuOn ? (productCodePreview || '') : simple.sku}
                  onChange={(e) => { if (!skuOn) setSimple((s) => ({ ...s, sku: e.target.value })) }}
                  placeholder={skuOn ? 'otomatik' : 'opsiyonel'} />
              </td>
              {cols.map((c) => (
                <td key={c.key}>
                  <Input size="sm" mono suffix="₺" value={colVal(c)} placeholder={c.ph} style={numStyle} onChange={(e) => setCol(c, e.target.value)} />
                </td>
              ))}
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  )
}

// Varyant seçimi: tür başına arama + chip kartı; ardından ayraç (renk) başına ürün kartları.
function VariantSection({ types, chosen, typeById, availableToAdd, adding, setAdding, addType, removeType, toggleValue, addValue, combos, activeChosen, rowOf, setRow, skuOn, bcOn, variantSkuPreview, splitNameOf, setSplitName, priceDefs, itemDefPrices, setItemDef, fillGroupDef, fillGroupRow, marketplaces, chanOf, setItemChannel, fillGroupChannel }) {
  const [newValFor, setNewValFor] = useState(null)
  const [valDraft, setValDraft] = useState('')
  const [queries, setQueries] = useState({}) // typeId -> arama metni
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
        const q = (queries[c.typeId] || '').trim().toLowerCase()
        const values = (t.values || []).filter((v) => !q || (v.label || '').toLowerCase().includes(q))
        return (
          <div key={c.typeId} className="selcard">
            <div className="selcard__head">
              <span className="selcard__title">{t.name}<span className="list-meta">{c.valueIds.length} seçili</span></span>
              <div className="hstack" style={{ gap: 8 }}>
                <span className="selcard__search">
                  <Input size="sm" icon={I('search', { size: 15 })} value={queries[c.typeId] || ''} placeholder={`${t.name} ara…`}
                    onChange={(e) => setQueries((m) => ({ ...m, [c.typeId]: e.target.value }))} />
                </span>
                <button className="tb__icon" style={{ width: 28, height: 28 }} title="Türü kaldır" onClick={() => removeType(c.typeId)}>{I('trash-2')}</button>
              </div>
            </div>
            <div className="chipset" style={{ alignItems: 'center' }}>
              {values.map((v) => (
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
          <div className="selcard">
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

      {combos.length === 0 && activeChosen.length === 0 && chosen.length > 0 && (
        <Banner tone="info" title="En az bir değer seç">Seçtiğin değerlerden varyant satırları otomatik üretilir; ayraç türü her değeri ayrı ürüne böler.</Banner>
      )}

      {combos.length > 0 && (
        <VariantGroups
          combos={combos} activeChosen={activeChosen} typeById={typeById} toggleValue={toggleValue}
          rowOf={rowOf} setRow={setRow} skuOn={skuOn} bcOn={bcOn} variantSkuPreview={variantSkuPreview}
          priceDefs={priceDefs} itemDefPrices={itemDefPrices} setItemDef={setItemDef}
          fillGroupDef={fillGroupDef} fillGroupRow={fillGroupRow}
          splitNameOf={splitNameOf} setSplitName={setSplitName}
          marketplaces={marketplaces} chanOf={chanOf} setItemChannel={setItemChannel} fillGroupChannel={fillGroupChannel}
        />
      )}
    </div>
  )
}

// Varyant düzenleyici: ayraç (renk) başına ürün kartı — kart başında renk yutusu,
// ad, kod chip'i ve kaldırma; gövdede hizalı varyant matrisi.
function VariantGroups({ combos, activeChosen, typeById, toggleValue, rowOf, setRow, skuOn, bcOn, variantSkuPreview, priceDefs, itemDefPrices, setItemDef, fillGroupDef, fillGroupRow, splitNameOf, setSplitName, marketplaces, chanOf, setItemChannel, fillGroupChannel }) {
  const used = activeChosen.map((c) => typeById[c.typeId])
  const slicerIdx = used.findIndex((t) => t?.slicer)
  const slicerType = slicerIdx !== -1 ? used[slicerIdx] : null

  const variantLabel = (combo) => {
    const labels = combo.filter((_, i) => i !== slicerIdx)
    if (labels.length === 0) return <span className="vmx__chip">tek varyant</span>
    return labels.map((v) => {
      const ti = combo.indexOf(v)
      return (
        <span key={v.id} className="vmx__chip" style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}>
          {used[ti]?.selection_style === 'color' && <span className="swatch-sm" style={swatchOf(v)} />}{v.label}
        </span>
      )
    })
  }

  const table = (rows) => (
    <MatrixTable rows={rows} keys={rows.map(comboKey)} variantLabel={variantLabel}
      rowOf={rowOf} setRow={setRow} skuOn={skuOn} bcOn={bcOn} variantSkuPreview={variantSkuPreview}
      priceDefs={priceDefs} itemDefPrices={itemDefPrices} setItemDef={setItemDef}
      fillGroupRow={fillGroupRow} fillGroupDef={fillGroupDef}
      marketplaces={marketplaces} chanOf={chanOf} setItemChannel={setItemChannel} fillGroupChannel={fillGroupChannel} />
  )

  if (slicerIdx === -1) {
    return (
      <div className="vcard">
        <div className="vcard__head vcard__head--plain">
          <span className="vcard__title">Varyantlar</span>
          <span className="list-meta" style={{ marginLeft: 'auto' }}>{combos.length} varyant</span>
        </div>
        <div className="vcard__body">{table(combos)}</div>
      </div>
    )
  }

  const slicerChosen = activeChosen[slicerIdx]
  const groupVals = (slicerType.values || []).filter((v) => combos.some((c) => c[slicerIdx]?.id === v.id))
  return (
    <div className="stack" style={{ gap: 14 }}>
      {groupVals.map((sv) => {
        const rows = combos.filter((c) => c[slicerIdx]?.id === sv.id)
        return (
          <div className="vcard" key={sv.id}>
            <div className="vcard__head">
              <div className="vcard__titlerow">
                {slicerType.selection_style === 'color' && <span className="swatch-sm" style={{ ...swatchOf(sv), width: 18, height: 18, borderRadius: 5 }} />}
                <span className="vcard__title">{sv.label}</span>
                {sv.key && <span className="typechip">{String(sv.key).toUpperCase()}</span>}
                <span className="pim-badge" style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 11 }}>{I('scissors', { size: 11 })} ayrı ürün</span>
                <span className="list-meta" style={{ marginLeft: 'auto' }}>{rows.length} varyant</span>
                <button className="tb__icon" style={{ width: 28, height: 28 }} title={`${sv.label} rengini kaldır`}
                  onClick={() => toggleValue(slicerChosen.typeId, sv.id)}>{I('trash-2')}</button>
              </div>
              <div className="vcard__namerow">
                <Field label="Ürün adı">
                  <Input size="sm" value={splitNameOf(sv)} onChange={(e) => setSplitName(sv.id, e.target.value)}
                    title="Ürün adına göre otomatik türetilir; elle değiştirebilirsin (konum: Ayarlar)" />
                </Field>
              </div>
            </div>
            <div className="vcard__body">{table(rows)}</div>
          </div>
        )
      })}
    </div>
  )
}

// Hizalı varyant matrisi: solda yapışkan Varyant sütunu; Stok | Barkod | SKU ‖
// fiyat kanalları (Genel + tanımlar + pazaryeri satış/karş.). En üstte kesikli
// kenarlıklı toplu-doldur satırı: stok ve fiyatlar gruptaki tüm satırlara yayılır.
function MatrixTable({ rows, keys, variantLabel, rowOf, setRow, skuOn, bcOn, variantSkuPreview, priceDefs, itemDefPrices, setItemDef, fillGroupRow, fillGroupDef, marketplaces, chanOf, setItemChannel, fillGroupChannel }) {
  const numStyle = { textAlign: 'right' }
  const cols = priceColumns(priceDefs, marketplaces)
  const cellVal = (key, c, r) => {
    if (c.def) return itemDefPrices[key]?.[c.def.id] || ''
    if (c.mp) return chanOf(key, c.mp.code)[c.field] || ''
    return r[c.key]
  }
  const setCell = (key, c, v) => {
    if (c.def) setItemDef(key, c.def.id, v)
    else if (c.mp) setItemChannel(key, c.mp.code, { [c.field]: v })
    else setRow(key, { [c.key]: v })
  }
  const fillCol = (c, v) => {
    if (c.def) fillGroupDef(keys, c.def.id, v)
    else if (c.mp) fillGroupChannel(keys, c.mp.code, c.field, v)
    else fillGroupRow(keys, c.key, v)
  }
  return (
    <>
      <div className="vmx-wrap">
        <table className="vmx">
          <thead>
            <tr>
              <th className="vmx__lead" style={{ minWidth: 92 }}>Varyant</th>
              <th data-num="true" style={{ width: 100 }}>Stok</th>
              <th style={{ width: 150 }}>Barkod</th>
              <th className="vmx__sep" style={{ width: 150 }}>SKU</th>
              {cols.map((c) => <th key={c.key} data-num="true" style={{ minWidth: 128 }}>{c.label}</th>)}
            </tr>
          </thead>
          <tbody>
            {/* Toplu doldurma satırı */}
            <tr className="vmx__bulk">
              <td className="vmx__lead"><span className="vmx__bulklabel">{I('sparkles')} Toplu</span></td>
              <td><Input size="sm" mono className="vmx__fill" placeholder="tümü" style={numStyle} onChange={(e) => fillGroupRow(keys, 'stock', e.target.value)} /></td>
              <td />
              <td className="vmx__sep" />
              {cols.map((c) => (
                <td key={c.key}><Input size="sm" mono suffix="₺" className="vmx__fill" placeholder="tümü" style={numStyle} onChange={(e) => fillCol(c, e.target.value)} /></td>
              ))}
            </tr>
            {rows.map((combo) => {
              const key = comboKey(combo)
              const r = rowOf(key)
              return (
                <tr key={key}>
                  <td className="vmx__lead">
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>{variantLabel(combo)}</span>
                  </td>
                  <td><Input size="sm" mono value={r.stock} style={numStyle} onChange={(e) => setRow(key, { stock: e.target.value })} /></td>
                  <td><Input size="sm" mono value={r.barcode} onChange={(e) => setRow(key, { barcode: e.target.value })} placeholder={bcOn ? 'ayrılıyor…' : 'zorunlu'} /></td>
                  <td className="vmx__sep">
                    <Input size="sm" mono readOnly={skuOn} title={skuOn ? 'Şablondan otomatik (Ayarlar → Ürün Kodu)' : undefined}
                      value={skuOn ? (variantSkuPreview(combo) || '') : r.sku}
                      onChange={(e) => { if (!skuOn) setRow(key, { sku: e.target.value }) }} placeholder={skuOn ? 'otomatik' : 'opsiyonel'} />
                  </td>
                  {cols.map((c) => (
                    <td key={c.key}>
                      <Input size="sm" mono suffix="₺" value={cellVal(key, c, r)} placeholder={c.ph} style={numStyle} onChange={(e) => setCell(key, c, e.target.value)} />
                    </td>
                  ))}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
      <div className="list-meta" style={{ marginTop: 9 }}>
        {I('info', { size: 13 })} Solda stok / barkod / SKU sabit; sağda fiyat kanalları. Varyant sütunu kaydırınca yapışık kalır. {bcOn ? 'Barkod boşsa otomatik üretilir.' : 'Barkod zorunludur.'} Pazaryeri sütunları o kanalın yayın fiyatını belirler.
      </div>
    </>
  )
}
