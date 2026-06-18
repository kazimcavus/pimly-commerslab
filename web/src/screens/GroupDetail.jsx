import React, { useEffect, useState } from 'react'
import { Button, Badge, Tabs } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { trMoney } from '../lib/format.js'

// Render a variant's option combination (Renk/Beden…) as badges, with a
// color/image swatch where the type uses one. Falls back to legacy axis_value.
function variantLabel(v) {
  const opts = Array.isArray(v.options) ? v.options : []
  if (opts.length === 0) return v.axis_value ? <span className="pim-badge">{v.axis_value}</span> : <span className="subtle">—</span>
  return (
    <span style={{ display: 'inline-flex', gap: 4, flexWrap: 'wrap' }}>
      {opts.map((o, i) => (
        <span key={i} className="pim-badge" style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
          {(o.color || o.image_url) && <span className="swatch-sm" style={o.image_url ? { backgroundImage: `url(${o.image_url})`, backgroundSize: 'cover' } : { background: o.color }} />}
          {o.value_label}
        </span>
      ))}
    </span>
  )
}

export function GroupDetail({ groupId, onNavigate, onToast }) {
  const [g, setG] = useState(null)
  const [cats, setCats] = useState({})
  const [tab, setTab] = useState('urunler')
  const [open, setOpen] = useState({})
  const [media, setMedia] = useState([])

  const load = () => {
    api.getGroup(groupId).then((grp) => {
      setG(grp)
      const o = {}
      ;(grp.products || []).forEach((p, i) => { o[p.id] = i < 2 })
      setOpen(o)
    }).catch((e) => onToast?.({ tone: 'danger', title: 'Yüklenemedi', body: e.message }))
    api.listCategories().then((cs) => { const m = {}; cs.forEach((c) => (m[c.id] = c.name)); setCats(m) }).catch(() => {})
  }
  useEffect(() => { load() }, [groupId])

  useEffect(() => {
    if (tab !== 'medya' || !g) return
    Promise.all((g.products || []).map((p) => api.listMedia(p.id).catch(() => [])))
      .then((lists) => setMedia(lists.flat()))
  }, [tab, g])

  if (!g) return <div className="page"><div className="list-meta">Yükleniyor…</div></div>

  const products = g.products || []
  const variantCount = products.reduce((a, p) => a + (p.variants?.length || 0), 0)
  const toggle = (id) => setOpen((o) => ({ ...o, [id]: !o[id] }))

  const setStatus = async (status) => {
    try { await api.updateGroup(g.id, { status }); onToast?.({ tone: 'success', title: status === 'active' ? 'Yayınlandı' : 'Taslağa alındı' }); load() }
    catch (e) { onToast?.({ tone: 'danger', title: 'Güncellenemedi', body: e.message }) }
  }
  const remove = async () => {
    if (!confirm('Grup silinecek. Emin misin?')) return
    try { await api.deleteGroup(g.id); onToast?.({ tone: 'success', title: 'Grup silindi' }); onNavigate('products') }
    catch (e) { onToast?.({ tone: 'danger', title: 'Silinemedi', body: e.message }) }
  }

  return (
    <div className="page">
      <PageHeader
        crumbs={[{ label: 'Ürünler', onClick: () => onNavigate('products') }, { label: g.title || g.group_code }]}
        eyebrow={<span className="mono">{g.group_code}</span>}
        title={g.title || '(başlıksız)'}
        actions={<>
          <Button variant="secondary" iconLeft={I('trash-2')} onClick={remove}>Sil</Button>
          {g.status === 'active'
            ? <Button variant="secondary" onClick={() => setStatus('draft')}>Taslağa al</Button>
            : <Button variant="primary" iconLeft={I('check')} onClick={() => setStatus('active')}>Yayınla</Button>}
        </>}
      />
      <div className="hstack" style={{ marginBottom: 16, gap: 14 }}>
        <StatusBadge status={g.status} />
        <span className="list-meta">Kategori <b style={{ color: 'var(--text-default)' }}>{g.category_id ? (cats[g.category_id] || '—') : '—'}</b></span>
        <span className="list-meta">{products.length} ürün · {variantCount} varyant</span>
      </div>

      <div style={{ marginBottom: 18 }}>
        <Tabs value={tab} onChange={setTab} tabs={[
          { value: 'urunler', label: 'Ürünler & varyantlar', icon: 'package', count: products.length },
          { value: 'medya', label: 'Medya', icon: 'image' },
          { value: 'pazaryeri', label: 'Pazaryeri', icon: 'store' },
        ]} />
      </div>

      {tab === 'urunler' && (
        <div className="pim-table-wrap">
          <table className="pim-table">
            <thead><tr><th style={{ width: 40 }}></th><th>Ürün / Varyant</th><th>SKU / Barkod</th><th>Varyant</th><th className="pim-td-num">Fiyat</th><th className="pim-td-num">Stok</th></tr></thead>
            <tbody>
              {products.map((p) => (
                <React.Fragment key={p.id}>
                  <tr onClick={() => toggle(p.id)} style={{ cursor: 'pointer', background: 'var(--surface-subtle)' }}>
                    <td><span className="hstack" style={{ color: 'var(--text-muted)' }}>{I(open[p.id] ? 'chevron-down' : 'chevron-right')}</span></td>
                    <td><div className="cellrow"><span className="pim-td-strong">{p.title || p.product_sku}</span><span className="lvlchip lvl-product">product</span></div></td>
                    <td className="pim-td-mono">{p.product_sku}</td>
                    <td className="subtle">—</td>
                    <td className="pim-td-num subtle">—</td>
                    <td className="pim-td-num mono">{(p.variants || []).reduce((a, v) => a + (v.stock || 0), 0)}</td>
                  </tr>
                  {open[p.id] && (p.variants || []).map((v) => (
                    <tr key={v.id}>
                      <td></td>
                      <td style={{ paddingLeft: 44 }}><span className="hstack list-meta">{I('corner-down-right')}<span className="lvlchip lvl-variant">variant</span></span></td>
                      <td className="pim-td-mono"><div>{v.sku || '—'}</div><div className="subtle" style={{ fontSize: 11 }}>{v.barcode}</div></td>
                      <td>{variantLabel(v)}</td>
                      <td className="pim-td-num mono">{trMoney(v.price)} ₺</td>
                      <td className="pim-td-num mono" style={{ color: v.stock === 0 ? 'var(--danger-fg)' : v.stock < 10 ? 'var(--status-archived-fg)' : 'var(--text-default)' }}>{v.stock}</td>
                    </tr>
                  ))}
                </React.Fragment>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'medya' && (
        media.length ? (
          <div className="media-grid">
            {media.map((m) => (
              <div className="media-tile" key={m.id}>
                <div className="media-tile__img"><img src={m.url} alt={m.alt_text || ''} style={{ width: '100%', height: '100%', objectFit: 'cover' }} onError={(e) => { e.target.style.display = 'none' }} />{I('image')}</div>
                <div className="media-tile__meta"><div className="mono" style={{ color: 'var(--text-strong)' }}>{m.alt_text || 'görsel'}</div></div>
              </div>
            ))}
          </div>
        ) : <div className="list-meta">Bu grupta görsel yok. Medya sekmesinden yükleyebilirsin.</div>
      )}

      {tab === 'pazaryeri' && (
        <div className="pim-card pim-card--pad">
          <div className="between"><div className="hstack">{I('store')}<span className="pim-td-strong">Trendyol</span></div><Badge status="draft">Eşleme v2</Badge></div>
        </div>
      )}
    </div>
  )
}
