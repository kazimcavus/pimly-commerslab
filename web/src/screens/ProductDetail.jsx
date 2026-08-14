import React, { useEffect, useMemo, useRef, useState } from 'react'
import { Button, Field, Input, Select, Banner, RichText, Tabs } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { friendlyError } from '../lib/errors.js'
import { askConfirm } from '../lib/confirm.jsx'
import { registerNavGuard } from '../lib/navGuard.js'
import { isHtmlEmpty } from '../lib/sanitizeHtml.js'
import { PhotoGallery } from './parts/PhotoGallery.jsx'
import { AttributeEditor } from './parts/AttributeEditor.jsx'

const STATUS_OPTIONS = [
  { value: 'active', label: 'Aktif' },
  { value: 'draft', label: 'Taslak' },
  { value: 'archived', label: 'Arşiv' },
]

const PAGE_SIZES = ['10', '20', '50', 'all']

const parseMoney = (v) => Number(String(v).trim().replace(',', '.'))
const fmtMoney = (n) => (n == null ? '' : String(n).replace('.', ','))

// Kopyalanabilir kod chip'i (model/stok kodu) — 1.5 sn ✓ geri bildirimi.
function CodeChip({ value }) {
  const [copied, setCopied] = useState(false)
  const timer = useRef(null)
  useEffect(() => () => clearTimeout(timer.current), [])
  if (!value) return null
  const copy = () => {
    try { navigator.clipboard?.writeText(value) } catch { /* pano izni yoksa sessiz geç */ }
    setCopied(true)
    clearTimeout(timer.current)
    timer.current = setTimeout(() => setCopied(false), 1500)
  }
  return (
    <button type="button" className="codechip" title="Kopyala" onClick={copy}>
      {value}
      {copied
        ? I('check', { size: 13, strokeWidth: 2.4, style: { color: 'var(--status-active-fg)' } })
        : I('copy', { size: 13, style: { color: 'var(--text-subtle)' } })}
    </button>
  )
}

// Ürün detayı — ürün oluştur ile eşleşen tasarım: kopyalanabilir kodlar, 2:3 görsel
// galerisi, üç sekme (Bilgiler / Varyantlar / Özellikler), sayfalanan varyant tablosu,
// seçime duyarlı toplu yayma ve sticky Kaydet/İptal. Catalog saf PIM olduğundan
// kayıtta yazımlar bölünür: sku/barkod → Catalog, fiyatlar → Pricing, stok → Inventory.
export function ProductDetail({ productId, onNavigate, onToast }) {
  const [product, setProduct] = useState(null)
  const [error, setError] = useState('')
  const [tab, setTab] = useState('bilgiler')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [status, setStatus] = useState('active')
  const [saving, setSaving] = useState(false)
  const [itemEdits, setItemEdits] = useState({})   // itemId -> { sku, barcode, price, compareAt, stock }

  // Varyant tablosu: seçim + sayfalama (toplu yayma seçili satırlara ya da tümüne uygulanır).
  const [selected, setSelected] = useState({})     // itemId -> true
  const [pageSize, setPageSize] = useState('10')
  const [page, setPage] = useState(0)

  // Varyant Ekle formu
  const [adding, setAdding] = useState(false)
  const [axisValues, setAxisValues] = useState({})   // variantId -> [{id,label}]
  const [newItem, setNewItem] = useState({ selections: {}, sku: '', barcode: '', price: '', stock: '0' })
  const [savingNew, setSavingNew] = useState(false)

  // Tanımlı fiyatlar: itemId -> ItemPriceDto[]; matris hücrelerinde düzenlenir.
  const [priceDefs, setPriceDefs] = useState([])      // fiyat tanımları (Tanımlar → Fiyatlar)
  const [itemPrices, setItemPrices] = useState({})
  const [dpEdits, setDpEdits] = useState({})          // `${itemId}:${defId}` -> tutar metni

  // Fiyat/stok artık ayrı modüllerde (Catalog saf PIM). Kalem başına Pricing'den
  // temel fiyat + kanal fiyatları, Inventory'den stok çekilir.
  const [basePrices, setBasePrices] = useState({})    // itemId -> { amount, compare_at_amount } | null
  const [stocks, setStocks] = useState({})            // itemId -> quantity (number)
  const [marketplaces, setMarketplaces] = useState([])   // kanal fiyat sütunları (bağlı pazaryerleri)
  const [channelPrices, setChannelPrices] = useState({}) // itemId -> { [mpCode]: ChannelPriceDto }
  const [cpEdits, setCpEdits] = useState({})          // `${itemId}:${mpCode}` -> tutar metni

  // Düzenlenebilir ürün alanları: marka, kategori, özellik seçimleri.
  const [brandId, setBrandId] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [brands, setBrands] = useState([])
  const [categories, setCategories] = useState([])
  const [attrPick, setAttrPick] = useState({})        // { attribute_id: value_id }
  const [initialAttrPick, setInitialAttrPick] = useState({})
  const [catAttrsMeta, setCatAttrsMeta] = useState([])

  useEffect(() => { api.listBrands().then(setBrands).catch(() => {}) }, [])
  useEffect(() => { api.listCategories().then(setCategories).catch(() => {}) }, [])

  // Kaydedilmemiş değişiklik koruması: kirliyken ekrandan ayrılmadan önce sorulur.
  const anyDirtyRef = useRef(false)
  useEffect(() => registerNavGuard(() => anyDirtyRef.current), [])

  const load = () => {
    if (!productId) return
    api.getProduct(productId)
      .then((p) => {
        setProduct(p); setName(p.name || ''); setDescription(p.description || ''); setStatus(p.status || 'active')
        setBrandId(p.brand_id || ''); setCategoryId(p.category_id || '')
        const pick = Object.fromEntries((p.attribute_values || []).map((av) => [av.attribute?.id, av.id]))
        setAttrPick(pick); setInitialAttrPick(pick)
        setItemEdits({})
      })
      .catch((e) => setError(friendlyError(e)))
  }
  useEffect(() => { load(); setSelected({}); setPage(0) }, [productId])

  const items = useMemo(() => {
    const list = [...(product?.items || [])]
    const label = (it) => (it.variant_values || []).map((v) => v.name).join(' / ')
    return list.sort((a, b) => label(a).localeCompare(label(b), 'tr', { numeric: true }))
  }, [product])

  // Fiyat tanımlarını bir kez yükle (panel satırları + builder ile aynı kaynak).
  useEffect(() => { api.listPriceDefinitions().then(setPriceDefs).catch(() => {}) }, [])

  // Kanal fiyat sütunları için pazaryerlerini bir kez yükle (yalnızca bağlı olanlar).
  useEffect(() => {
    api.listMarketplaces()
      .then((mps) => setMarketplaces((mps || []).filter((m) => m.is_configured)))
      .catch(() => {})
  }, [])

  // Kalem başına fiyat/stok yükle: tanımlı fiyatlar + temel fiyat + kanal fiyatları (Pricing),
  // stok (Inventory). Temel fiyat/stok kaydı yoksa uçlar 404 döner → boş/0 kabul edilir.
  useEffect(() => {
    const its = product?.items || []
    if (its.length === 0) { setItemPrices({}); setBasePrices({}); setStocks({}); setChannelPrices({}); return }
    let alive = true
    Promise.all(its.map(async (it) => {
      const [ip, bp, st, cp] = await Promise.all([
        api.listItemPrices(it.id).then((rows) => Array.isArray(rows) ? rows : rows?.items || []).catch(() => []),
        api.getBasePrice(it.id).catch(() => null),
        api.getStock(it.id).then((s) => s?.quantity ?? 0).catch(() => 0),
        api.listChannelPrices(it.id).then((rows) => Array.isArray(rows) ? rows : rows?.items || []).catch(() => []),
      ])
      return { id: it.id, ip, bp, st, cp }
    })).then((rows) => {
      if (!alive) return
      setItemPrices(Object.fromEntries(rows.map((r) => [r.id, r.ip])))
      setBasePrices(Object.fromEntries(rows.map((r) => [r.id, r.bp])))
      setStocks(Object.fromEntries(rows.map((r) => [r.id, r.st])))
      setChannelPrices(Object.fromEntries(rows.map((r) => [r.id, Object.fromEntries((r.cp || []).map((c) => [c.marketplace, c]))])))
    })
    return () => { alive = false }
  }, [product])

  // Tanım hücresi: kayıtlı değer varsa metni, yoksa boş; düzenleme metni öncelikli.
  const dpValOf = (itemId, def, stored) => {
    const e = dpEdits[`${itemId}:${def.id}`]
    return e !== undefined ? e : fmtMoney(stored?.amount ?? null)
  }
  const setDpEdit = (itemId, def, text) =>
    setDpEdits((cur) => ({ ...cur, [`${itemId}:${def.id}`]: text }))
  const dpDirty = (itemId, def, stored) => {
    const e = dpEdits[`${itemId}:${def.id}`]
    if (e === undefined) return false
    if (!e.trim()) return !!stored // boş bırakıldı: değer varsa "kirli" (kaydet → sil)
    return !stored || parseMoney(e) !== Number(stored.amount)
  }

  // Kanal (pazaryeri) fiyat hücresi — tanım hücresiyle aynı desen; stored = ChannelPriceDto | undefined.
  // Not: kanal fiyatının silme ucu yok; boş bırakılan hücre yok sayılır (mevcut değer korunur).
  const cpValOf = (itemId, mpCode, stored) => {
    const e = cpEdits[`${itemId}:${mpCode}`]
    return e !== undefined ? e : fmtMoney(stored?.amount ?? null)
  }
  const setCpEdit = (itemId, mpCode, text) =>
    setCpEdits((cur) => ({ ...cur, [`${itemId}:${mpCode}`]: text }))
  const cpDirty = (itemId, mpCode, stored) => {
    const e = cpEdits[`${itemId}:${mpCode}`]
    if (e === undefined || !e.trim()) return false
    return !stored || parseMoney(e) !== Number(stored.amount)
  }

  // Gösterilecek kanal sütunları: bağlı pazaryerleri + herhangi bir kalemde kanal fiyatı olan kodlar.
  const mpColumns = useMemo(() => {
    const byCode = new Map(marketplaces.map((m) => [m.code, m.name]))
    for (const perItem of Object.values(channelPrices)) {
      for (const code of Object.keys(perItem || {})) if (!byCode.has(code)) byCode.set(code, code)
    }
    return [...byCode.entries()].map(([code, name]) => ({ code, name }))
  }, [marketplaces, channelPrices])

  if (error) {
    return (
      <div className="page" style={{ maxWidth: 900 }}>
        <Banner tone="danger" title="Ürün açılamadı">{error}</Banner>
        <div style={{ marginTop: 14 }}><Button variant="secondary" onClick={() => onNavigate('products')}>Ürünlere dön</Button></div>
      </div>
    )
  }
  if (!product) return <div className="page"><div className="list-meta">Yükleniyor…</div></div>

  const images = product.images || []
  const attrValues = Object.entries(attrPick)
    .filter(([, v]) => v)
    .map(([attribute_id, attribute_value_id]) => ({ attribute_id, attribute_value_id }))
  const attrDirty = JSON.stringify(attrPick) !== JSON.stringify(initialAttrPick)
  const dirtyProduct = name !== product.name || status !== product.status
    || description !== (product.description || '')
    || (brandId || '') !== (product.brand_id || '')
    || (categoryId || '') !== (product.category_id || '')
    || attrDirty

  // ---- Satır düzenleme durumu ----
  const baseEdit = (it) => ({
    sku: it.sku || '', barcode: it.barcode || '',
    price: fmtMoney(basePrices[it.id]?.amount ?? null),
    compareAt: fmtMoney(basePrices[it.id]?.compare_at_amount ?? null),
    stock: String(stocks[it.id] ?? 0),
  })
  const editOf = (it) => itemEdits[it.id] || baseEdit(it)
  const setEdit = (it, patch) => setItemEdits((cur) => ({ ...cur, [it.id]: { ...(cur[it.id] || baseEdit(it)), ...patch } }))

  const anyDefDirty = (it) => priceDefs.some((def) =>
    dpDirty(it.id, def, (itemPrices[it.id] || []).find((p) => p.price_definition_id === def.id)))
  const anyChannelDirty = (it) => mpColumns.some((mp) =>
    cpDirty(it.id, mp.code, channelPrices[it.id]?.[mp.code]))
  const itemDirty = (it) => {
    const e = itemEdits[it.id]
    const bp = basePrices[it.id]
    if (!e) return anyDefDirty(it) || anyChannelDirty(it)
    return parseMoney(e.price) !== Number(bp?.amount ?? 0)
      || (e.compareAt.trim() ? parseMoney(e.compareAt) : null) !== (bp?.compare_at_amount ?? null)
      || Math.trunc(Number(e.stock)) !== Number(stocks[it.id] ?? 0)
      || e.sku !== (it.sku || '')
      || e.barcode !== (it.barcode || '')
      || anyDefDirty(it) || anyChannelDirty(it)
  }

  const dirtyItems = items.filter(itemDirty)
  const anyDirty = dirtyProduct || dirtyItems.length > 0
  anyDirtyRef.current = anyDirty
  const itemLabel = (it) => (it.variant_values || []).map((v) => v.name).join(' / ') || it.axis_value || '—'

  // ---- Seçim + toplu yayma ----
  const selCount = items.filter((it) => selected[it.id]).length
  const allSelected = items.length > 0 && selCount === items.length
  const toggleSel = (it) => setSelected((cur) => ({ ...cur, [it.id]: !cur[it.id] }))
  const toggleAll = () => setSelected(allSelected ? {} : Object.fromEntries(items.map((it) => [it.id, true])))
  const bulkTargets = () => (selCount ? items.filter((it) => selected[it.id]) : items)

  // Toplu satıra yazılan değer, sayfalama fark etmeksizin hedef satırlara yayılır.
  const bulkField = (field, v) => setItemEdits((cur) => {
    const next = { ...cur }
    for (const it of bulkTargets()) next[it.id] = { ...(cur[it.id] || baseEdit(it)), [field]: v }
    return next
  })
  const bulkDef = (def, v) => setDpEdits((cur) => {
    const next = { ...cur }
    for (const it of bulkTargets()) next[`${it.id}:${def.id}`] = v
    return next
  })
  const bulkChannel = (mp, v) => setCpEdits((cur) => {
    const next = { ...cur }
    for (const it of bulkTargets()) next[`${it.id}:${mp.code}`] = v
    return next
  })

  // ---- Sayfalama ----
  const per = pageSize === 'all' ? Math.max(1, items.length) : parseInt(pageSize, 10)
  const pageCount = Math.max(1, Math.ceil(items.length / per))
  const curPage = Math.min(page, pageCount - 1)
  const start = curPage * per
  const end = Math.min(start + per, items.length)
  const pagedItems = items.slice(start, end)

  // ---- Kaydetme ----
  const rowError = (it) => {
    const e = editOf(it)
    const price = parseMoney(e.price)
    const compareAt = e.compareAt.trim() ? parseMoney(e.compareAt) : null
    if (!Number.isFinite(price) || price < 0) return 'Geçersiz fiyat'
    if (compareAt != null && (!Number.isFinite(compareAt) || compareAt < 0)) return 'Geçersiz karşılaştırma fiyatı'
    if (!Number.isFinite(Math.trunc(Number(e.stock)))) return 'Geçersiz stok'
    if (!e.barcode.trim()) return 'Barkod boş olamaz'
    return null
  }

  // Satırın tüm değişikliklerini yazar. Catalog saf PIM olduğundan yazımlar bölünür:
  // sku/barkod → Catalog (updateItem); genel fiyat → Pricing (base-price); stok → Inventory;
  // tanımlı fiyatlar → Pricing (item-price); kanal fiyatları → Pricing (channel-price).
  const persistRow = async (it) => {
    const e = editOf(it)
    const price = parseMoney(e.price)
    const compareAt = e.compareAt.trim() ? parseMoney(e.compareAt) : null
    const stock = Math.max(0, Math.trunc(Number(e.stock)))
    // Katalog: yalnızca sku/barkod değiştiyse güncelle (fiyat/stok artık kaleme yazılmaz).
    const skuChanged = e.sku !== (it.sku || '')
    const bcChanged = e.barcode.trim() !== (it.barcode || '')
    if (skuChanged || bcChanged) {
      await api.updateItem(it.id, {
        gtin: it.gtin, mpn: it.mpn, axis_value_entry_id: it.axis_value_entry_id, axis_value: it.axis_value,
        sku: skuChanged ? e.sku : null,
        barcode: bcChanged ? e.barcode.trim() : null,
      })
    }
    // Genel (temel) fiyat — değiştiyse Pricing'e yaz.
    const bp = basePrices[it.id]
    if (price !== Number(bp?.amount ?? 0) || compareAt !== (bp?.compare_at_amount ?? null)) {
      await api.putBasePrice(it.id, { amount: price, compare_at_amount: compareAt, currency: 'TRY' })
    }
    // Stok — değiştiyse Inventory'ye yaz.
    if (stock !== Number(stocks[it.id] ?? 0)) {
      await api.putStock(it.id, { quantity: stock })
    }
    // Tanımlı fiyatlar — düzenlenenleri yaz (boşsa sil).
    const ips = itemPrices[it.id] || []
    for (const def of priceDefs) {
      const key = `${it.id}:${def.id}`
      if (!(key in dpEdits)) continue
      const txt = dpEdits[key]
      const stored = ips.find((p) => p.price_definition_id === def.id)
      if (!txt.trim()) { if (stored) await api.deleteItemPrice(it.id, def.id) }
      else await api.putItemPrice(it.id, def.id, { amount: parseMoney(txt), currency: 'TRY' })
    }
    // Kanal (pazaryeri) fiyatları — düzenlenen ve dolu olanları yaz (mevcut karş. fiyat korunur).
    const cps = channelPrices[it.id] || {}
    for (const mp of mpColumns) {
      const key = `${it.id}:${mp.code}`
      if (!(key in cpEdits)) continue
      const txt = cpEdits[key]
      if (!txt.trim()) continue // kanal fiyatının silme ucu yok
      await api.putChannelPrice(it.id, mp.code, {
        amount: parseMoney(txt),
        compare_at_amount: cps[mp.code]?.compare_at_amount ?? null,
        currency: cps[mp.code]?.currency || 'TRY',
      })
    }
  }

  // Sticky footer'daki Kaydet: ürün alanları + tüm kirli satırlar tek seferde.
  const saveAll = async () => {
    const missing = catAttrsMeta.filter((ca) => ca.required && !attrPick[ca.attribute_id]).map((ca) => ca.name)
    if (dirtyProduct && missing.length) { onToast?.({ tone: 'danger', title: 'Zorunlu özellikler eksik', body: missing.join(', ') }); return }
    for (const it of dirtyItems) {
      const err = rowError(it)
      if (err) { onToast?.({ tone: 'danger', title: err, body: `Varyant: ${itemLabel(it)}` }); return }
    }
    setSaving(true)
    try {
      if (dirtyProduct) {
        await api.updateProduct(product.id, {
          category_id: categoryId || product.category_id,
          brand_id: brandId || null,
          name: name.trim(),
          description: isHtmlEmpty(description) ? null : description,
          status,
          attribute_values: attrValues,
        })
      }
      for (const it of dirtyItems) await persistRow(it)
      setItemEdits({}); setDpEdits({}); setCpEdits({})
      onToast?.({ tone: 'success', title: 'Ürün kaydedildi' })
      load()
    } catch (e2) {
      onToast?.({ tone: 'danger', title: 'Kaydedilemedi', error: e2 })
      load()
    } finally {
      setSaving(false)
    }
  }

  // İptal: tüm yerel düzenlemeleri son yüklenen değerlere döndürür (onayla).
  const cancelEdits = async () => {
    const ok = await askConfirm({
      title: 'Değişiklikleri geri al',
      body: 'Kaydedilmemiş tüm değişiklikler son kaydedilen değerlere döndürülecek.',
      tone: 'danger', confirmLabel: 'Geri al', cancelLabel: 'Vazgeç',
    })
    if (!ok) return
    setName(product.name || ''); setDescription(product.description || ''); setStatus(product.status || 'active')
    setBrandId(product.brand_id || ''); setCategoryId(product.category_id || '')
    setAttrPick(initialAttrPick)
    setItemEdits({}); setDpEdits({}); setCpEdits({})
  }

  // Varyant Ekle: ürünün eksen(ler)i için katalogdaki değerleri yükle.
  const openAdd = async () => {
    setAdding(true)
    setNewItem({ selections: {}, sku: '', barcode: '', price: '', stock: '0' })
    const types = product.variants || []
    const loaded = {}
    for (const t of types) {
      loaded[t.id] = await api.listVariantValues(t.id).catch(() => [])
    }
    setAxisValues(loaded)
  }

  const saveNewItem = async () => {
    const types = product.variants || []
    const selections = types.map((t) => ({ variant_id: t.id, variant_value_id: newItem.selections[t.id] || '' }))
    if (selections.some((s) => !s.variant_value_id)) { onToast?.({ tone: 'danger', title: 'Her eksen için bir değer seç' }); return }
    if (!newItem.barcode.trim()) { onToast?.({ tone: 'danger', title: 'Barkod gerekli' }); return }
    const price = Number(String(newItem.price).replace(',', '.'))
    const stock = Math.max(0, Math.trunc(Number(newItem.stock || 0)))
    if (!Number.isFinite(price) || price < 0) { onToast?.({ tone: 'danger', title: 'Geçersiz fiyat' }); return }
    setSavingNew(true)
    try {
      // Katalog kalemini oluştur (fiyat/stok kabul etmez), ardından temel fiyat + stok yaz.
      const created = await api.createItem(product.id, {
        sku: newItem.sku.trim() || null,
        barcode: newItem.barcode.trim(),
        variant_values: selections,
      })
      if (created?.id) {
        await api.putBasePrice(created.id, { amount: price, compare_at_amount: null, currency: 'TRY' })
        await api.putStock(created.id, { quantity: stock })
      }
      onToast?.({ tone: 'success', title: 'Varyant eklendi' })
      setAdding(false)
      load()
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Eklenemedi', error: e })
    } finally {
      setSavingNew(false)
    }
  }

  const removeItem = async (it) => {
    const label = itemLabel(it) === '—' ? it.barcode : itemLabel(it)
    const ok = await askConfirm({
      title: 'Varyantı sil',
      body: `"${label}" varyantı, fiyat ve stok kayıtlarıyla birlikte kalıcı olarak silinecek.`,
      tone: 'danger', confirmLabel: 'Sil',
    })
    if (!ok) return
    try {
      await api.deleteItem(it.id)
      onToast?.({ tone: 'success', title: 'Varyant silindi' })
      load()
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Silinemedi', error: e })
    }
  }

  const removeProduct = async () => {
    const ok = await askConfirm({
      title: 'Ürünü sil',
      body: `"${product.name}" ürünü ve tüm varyantları kalıcı olarak silinecek. Bu işlem geri alınamaz.`,
      tone: 'danger', confirmLabel: 'Ürünü sil',
    })
    if (!ok) return
    try {
      await api.deleteProduct(product.id)
      onToast?.({ tone: 'success', title: 'Ürün silindi' })
      anyDirtyRef.current = false // ürün silindi; bekleyen düzenlemeler anlamsız
      onNavigate('products')
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Silinemedi', error: e })
    }
  }

  const numStyle = { textAlign: 'right' }
  const priceColCount = 2 + priceDefs.length + mpColumns.length

  return (
    <div className="page" style={{ maxWidth: 1080 }}>
      <PageHeader
        crumbs={[{ label: 'Ürünler', onClick: () => onNavigate('products') }, { label: product.name }]}
        eyebrow="Katalog · ürün detay"
        title={product.name}
        sub={<span className="hstack" style={{ gap: 8, flexWrap: 'wrap' }}>
          <span>Model kodu</span><CodeChip value={product.group_code || product.model_code} />
          <span className="subtle">·</span>
          <span>Stok kodu</span><CodeChip value={product.model_code} />
          {product.slicer_value ? <>
            <span className="subtle">·</span>
            <span>Renk <b style={{ color: 'var(--text-default)', fontWeight: 600 }}>{product.slicer_value}</b></span>
          </> : null}
        </span>}
        actions={
          <div className="seg">
            {STATUS_OPTIONS.map((o) => (
              <button key={o.value} data-active={status === o.value} onClick={() => setStatus(o.value)}>{o.label}</button>
            ))}
          </div>
        }
      />

      {/* Görseller — düzenli 2:3 galeri */}
      <div className="bnode" style={{ marginBottom: 18 }}>
        <div className="bnode__head">
          <span className="ic">{I('image')}</span>
          <div><div className="bnode__title">Görseller</div>
            <div className="list-meta">{images.length} görsel · yükle, kapak yap, sırala, sil.</div></div>
        </div>
        <div className="bnode__body">
          <PhotoGallery images={images} productId={product.id} onChanged={load} onToast={onToast} />
        </div>
      </div>

      {/* Sekmeler */}
      <Tabs
        value={tab}
        onChange={setTab}
        className=""
        tabs={[
          { value: 'bilgiler', label: 'Bilgiler', icon: 'file-text' },
          { value: 'varyantlar', label: 'Varyantlar', icon: 'layers', count: items.length },
          { value: 'ozellikler', label: 'Özellikler', icon: 'tags', count: catAttrsMeta.length },
        ]}
      />

      {/* Sekme 1 · Bilgiler — panel gizlense de mount kalır (düzenlemeler korunur) */}
      <div style={{ display: tab === 'bilgiler' ? 'block' : 'none', marginTop: 18 }}>
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('package')}</span>
            <div><div className="bnode__title">Ürün bilgileri</div>
              <div className="list-meta">Ad, marka, kategori, açıklama ve durum düzenlenebilir; kodlar import ile eşlendiği için sabittir.</div></div>
          </div>
          <div className="bnode__body">
            <div className="fieldgrid">
              <Field label="Ürün adı" required>
                <Input value={name} onChange={(e) => setName(e.target.value)} />
              </Field>
              <Field label="Kategori" required>
                <Select value={categoryId} placeholder="Seç…" onChange={(e) => setCategoryId(e.target.value)}
                  options={categories.map((c) => ({ value: c.id, label: c.name }))} />
              </Field>
              <Field label="Marka">
                <Select value={brandId} placeholder="Seç…" onChange={(e) => setBrandId(e.target.value)}
                  options={brands.map((b) => ({ value: b.id, label: b.name }))} />
              </Field>
              <Field label="Durum">
                <Select value={status} onChange={(e) => setStatus(e.target.value)} options={STATUS_OPTIONS} />
              </Field>
            </div>

            <div style={{ marginTop: 14 }}>
              <Field label="Ürün açıklaması" optional>
                <RichText value={description} onChange={setDescription} placeholder="Ürün açıklaması…"
                  uploadImage={(f) => api.uploadImage(f, 'product').then((r) => r.url)} />
              </Field>
            </div>
          </div>
        </div>
      </div>

      {/* Sekme 2 · Varyantlar */}
      <div style={{ display: tab === 'varyantlar' ? 'block' : 'none', marginTop: 18 }}>
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('layers')}</span>
            <div><div className="bnode__title">Varyantlar</div>
              <div className="list-meta">{items.length} kalem · stok, kodlar ve fiyatlar tek tabloda; her fiyat tanımı ve bağlı pazaryeri bir sütundur.</div></div>
            <div className="hstack" style={{ marginLeft: 'auto' }}>
              <Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={openAdd} disabled={adding}>Varyant Ekle</Button>
            </div>
          </div>
          <div className="bnode__body">
            {adding && (
              <div style={{ padding: '14px 16px', marginBottom: 12, borderRadius: 'var(--radius-md)', border: '1px solid var(--border-subtle)', background: 'var(--surface-subtle)' }}>
                <div className="list-meta" style={{ fontWeight: 600, marginBottom: 10 }}>Yeni varyant</div>
                <div className="hstack" style={{ gap: 10, flexWrap: 'wrap', alignItems: 'flex-end' }}>
                  {(product.variants || []).map((t) => (
                    <Field key={t.id} label={t.name} required>
                      <Select value={newItem.selections[t.id] || ''} placeholder="Seç…"
                        onChange={(e2) => setNewItem((cur) => ({ ...cur, selections: { ...cur.selections, [t.id]: e2.target.value } }))}
                        options={(axisValues[t.id] || []).map((v) => ({ value: v.id, label: v.label || v.name }))} />
                    </Field>
                  ))}
                  <Field label="Barkod" required>
                    <Input mono value={newItem.barcode} onChange={(e2) => setNewItem((c) => ({ ...c, barcode: e2.target.value }))} style={{ width: 160 }} />
                  </Field>
                  <Field label="SKU">
                    <Input mono value={newItem.sku} onChange={(e2) => setNewItem((c) => ({ ...c, sku: e2.target.value }))} style={{ width: 150 }} />
                  </Field>
                  <Field label="Fiyat" required>
                    <Input mono value={newItem.price} onChange={(e2) => setNewItem((c) => ({ ...c, price: e2.target.value }))} style={{ width: 100 }} />
                  </Field>
                  <Field label="Stok">
                    <Input mono value={newItem.stock} onChange={(e2) => setNewItem((c) => ({ ...c, stock: e2.target.value }))} style={{ width: 80 }} />
                  </Field>
                  <div className="hstack" style={{ gap: 6 }}>
                    <Button variant="primary" size="sm" loading={savingNew} onClick={saveNewItem}>Ekle</Button>
                    <Button variant="ghost" size="sm" onClick={() => setAdding(false)}>Vazgeç</Button>
                  </div>
                </div>
              </div>
            )}

            {items.length === 0 ? (
              <div className="subtle" style={{ padding: 8 }}>Bu üründe kalem kalmadı.</div>
            ) : (
              <>
                {/* Toplu yayma bilgi çubuğu */}
                <div className="bulkbar">
                  <span className="bulkbar__title">{I('sparkles')} Toplu yayma</span>
                  {selCount > 0 ? (
                    <span><b>{selCount} varyant</b> seçili — toplu satıra yazdığın değer yalnızca seçili satırlara yayılır (hangi sayfada olduğu fark etmez).</span>
                  ) : (
                    <span>Toplu satıra bir değer yaz — görünen sayfa fark etmeksizin <b style={{ color: 'var(--text-default)' }}>tüm {items.length} varyanta</b> yayılır. Belirli satırları seçersen yalnız onlara uygulanır.</span>
                  )}
                  {selCount > 0 && (
                    <Button variant="ghost" size="sm" style={{ marginLeft: 'auto' }} onClick={() => setSelected({})}>Seçimi temizle</Button>
                  )}
                </div>

                <div className="vmx-wrap">
                  <table className="vmx">
                    <thead>
                      <tr>
                        <th className="vmx__lead" style={{ minWidth: 150 }}>
                          <span className="vmx__leadrow">
                            <button type="button" className="vmx__check" data-on={allSelected} title="Tümünü seç" onClick={toggleAll}>
                              {allSelected && I('check', { size: 11, strokeWidth: 3 })}
                            </button>
                            Varyant
                          </span>
                        </th>
                        <th data-num="true" style={{ width: 96 }}>Stok</th>
                        <th style={{ width: 150 }}>Barkod</th>
                        <th className="vmx__sep" style={{ width: 150 }}>SKU</th>
                        <th data-num="true" style={{ minWidth: 128 }}>Genel satış ₺</th>
                        <th data-num="true" style={{ minWidth: 128 }}>Genel karş. ₺</th>
                        {priceDefs.map((d) => <th key={d.id} data-num="true" style={{ minWidth: 128 }}>{d.name} ₺</th>)}
                        {mpColumns.map((mp) => <th key={mp.code} data-num="true" style={{ minWidth: 128 }} title={`${mp.name} yayın fiyatı`}>{mp.name} ₺</th>)}
                        <th style={{ width: 44 }} />
                      </tr>
                    </thead>
                    <tbody>
                      {/* Toplu doldurma satırı */}
                      <tr className="vmx__bulk">
                        <td className="vmx__lead">
                          <span className="vmx__bulklabel">{I('sparkles')} {selCount ? `Seçili ${selCount}` : 'Tümü'}</span>
                        </td>
                        <td><Input size="sm" mono className="vmx__fill" placeholder="stok" style={numStyle} onChange={(e) => bulkField('stock', e.target.value)} /></td>
                        <td />
                        <td className="vmx__sep" />
                        <td><Input size="sm" mono suffix="₺" className="vmx__fill" placeholder="tümü" style={numStyle} onChange={(e) => bulkField('price', e.target.value)} /></td>
                        <td><Input size="sm" mono suffix="₺" className="vmx__fill" placeholder="tümü" style={numStyle} onChange={(e) => bulkField('compareAt', e.target.value)} /></td>
                        {priceDefs.map((d) => (
                          <td key={d.id}><Input size="sm" mono suffix="₺" className="vmx__fill" placeholder="tümü" style={numStyle} onChange={(e) => bulkDef(d, e.target.value)} /></td>
                        ))}
                        {mpColumns.map((mp) => (
                          <td key={mp.code}><Input size="sm" mono suffix="₺" className="vmx__fill" placeholder="tümü" style={numStyle} onChange={(e) => bulkChannel(mp, e.target.value)} /></td>
                        ))}
                        <td />
                      </tr>

                      {/* Varyant satırları (sayfalı) */}
                      {pagedItems.map((it) => {
                        const e = editOf(it)
                        const ips = itemPrices[it.id] || []
                        const cps = channelPrices[it.id] || {}
                        const isSel = !!selected[it.id]
                        return (
                          <tr key={it.id}>
                            <td className="vmx__lead" data-sel={isSel}>
                              <span className="vmx__leadrow">
                                <button type="button" className="vmx__check" data-on={isSel} onClick={() => toggleSel(it)}>
                                  {isSel && I('check', { size: 11, strokeWidth: 3 })}
                                </button>
                                <span className="vmx__chip">{itemLabel(it)}</span>
                              </span>
                            </td>
                            <td><Input size="sm" mono value={e.stock} style={numStyle} onChange={(ev) => setEdit(it, { stock: ev.target.value })} /></td>
                            <td><Input size="sm" mono value={e.barcode} placeholder="otomatik" onChange={(ev) => setEdit(it, { barcode: ev.target.value })} /></td>
                            <td className="vmx__sep"><Input size="sm" mono value={e.sku} placeholder="opsiyonel" onChange={(ev) => setEdit(it, { sku: ev.target.value })} /></td>
                            <td><Input size="sm" mono suffix="₺" value={e.price} placeholder="0,00" style={numStyle} onChange={(ev) => setEdit(it, { price: ev.target.value })} /></td>
                            <td><Input size="sm" mono suffix="₺" value={e.compareAt} placeholder="—" style={numStyle} onChange={(ev) => setEdit(it, { compareAt: ev.target.value })} /></td>
                            {priceDefs.map((def) => {
                              const stored = ips.find((p) => p.price_definition_id === def.id)
                              return (
                                <td key={def.id}>
                                  <Input size="sm" mono suffix="₺" value={dpValOf(it.id, def, stored)} placeholder="—" style={numStyle}
                                    onChange={(ev) => setDpEdit(it.id, def, ev.target.value)} />
                                </td>
                              )
                            })}
                            {mpColumns.map((mp) => (
                              <td key={mp.code}>
                                <Input size="sm" mono suffix="₺" value={cpValOf(it.id, mp.code, cps[mp.code])} placeholder="—" style={numStyle}
                                  onChange={(ev) => setCpEdit(it.id, mp.code, ev.target.value)} />
                              </td>
                            ))}
                            <td>
                              <button className="tb__icon" title="Varyantı sil" style={{ width: 28, height: 28 }} onClick={() => removeItem(it)}>{I('trash-2')}</button>
                            </td>
                          </tr>
                        )
                      })}
                    </tbody>
                  </table>
                </div>

                {/* Sayfalayıcı */}
                <div className="pager">
                  <span><b style={{ color: 'var(--text-default)' }}>{start + 1}–{end}</b> / {items.length} varyant gösteriliyor</span>
                  <div className="hstack" style={{ gap: 8 }}>
                    <div className="seg">
                      {PAGE_SIZES.map((s) => (
                        <button key={s} data-active={pageSize === s} onClick={() => { setPageSize(s); setPage(0) }}>{s === 'all' ? 'Tümü' : s}</button>
                      ))}
                    </div>
                    {pageCount > 1 && (
                      <>
                        <Button variant="secondary" size="sm" disabled={curPage <= 0} iconLeft={I('chevron-left', { size: 13 })}
                          onClick={() => setPage(Math.max(0, curPage - 1))}>Önceki</Button>
                        <span className="pager__num">Sayfa {curPage + 1} / {pageCount}</span>
                        <Button variant="secondary" size="sm" disabled={curPage >= pageCount - 1}
                          onClick={() => setPage(Math.min(pageCount - 1, curPage + 1))}>Sonraki {I('chevron-right', { size: 13 })}</Button>
                      </>
                    )}
                  </div>
                </div>

                <div className="list-meta" style={{ marginTop: 11 }}>
                  {I('info', { size: 13 })} Solda varyant / stok / barkod / SKU sabit; sağda fiyat kanalları kaydırılır. Genel fiyat kendi siteniz içindir; pazaryeri sütunları o kanalın yayın fiyatını belirler. Toplu doldur, sayfalama fark etmeksizin tüm veriye uygulanır. Değişiklikler alttaki <b>Kaydet</b> ile yazılır.
                </div>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Sekme 3 · Özellikler */}
      <div style={{ display: tab === 'ozellikler' ? 'block' : 'none', marginTop: 18 }}>
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('tags')}</span>
            <div><div className="bnode__title">Özellikler</div>
              <div className="list-meta">Seçilen kategorinin özellikleri — değer seç ya da yeni ekle.</div></div>
            <span className="pim-badge pim-badge--count" style={{ marginLeft: 'auto' }}>{catAttrsMeta.length} özellik</span>
          </div>
          <div className="bnode__body">
            <AttributeEditor grid categoryId={categoryId} pick={attrPick} onPickChange={setAttrPick}
              onAttrsLoaded={setCatAttrsMeta} onToast={onToast} />
          </div>
        </div>
      </div>

      {/* Tehlikeli işlemler */}
      <div className="between" style={{ marginTop: 18 }}>
        <Button variant="secondary" iconLeft={I('arrow-left')} onClick={() => onNavigate('products')}>Ürünlere dön</Button>
        <Button variant="ghost" iconLeft={I('trash-2')} style={{ color: 'var(--danger-fg)' }} onClick={removeProduct}>Ürünü sil</Button>
      </div>

      {/* Sticky Kaydet/İptal */}
      <div className="savebar">
        <div className="savebar__meta">
          <b>{items.length} varyant</b> · <b>{catAttrsMeta.length} özellik</b> · durum: <StatusBadge status={status} />
          {anyDirty && <span className="subtle">· {dirtyItems.length + (dirtyProduct ? 1 : 0)} kayıtsız değişiklik</span>}
        </div>
        <div className="hstack">
          <Button variant="secondary" disabled={!anyDirty || saving} onClick={cancelEdits}>İptal</Button>
          <Button variant="primary" iconLeft={I('check')} disabled={!anyDirty} loading={saving} onClick={saveAll}>Kaydet</Button>
        </div>
      </div>
    </div>
  )
}
