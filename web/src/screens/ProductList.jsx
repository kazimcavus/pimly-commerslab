import React, { useEffect, useState } from 'react'
import { Button } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'

// Ürünler (.NET Catalog): her ürün bir "model" (group_id) altında yaşar. Slicer'lı
// bir ürün renk renk ayrı ürünlere bölünür; burada aynı model_id altında gruplanır.
export function ProductList({ onNavigate, onToast }) {
  const [products, setProducts] = useState([])
  const [filter, setFilter] = useState('all')
  const [q, setQ] = useState('')

  const load = () => { api.listProducts().then(setProducts).catch(() => {}) }
  useEffect(() => { load() }, [])

  const remove = async (e, id, name) => {
    e.stopPropagation()
    if (!confirm(`"${name}" ürünü ve tüm varyantları silinecek. Emin misin?`)) return
    try { await api.deleteProduct(id); onToast?.({ tone: 'success', title: 'Ürün silindi' }); load() }
    catch (err) { onToast?.({ tone: 'danger', title: 'Silinemedi', body: err.message }) }
  }

  let shown = filter === 'all' ? products : products.filter((p) => p.status === filter)
  if (q.trim()) {
    const needle = q.trim().toLocaleLowerCase('tr')
    shown = shown.filter((p) =>
      (p.name || '').toLocaleLowerCase('tr').includes(needle) ||
      (p.model_code || '').toLocaleLowerCase('tr').includes(needle))
  }

  // Group split color-products under their shared model (group_id).
  const byModel = new Map()
  for (const p of shown) { if (!byModel.has(p.group_id)) byModel.set(p.group_id, []); byModel.get(p.group_id).push(p) }
  const models = [...byModel.values()]

  const colorLabel = (p) => { const parts = (p.name || '').split(' - '); return parts.length > 1 ? parts.slice(1).join(' - ') : null }
  const modelTitle = (ps) => (ps[0].name || '').split(' - ')[0]

  return (
    <div className="page">
      <PageHeader
        eyebrow="Katalog"
        title="Ürünler"
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
                          {I('package', { size: 15 })}{modelTitle(ps)}
                          <span className="list-meta" style={{ fontWeight: 400 }}>· {ps.length} renk · {itemsTotal} varyant</span>
                        </span>
                      </td>
                    </tr>
                  )}
                  {ps.map((p) => (
                    <tr key={p.id}>
                      <td>
                        <div className="cellrow" style={{ paddingLeft: split ? 14 : 0 }}>
                          <span className="thumb">{I(split ? 'palette' : 'package')}</span>
                          <span className="pim-td-strong">{split ? (colorLabel(p) || p.name) : p.name}</span>
                        </div>
                      </td>
                      <td className="pim-td-mono">{p.model_code}</td>
                      <td><StatusBadge status={p.status} /></td>
                      <td className="muted">{p.items?.length || 0}</td>
                      <td onClick={(e) => e.stopPropagation()}>
                        <div className="rowact">
                          <button className="tb__icon" title="Sil" style={{ width: 28, height: 28 }} onClick={(e) => remove(e, p.id, p.name)}>{I('trash-2')}</button>
                        </div>
                      </td>
                    </tr>
                  ))}
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
