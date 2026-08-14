import React, { useEffect, useState } from 'react'
import { Button } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { askConfirm } from '../lib/confirm.jsx'

// Ürünler (.NET Catalog): her ürün bir "model" (group_id) altında yaşar. Slicer'lı
// bir ürün renk renk ayrı ürünlere bölünür; burada aynı model_id altında gruplanır.
const FILTERS = ['all', 'active', 'draft', 'archived']

// Türkçe para biçimi (görüntü); fiyat yoksa "—".
const fmtTL = (n) => (n == null ? '—' : `${Number(n).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ₺`)

export function ProductList({ onNavigate, onToast, initialFilter }) {
  const [products, setProducts] = useState([])
  // Panel kartlarından gelen durum filtresiyle açılabilir (ör. "Aktif" kartı → active).
  const [filter, setFilter] = useState(FILTERS.includes(initialFilter) ? initialFilter : 'all')
  const [q, setQ] = useState('')
  // Satır genişletme: hangi ürünlerin varyant kırılımı açık + kalem başına fiyat/stok önbelleği.
  // Fiyat/stok artık Pricing/Inventory modüllerinde olduğundan açılışta tembel çekilir.
  const [expanded, setExpanded] = useState(() => new Set())
  const [variantData, setVariantData] = useState({}) // productId -> { loading, rows }

  const load = () => { api.listProducts().then(setProducts).catch(() => {}) }
  useEffect(() => { load() }, [])

  // Bir ürünün varyantları için temel fiyat (Pricing) + stok (Inventory) çek; kayıt yoksa 404 → boş.
  const loadVariantData = (p) => {
    if (variantData[p.id]) return // önbellek
    const items = p.items || []
    setVariantData((m) => ({ ...m, [p.id]: { loading: true, rows: [] } }))
    Promise.all(items.map(async (it) => {
      const [bp, st] = await Promise.all([
        api.getBasePrice(it.id).catch(() => null),
        api.getStock(it.id).then((s) => s?.quantity ?? null).catch(() => null),
      ])
      return {
        id: it.id,
        label: (it.variant_values || []).map((v) => v.name).join(' / ') || '—',
        sku: it.sku || '',
        barcode: it.barcode || '',
        stock: st,
        price: bp?.amount ?? null,
      }
    })).then((rows) => setVariantData((m) => ({ ...m, [p.id]: { loading: false, rows } })))
  }

  const toggleExpand = (e, p) => {
    e.stopPropagation()
    setExpanded((cur) => {
      const next = new Set(cur)
      if (next.has(p.id)) next.delete(p.id)
      else { next.add(p.id); loadVariantData(p) }
      return next
    })
  }

  const remove = async (e, id, name) => {
    e.stopPropagation()
    const ok = await askConfirm({
      title: 'Ürünü sil',
      body: `"${name}" ürünü ve tüm varyantları kalıcı olarak silinecek. Bu işlem geri alınamaz.`,
      tone: 'danger', confirmLabel: 'Ürünü sil',
    })
    if (!ok) return
    try { await api.deleteProduct(id); onToast?.({ tone: 'success', title: 'Ürün silindi' }); load() }
    catch (err) { onToast?.({ tone: 'danger', title: 'Silinemedi', error: err }) }
  }

  let shown = filter === 'all' ? products : products.filter((p) => p.status === filter)
  if (q.trim()) {
    const needle = q.trim().toLocaleLowerCase('tr')
    shown = shown.filter((p) =>
      (p.name || '').toLocaleLowerCase('tr').includes(needle) ||
      (p.model_code || '').toLocaleLowerCase('tr').includes(needle) ||
      (p.group_code || '').toLocaleLowerCase('tr').includes(needle) ||
      (p.slicer_value || '').toLocaleLowerCase('tr').includes(needle))
  }

  // Group split color-products under their shared model (group_id).
  const byModel = new Map()
  for (const p of shown) { if (!byModel.has(p.group_id)) byModel.set(p.group_id, []); byModel.get(p.group_id).push(p) }
  const models = [...byModel.values()]

  // Renk ve grup kodu artık yapısal: slicer ile bölünen ürün slicer_value (renk adı)
  // ve group_code (pazaryerindeki "model kodu", ör. 26BHR0007) taşır; model_code ise
  // renk ürününe özgü koddur (pazaryerindeki "stok kodu", ör. 26BHR0007R15).
  const colorLabel = (p) => p.slicer_value || null
  const modelBaseCode = (ps) => ps.find((p) => p.group_code)?.group_code || null
  const thumbUrl = (p) => {
    const imgs = p.images || []
    const primary = imgs.find((im) => im.is_primary) || imgs[0]
    return primary?.url || null
  }

  return (
    <div className="page">
      <PageHeader
        eyebrow="Katalog"
        title="Ürünler"
        help="products"
        sub="Model → renk → varyant. Tek formdan toplu oluştur; slicer'lı türler renk renk ayrı ürün olur."
        actions={<>
          <Button variant="secondary" iconLeft={I('upload')} disabled>İçe aktar</Button>
          <Button variant="accent" iconLeft={I('plus')} onClick={() => onNavigate('builder')}>Ürün Oluştur</Button>
        </>}
      />
      <div className="toolbar">
        <div className="seg">
          {[['all', 'Tümü'], ['active', 'Aktif'], ['draft', 'Taslak'], ['archived', 'Arşiv']].map(([v, l]) => (
            <button key={v} data-active={filter === v} onClick={() => setFilter(v)}>{l}</button>
          ))}
        </div>
        <div className="toolbar__spacer"></div>
        <div style={{ width: 240 }}>
          <div className="pim-input-group">
            <span className="pim-input-group__icon">{I('search')}</span>
            <input className="pim-input pim-input--sm" placeholder="Ürün ara…" value={q} onChange={(e) => setQ(e.target.value)} />
          </div>
        </div>
      </div>
      <div className="pim-table-wrap">
        <table className="pim-table">
          <thead><tr>
            <th>Ürün / Renk</th><th>Model kodu</th><th>Durum</th><th>Varyant</th><th></th>
          </tr></thead>
          <tbody>
            {models.map((ps) => {
              const itemsTotal = ps.reduce((a, p) => a + (p.items?.length || 0), 0)
              const split = ps.length > 1 || !!colorLabel(ps[0])
              return (
                <React.Fragment key={ps[0].group_id}>
                  {split && (
                    <tr>
                      <td colSpan={5} style={{ background: 'var(--surface-subtle)' }}>
                        <span className="hstack" style={{ gap: 8, fontWeight: 700, color: 'var(--text-strong)' }}>
                          {I('package', { size: 15 })}
                          <span className="mono">{modelBaseCode(ps) || ps[0].model_code}</span>
                          <span className="list-meta" style={{ fontWeight: 400 }}>· {ps.length} renk · {itemsTotal} varyant</span>
                        </span>
                      </td>
                    </tr>
                  )}
                  {ps.map((p) => {
                    const img = thumbUrl(p)
                    const count = p.items?.length || 0
                    const isOpen = expanded.has(p.id)
                    return (
                      <React.Fragment key={p.id}>
                      <tr data-open={isOpen} style={{ cursor: 'pointer' }} onClick={() => onNavigate('product', p.id)}>
                        <td>
                          <div className="cellrow" style={{ paddingLeft: split ? 14 : 0 }}>
                            <span className="thumb" style={img ? { padding: 0, overflow: 'hidden' } : undefined}>
                              {img
                                ? <img src={img} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: 'inherit' }} />
                                : I(split ? 'palette' : 'package')}
                            </span>
                            <span>
                              <span className="pim-td-strong">{split ? (colorLabel(p) || p.name) : p.name}</span>
                              {split && colorLabel(p) && (
                                <span className="list-meta" style={{ display: 'block', maxWidth: 420, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{p.name}</span>
                              )}
                            </span>
                          </div>
                        </td>
                        <td className="pim-td-mono">{p.model_code}</td>
                        <td><StatusBadge status={p.status} /></td>
                        <td onClick={(e) => e.stopPropagation()}>
                          {count > 0 ? (
                            <button className="pvar-toggle" data-open={isOpen} onClick={(e) => toggleExpand(e, p)}
                              title={isOpen ? 'Varyantları gizle' : 'Varyantları göster'}>
                              {I('chevron-right', { size: 14, className: 'pvar-chev' })}
                              <span className="pim-td-strong">{count}</span>
                              <span className="list-meta">varyant</span>
                            </button>
                          ) : <span className="muted">0</span>}
                        </td>
                        <td onClick={(e) => e.stopPropagation()}>
                          <div className="rowact">
                            <button className="tb__icon" title="Düzenle" style={{ width: 28, height: 28 }} onClick={() => onNavigate('product', p.id)}>{I('square-pen')}</button>
                            <button className="tb__icon" title="Sil" style={{ width: 28, height: 28 }} onClick={(e) => remove(e, p.id, p.name)}>{I('trash-2')}</button>
                          </div>
                        </td>
                      </tr>
                      {isOpen && (
                        <tr className="pvar-panel">
                          <td colSpan={5} style={{ padding: 0, background: 'var(--surface-subtle)' }}>
                            <VariantBreakdown data={variantData[p.id]} onEdit={() => onNavigate('product', p.id)} />
                          </td>
                        </tr>
                      )}
                      </React.Fragment>
                    )
                  })}
                </React.Fragment>
              )
            })}
            {shown.length === 0 && (
              <tr><td colSpan={5} className="subtle" style={{ padding: 18 }}>Ürün bulunamadı. Sağ üstten “Ürün Oluştur” ile ekleyin.</td></tr>
            )}
          </tbody>
        </table>
      </div>
      <div className="list-meta" style={{ marginTop: 12 }}>{shown.length} ürün · {models.length} model gösteriliyor</div>
    </div>
  )
}

// Ürün satırının varyant kırılımı: her kalem için varyant adı, barkod, SKU, stok ve
// genel (temel) fiyat. Fiyat/stok Pricing/Inventory modüllerinden tembel yüklenir.
function VariantBreakdown({ data, onEdit }) {
  if (!data || data.loading) {
    return (
      <div className="pvar">
        <div className="list-meta hstack" style={{ gap: 8, padding: '6px 2px' }}>
          {I('loader', { size: 14 })} Varyant bilgileri yükleniyor…
        </div>
      </div>
    )
  }
  const rows = data.rows || []
  if (rows.length === 0) {
    return <div className="pvar"><div className="list-meta" style={{ padding: '6px 2px' }}>Bu üründe kalem yok.</div></div>
  }
  return (
    <div className="pvar">
      <div className="pvar__tbl">
        <div className="pvar__row pvar__row--head">
          <span>Varyant</span><span>Barkod</span><span>SKU</span>
          <span className="pvar__num">Stok</span><span className="pvar__num">Genel Fiyat</span>
        </div>
        {rows.map((r) => (
          <div className="pvar__row" key={r.id}>
            <span className="pvar__strong">{r.label}</span>
            <span className="mono">{r.barcode || '—'}</span>
            <span className="mono">{r.sku || '—'}</span>
            <span className="pvar__num">{r.stock == null ? '—' : r.stock}</span>
            <span className="pvar__num mono">{fmtTL(r.price)}</span>
          </div>
        ))}
      </div>
      <div className="between" style={{ marginTop: 8 }}>
        <span className="list-meta">{rows.length} varyant · fiyat ve stok ürün detayından düzenlenir</span>
        <Button variant="ghost" size="sm" iconLeft={I('square-pen')} onClick={onEdit}>Düzenle</Button>
      </div>
    </div>
  )
}
