import React, { useEffect, useMemo, useState } from 'react'
import { Button, Field, Input, Select, Banner, RichText } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { isHtmlEmpty } from '../lib/sanitizeHtml.js'
import { PhotoGallery } from './parts/PhotoGallery.jsx'
import { AttributeEditor } from './parts/AttributeEditor.jsx'

const STATUS_OPTIONS = [
  { value: 'active', label: 'Aktif' },
  { value: 'draft', label: 'Taslak' },
  { value: 'archived', label: 'Arşiv' },
]

const parseMoney = (v) => Number(String(v).trim().replace(',', '.'))
const fmtMoney = (n) => (n == null ? '' : String(n).replace('.', ','))

// Ürün detayı: kodlar (model/stok), renk, görseller, özellikler ve kalem tablosu.
// Ad/durum ile kalem fiyat/stok düzenlenebilir; kalem ve ürün silinebilir.
export function ProductDetail({ productId, onNavigate, onToast }) {
  const [product, setProduct] = useState(null)
  const [error, setError] = useState('')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [status, setStatus] = useState('active')
  const [savingProduct, setSavingProduct] = useState(false)
  const [itemEdits, setItemEdits] = useState({})   // itemId -> { sku, barcode, price, stock }
  const [savingItem, setSavingItem] = useState(null)

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
      .catch((e) => setError(e.message || 'Ürün yüklenemedi'))
  }
  useEffect(() => { load() }, [productId])

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

  const saveProduct = async () => {
    // Zorunlu özellik istemci-tarafı kontrolü (backend de doğrular).
    const missing = catAttrsMeta.filter((ca) => ca.required && !attrPick[ca.attribute_id]).map((ca) => ca.name)
    if (missing.length) { onToast?.({ tone: 'danger', title: 'Zorunlu özellikler eksik', body: missing.join(', ') }); return }
    setSavingProduct(true)
    try {
      const updated = await api.updateProduct(product.id, {
        category_id: categoryId || product.category_id,
        brand_id: brandId || null,
        name: name.trim(),
        description: isHtmlEmpty(description) ? null : description,
        status,
        attribute_values: attrValues,
      })
      setProduct(updated)
      const pick = Object.fromEntries((updated.attribute_values || []).map((av) => [av.attribute?.id, av.id]))
      setAttrPick(pick); setInitialAttrPick(pick)
      onToast?.({ tone: 'success', title: 'Ürün güncellendi' })
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Kaydedilemedi', body: e.message })
    } finally {
      setSavingProduct(false)
    }
  }

  const editOf = (it) => itemEdits[it.id] || {
    sku: it.sku || '', barcode: it.barcode || '',
    price: fmtMoney(basePrices[it.id]?.amount ?? null),
    compareAt: fmtMoney(basePrices[it.id]?.compare_at_amount ?? null),
    stock: String(stocks[it.id] ?? 0),
  }
  const setEdit = (it, patch) => setItemEdits((cur) => ({ ...cur, [it.id]: { ...editOf(it), ...patch } }))
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

  // Satırın tüm değişikliklerini kaydeder. Catalog saf PIM olduğundan yazımlar bölünür:
  // sku/barkod → Catalog (updateItem); genel fiyat → Pricing (base-price); stok → Inventory;
  // tanımlı fiyatlar → Pricing (item-price); kanal fiyatları → Pricing (channel-price).
  const saveItemRow = async (it) => {
    const e = editOf(it)
    const price = parseMoney(e.price)
    const compareAt = e.compareAt.trim() ? parseMoney(e.compareAt) : null
    const stock = Math.max(0, Math.trunc(Number(e.stock)))
    if (!Number.isFinite(price) || price < 0) { onToast?.({ tone: 'danger', title: 'Geçersiz fiyat' }); return }
    if (compareAt != null && (!Number.isFinite(compareAt) || compareAt < 0)) { onToast?.({ tone: 'danger', title: 'Geçersiz karşılaştırma fiyatı' }); return }
    if (!Number.isFinite(stock)) { onToast?.({ tone: 'danger', title: 'Geçersiz stok' }); return }
    if (!e.barcode.trim()) { onToast?.({ tone: 'danger', title: 'Barkod boş olamaz' }); return }
    setSavingItem(it.id)
    try {
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
      setDpEdits((cur) => { const n = { ...cur }; priceDefs.forEach((def) => delete n[`${it.id}:${def.id}`]); return n })
      setCpEdits((cur) => { const n = { ...cur }; mpColumns.forEach((mp) => delete n[`${it.id}:${mp.code}`]); return n })
      setItemEdits((cur) => { const n = { ...cur }; delete n[it.id]; return n })
      onToast?.({ tone: 'success', title: 'Varyant güncellendi' })
      load()
    } catch (e2) {
      onToast?.({ tone: 'danger', title: 'Kaydedilemedi', body: e2.message })
    } finally {
      setSavingItem(null)
    }
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
      onToast?.({ tone: 'danger', title: 'Eklenemedi', body: e.message })
    } finally {
      setSavingNew(false)
    }
  }

  const removeItem = async (it) => {
    const label = (it.variant_values || []).map((v) => v.name).join(' / ') || it.barcode
    if (!confirm(`"${label}" varyantı silinecek. Emin misin?`)) return
    try {
      await api.deleteItem(it.id)
      onToast?.({ tone: 'success', title: 'Varyant silindi' })
      load()
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Silinemedi', body: e.message })
    }
  }

  const removeProduct = async () => {
    if (!confirm(`"${product.name}" ürünü ve tüm varyantları silinecek. Emin misin?`)) return
    try {
      await api.deleteProduct(product.id)
      onToast?.({ tone: 'success', title: 'Ürün silindi' })
      onNavigate('products')
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Silinemedi', body: e.message })
    }
  }

  return (
    <div className="page" style={{ maxWidth: 980 }}>
      <PageHeader
        crumbs={[{ label: 'Ürünler', onClick: () => onNavigate('products') }, { label: product.slicer_value || product.model_code }]}
        eyebrow="Katalog"
        title={product.name}
        sub={<>
          Model kodu <span className="mono pim-td-strong">{product.group_code || product.model_code}</span>
          {' · '}Stok kodu <span className="mono pim-td-strong">{product.model_code}</span>
          {product.slicer_value ? <> · Renk <span className="pim-td-strong">{product.slicer_value}</span></> : null}
        </>}
        actions={<StatusBadge status={product.status} />}
      />

      {/* Görseller */}
      <div className="bnode" style={{ marginBottom: 14 }}>
        <div className="bnode__head">
          <span className="ic">{I('image')}</span>
          <div><div className="bnode__title">Görseller</div>
            <div className="list-meta">{images.length} görsel · yükle, kapak yap, sırala, sil.</div></div>
        </div>
        <div className="bnode__body">
          <PhotoGallery images={images} productId={product.id} onChanged={load} onToast={onToast} />
        </div>
      </div>

      {/* Ürün bilgileri */}
      <div className="bnode">
        <div className="bnode__head">
          <span className="ic">{I('package')}</span>
          <div><div className="bnode__title">Ürün bilgileri</div>
            <div className="list-meta">Ad, marka, kategori, açıklama, durum ve özellikler düzenlenebilir; kodlar import ile eşlendiği için sabittir.</div></div>
          <div className="hstack" style={{ marginLeft: 'auto' }}>
            <Button variant="primary" size="sm" loading={savingProduct} disabled={!dirtyProduct} onClick={saveProduct}>Kaydet</Button>
          </div>
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

          <div style={{ marginTop: 16 }}>
            <div className="list-meta" style={{ fontWeight: 600, marginBottom: 8 }}>Özellikler</div>
            <AttributeEditor categoryId={categoryId} pick={attrPick} onPickChange={setAttrPick}
              onAttrsLoaded={setCatAttrsMeta} onToast={onToast} />
          </div>
        </div>
      </div>

      {/* Varyant kalemleri */}
      <div className="bnode" style={{ marginTop: 14 }}>
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
          ) : (() => {
            const label = (it) => (it.variant_values || []).map((v) => v.name).join(' / ') || '—'
            const rowActions = (it) => (
              <span className="rowact" style={{ display: 'flex', gap: 4, justifyContent: 'flex-end' }}>
                {itemDirty(it) && (
                  <button className="tb__icon" title="Kaydet" style={{ width: 28, height: 28, color: 'var(--accent, #4f7d5a)' }}
                    disabled={savingItem === it.id} onClick={() => saveItemRow(it)}>{I('check')}</button>
                )}
                <button className="tb__icon" title="Varyantı sil" style={{ width: 28, height: 28 }} onClick={() => removeItem(it)}>{I('trash-2')}</button>
              </span>
            )
            const priceCount = 2 + priceDefs.length + mpColumns.length
            const cols = `minmax(150px,1.3fr) 0.7fr 1.05fr 1.05fr 2px repeat(${priceCount}, minmax(120px,1fr)) 64px`
            const minW = 150 + 76 + 150 + 150 + 2 + priceCount * 128 + 64
            return (
              <div className="hscroll">
                <div className="pmatrix__row pmatrix__head" style={{ gridTemplateColumns: cols, minWidth: minW }}>
                  <span>Varyant</span><span>Stok</span><span>Barkod</span><span>SKU</span>
                  <span className="pmatrix__vrule" />
                  <span className="pmatrix__pcol">Genel Satış ₺</span><span className="pmatrix__pcol">Genel Karş. ₺</span>
                  {priceDefs.map((d) => <span key={d.id} className="pmatrix__pcol">{d.name}</span>)}
                  {mpColumns.map((mp) => <span key={mp.code} className="pmatrix__pcol" title={`${mp.name} yayın fiyatı`}>{mp.name} ₺</span>)}
                  <span />
                </div>
                {items.map((it) => {
                  const e = editOf(it); const ips = itemPrices[it.id] || []; const cps = channelPrices[it.id] || {}
                  return (
                    <div className="pmatrix__row" key={it.id} style={{ gridTemplateColumns: cols, minWidth: minW }}>
                      <span className="pim-td-strong">{label(it)}</span>
                      <Input size="sm" mono value={e.stock} onChange={(ev) => setEdit(it, { stock: ev.target.value })} />
                      <Input size="sm" mono value={e.barcode} onChange={(ev) => setEdit(it, { barcode: ev.target.value })} />
                      <Input size="sm" mono value={e.sku} onChange={(ev) => setEdit(it, { sku: ev.target.value })} placeholder="—" />
                      <span className="pmatrix__vrule" />
                      <Input size="sm" mono suffix="₺" value={e.price} onChange={(ev) => setEdit(it, { price: ev.target.value })} placeholder="0,00" />
                      <Input size="sm" mono suffix="₺" value={e.compareAt} onChange={(ev) => setEdit(it, { compareAt: ev.target.value })} placeholder="—" />
                      {priceDefs.map((def) => {
                        const stored = ips.find((p) => p.price_definition_id === def.id)
                        return <Input key={def.id} size="sm" mono suffix="₺" value={dpValOf(it.id, def, stored)} placeholder="—"
                          onChange={(ev) => setDpEdit(it.id, def, ev.target.value)} />
                      })}
                      {mpColumns.map((mp) => (
                        <Input key={mp.code} size="sm" mono suffix="₺" value={cpValOf(it.id, mp.code, cps[mp.code])} placeholder="—"
                          onChange={(ev) => setCpEdit(it.id, mp.code, ev.target.value)} />
                      ))}
                      {rowActions(it)}
                    </div>
                  )
                })}
                <div className="list-meta" style={{ marginTop: 10 }}>{I('info', { size: 13 })} Önde ürün bilgisi, ayraçtan sonra fiyatlar. Genel fiyat kendi siteniz içindir ve Trendyol import'unda otomatik dolar; pazaryeri sütunları o kanalın yayın (publication) fiyatını belirler. Değişiklikleri satır sonundaki ✓ ile kaydedin.</div>
              </div>
            )
          })()}
        </div>
      </div>

      {/* Tehlikeli işlemler */}
      <div className="between" style={{ marginTop: 18 }}>
        <Button variant="secondary" iconLeft={I('arrow-left')} onClick={() => onNavigate('products')}>Ürünlere dön</Button>
        <Button variant="ghost" iconLeft={I('trash-2')} style={{ color: 'var(--danger-fg)' }} onClick={removeProduct}>Ürünü sil</Button>
      </div>
    </div>
  )
}
