import React, { useEffect, useState } from 'react'
import { Button, Field, Input, Select, Badge, Banner } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { parseTrMoney } from '../lib/format.js'

const DEFAULT_COLORS = [
  { name: 'Kırmızı', hex: '#d7382b' }, { name: 'Siyah', hex: '#1c1b19' },
  { name: 'Beyaz', hex: '#ffffff' }, { name: 'Lacivert', hex: '#1e3a5f' },
  { name: 'Haki', hex: '#6b6f4a' }, { name: 'Bej', hex: '#d9cbb2' },
]
const DEFAULT_SIZES = ['XS', 'S', 'M', 'L', 'XL', '2XL']

let _pid = 1
const newProduct = (color, hex, sizes) => ({
  id: _pid++,
  color: color || '',
  hex: hex || '#d3ccc1',
  code: '',
  title: '',
  rows: (sizes || []).map((s) => ({ size: s, price: '1.299,90', compareAt: '', stock: '0' })),
})

export function ProductBuilder({ onNavigate, onSaved }) {
  const [categories, setCategories] = useState([])
  const [categoryId, setCategoryId] = useState('')
  const [groupCode, setGroupCode] = useState('')
  const [title, setTitle] = useState('')
  const [status, setStatus] = useState('draft')
  const [products, setProducts] = useState([
    newProduct('Kırmızı', '#d7382b', ['S', 'M', 'L']),
    newProduct('Siyah', '#1c1b19', ['S', 'M', 'L', 'XL']),
  ])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => { api.listCategories().then(setCategories).catch(() => {}) }, [])

  const totalVariants = products.reduce((a, p) => a + p.rows.length, 0)

  const toggleSize = (pid, size) =>
    setProducts((ps) => ps.map((p) => {
      if (p.id !== pid) return p
      const has = p.rows.some((r) => r.size === size)
      return {
        ...p,
        rows: has ? p.rows.filter((r) => r.size !== size)
          : [...p.rows, { size, price: '1.299,90', compareAt: '', stock: '0' }],
      }
    }))

  const setRow = (pid, size, key, val) =>
    setProducts((ps) => ps.map((p) => p.id !== pid ? p : {
      ...p, rows: p.rows.map((r) => r.size === size ? { ...r, [key]: val } : r),
    }))

  const setProduct = (pid, key, val) =>
    setProducts((ps) => ps.map((p) => p.id !== pid ? p : { ...p, [key]: val }))

  const addProduct = () => setProducts((ps) => [...ps, newProduct('', '#d3ccc1', [])])
  const removeProduct = (pid) => setProducts((ps) => ps.filter((p) => p.id !== pid))

  const save = async () => {
    setError('')
    if (!title.trim()) { setError('Grup başlığı gerekli.'); return }
    const payload = {
      group: {
        group_code: groupCode.trim(),
        category_id: categoryId || null,
        title: title.trim(),
        status,
      },
      products: products.map((p, i) => ({
        code: p.code.trim() || `R${String(i + 1).padStart(2, '0')}`,
        title: p.title.trim(),
        variants: p.rows.map((r) => ({
          axis_value: r.size,
          price: parseTrMoney(r.price),
          compare_at_price: r.compareAt ? parseTrMoney(r.compareAt) : null,
          stock: parseInt(r.stock, 10) || 0,
        })),
      })),
    }
    setSaving(true)
    try {
      const res = await api.productsBatch(payload)
      const pc = res.products?.length || 0
      const vc = (res.products || []).reduce((a, p) => a + (p.variants?.length || 0), 0)
      onSaved?.(`${pc} ürün, ${vc} varyant oluşturuldu. SKU & barkod üretildi.`)
    } catch (e) {
      setError(e.message || 'Kaydedilemedi')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="page" style={{ maxWidth: 980 }}>
      <PageHeader
        crumbs={[{ label: 'Ürünler', onClick: () => onNavigate('products') }, { label: 'Ürün Oluştur' }]}
        eyebrow="Tek yazma yolu · products:batch"
        title="Ürün Oluştur"
        sub="Grup, ürünler ve varyantlar aynı anda kaydedilir."
        actions={<>
          <Button variant="secondary" onClick={() => onNavigate('products')}>İptal</Button>
          <Button variant="primary" iconLeft={I('check')} onClick={save} loading={saving}>Kaydet</Button>
        </>}
      />

      {error && <div style={{ marginBottom: 16 }}><Banner tone="danger" title="Kaydedilemedi">{error}</Banner></div>}

      <div className="builder">
        {/* 1 — GROUP */}
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('folder')}</span>
            <div><div className="bnode__title">1 · Grup</div><div className="list-meta">Tüm ürünleri kapsayan üst kayıt</div></div>
            <div style={{ marginLeft: 'auto' }}>
              <div className="seg">
                <button data-active={status === 'draft'} onClick={() => setStatus('draft')}>Taslak</button>
                <button data-active={status === 'active'} onClick={() => setStatus('active')}>Aktif</button>
              </div>
            </div>
          </div>
          <div className="bnode__body">
            <div className="fieldgrid">
              <Field label="Grup kodu" auto="Boş bırakılırsa otomatik üretilecek">
                <Input mono placeholder="TS-0001" value={groupCode} onChange={(e) => setGroupCode(e.target.value)} />
              </Field>
              <Field label="Kategori">
                <Select
                  placeholder="Seç…"
                  value={categoryId}
                  onChange={(e) => setCategoryId(e.target.value)}
                  options={categories.map((c) => ({ value: c.id, label: c.name }))}
                />
              </Field>
              <Field label="Başlık" required>
                <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Basic Tişört" />
              </Field>
            </div>
          </div>
        </div>

        {/* 2 — PRODUCTS */}
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('package')}</span>
            <div><div className="bnode__title">2 · Ürünler</div><div className="list-meta">Renk bazlı — her renk bir ürün</div></div>
            <span className="pim-badge pim-badge--count" style={{ marginLeft: 'auto' }}>{products.length} ürün · {totalVariants} varyant</span>
          </div>
          <div className="bnode__body">
            {products.map((p, i) => (
              <div className="product-card" key={p.id}>
                <div className="product-card__head">
                  <span className="swatch-sm" style={{ background: p.hex, width: 18, height: 18 }}></span>
                  <span style={{ fontWeight: 600, color: 'var(--text-strong)' }}>{p.color || 'Yeni ürün'}</span>
                  <span className="lvlchip lvl-product">product</span>
                  <span className="mono list-meta">{p.code || `R${String(i + 1).padStart(2, '0')}`}</span>
                  <div style={{ marginLeft: 'auto' }}>
                    <button className="tb__icon" style={{ width: 28, height: 28 }} title="Ürünü kaldır" onClick={() => removeProduct(p.id)}>{I('trash-2')}</button>
                  </div>
                </div>
                <div className="product-card__body">
                  <div className="fieldgrid" style={{ marginBottom: 14 }}>
                    <Field label="Renk (grouping)">
                      <Select
                        value={p.color}
                        placeholder="Seç…"
                        onChange={(e) => {
                          const c = DEFAULT_COLORS.find((x) => x.name === e.target.value)
                          setProducts((ps) => ps.map((x) => x.id === p.id ? { ...x, color: e.target.value, hex: c ? c.hex : x.hex } : x))
                        }}
                        options={DEFAULT_COLORS.map((c) => ({ value: c.name, label: c.name }))}
                      />
                    </Field>
                    <Field label="Ürün kodu" auto="boş ise otomatik">
                      <Input mono placeholder={`R${String(i + 1).padStart(2, '0')}`} value={p.code} onChange={(e) => setProduct(p.id, 'code', e.target.value)} />
                    </Field>
                    <Field label="Ürün başlığı" optional>
                      <Input placeholder={`Basic Tişört — ${p.color}`} value={p.title} onChange={(e) => setProduct(p.id, 'title', e.target.value)} />
                    </Field>
                  </div>

                  <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--text-strong)', marginBottom: 8 }}>Beden seç → varyant satırları üret</div>
                  <div className="chipset" style={{ marginBottom: 14 }}>
                    {DEFAULT_SIZES.map((s) => (
                      <span key={s} className="sizechip" data-on={p.rows.some((r) => r.size === s)} onClick={() => toggleSize(p.id, s)}>{s}</span>
                    ))}
                  </div>

                  {p.rows.length > 0 && (
                    <div style={{ border: '1px solid var(--border-subtle)', borderRadius: 'var(--radius-md)', padding: '4px 12px 10px' }}>
                      <div className="variant-row variant-row__head">
                        <span>Beden</span><span>Fiyat</span><span>Karşılaştırma</span><span>Stok</span><span></span>
                      </div>
                      {p.rows.map((r) => (
                        <div className="variant-row" key={r.size}>
                          <span><span className="pim-badge">{r.size}</span></span>
                          <Input size="sm" mono suffix="₺" value={r.price} onChange={(e) => setRow(p.id, r.size, 'price', e.target.value)} />
                          <Input size="sm" mono suffix="₺" placeholder="—" value={r.compareAt} onChange={(e) => setRow(p.id, r.size, 'compareAt', e.target.value)} />
                          <Input size="sm" mono value={r.stock} onChange={(e) => setRow(p.id, r.size, 'stock', e.target.value)} />
                          <button className="tb__icon" style={{ width: 28, height: 28 }} title="Satırı kaldır" onClick={() => toggleSize(p.id, r.size)}>{I('x')}</button>
                        </div>
                      ))}
                      <div className="list-meta" style={{ marginTop: 8 }}>{I('info')} Barkod (EAN-13) her satır için otomatik üretilecek.</div>
                    </div>
                  )}
                </div>
              </div>
            ))}
            <Button variant="secondary" iconLeft={I('plus')} onClick={addProduct}>Renk / ürün ekle</Button>
          </div>
        </div>
      </div>

      <div className="between" style={{ marginTop: 18, padding: '14px 16px', background: 'var(--surface)', border: '1px solid var(--border-default)', borderRadius: 'var(--radius-lg)' }}>
        <div className="list-meta">
          <span style={{ color: 'var(--text-strong)', fontWeight: 600 }}>{products.length} ürün</span> · <span style={{ color: 'var(--text-strong)', fontWeight: 600 }}>{totalVariants} varyant</span> oluşturulacak · durum: <StatusBadge status={status} />
        </div>
        <div className="hstack">
          <Button variant="secondary" onClick={() => onNavigate('products')}>İptal</Button>
          <Button variant="primary" iconLeft={I('check')} onClick={save} loading={saving}>Kaydet</Button>
        </div>
      </div>
    </div>
  )
}
