import React, { useEffect, useState } from 'react'
import { Button, Badge, Card, CardHeader, CardBody } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'

// Hafif panel — .NET Catalog ürün verisinden anlık durum (grup yok).
export function Dashboard({ onNavigate, user }) {
  const [products, setProducts] = useState([])
  useEffect(() => { api.listProducts().then(setProducts).catch(() => {}) }, [])

  const count = (s) => products.filter((p) => p.status === s).length
  const total = products.length
  const active = count('active')
  const drafts = count('draft')
  const archived = count('archived')
  const activePct = total ? Math.round((100 * active) / total) : 0

  const stats = [
    { label: 'Ürün', icon: 'package', value: String(total), delta: 'canlı katalog' },
    { label: 'Aktif', icon: 'circle-check-big', value: String(active), delta: `%${activePct} oran` },
    { label: 'Taslak', icon: 'file-pen-line', value: String(drafts), delta: 'yayına hazırlanıyor' },
    { label: 'Arşiv', icon: 'archive', value: String(archived), delta: '' },
  ]

  return (
    <div className="page">
      <PageHeader
        eyebrow="Genel bakış"
        title="Panel"
        sub={`${user?.name || 'Mağaza'} kataloğunun anlık durumu.`}
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={() => onNavigate('builder')}>Ürün Oluştur</Button>}
      />
      {total === 0 && (
        <Card style={{ marginBottom: 16 }}>
          <CardBody>
            <div className="between" style={{ flexWrap: 'wrap', gap: 12 }}>
              <div className="hstack" style={{ gap: 12 }}>
                <span className="thumb" style={{ width: 40, height: 40 }}>{I('store', { size: 20 })}</span>
                <div>
                  <div style={{ fontWeight: 700, color: 'var(--text-strong)' }}>Trendyol'dan ürünlerini çek</div>
                  <div className="list-meta">Mağazanı bağla; kategoriler, özellikler ve varyantlar otomatik tanımlansın, ürünlerin içeri aktarılsın.</div>
                </div>
              </div>
              <Button variant="primary" iconLeft={I('download')} onClick={() => onNavigate('onboarding')}>İçe aktarmayı başlat</Button>
            </div>
          </CardBody>
        </Card>
      )}
      <div className="stats">
        {stats.map((s) => (
          <div className="stat" key={s.label}>
            <div className="stat__label">{I(s.icon)}{s.label}</div>
            <div className="stat__value">{s.value}</div>
            <div className="stat__delta">{s.delta}</div>
          </div>
        ))}
      </div>
      <div className="cols">
        <Card>
          <CardHeader title="Son eklenen ürünler" actions={<Button variant="ghost" size="sm" iconRight={I('arrow-right')} onClick={() => onNavigate('products')}>Tümü</Button>} />
          <div className="pim-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
            <table className="pim-table">
              <thead><tr><th>Ürün</th><th>Model kodu</th><th>Durum</th></tr></thead>
              <tbody>
                {products.slice(0, 6).map((p) => (
                  <tr key={p.id} onClick={() => onNavigate('products')} style={{ cursor: 'pointer' }}>
                    <td><div className="cellrow"><span className="thumb">{I('package')}</span><span className="pim-td-strong">{p.name || '(başlıksız)'}</span></div></td>
                    <td className="pim-td-mono">{p.model_code}</td>
                    <td><StatusBadge status={p.status} /></td>
                  </tr>
                ))}
                {total === 0 && (
                  <tr><td colSpan={3} className="subtle" style={{ padding: 18 }}>Henüz ürün yok — “Ürün Oluştur” ile başla.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </Card>
        <div className="stack">
          <Card>
            <CardHeader title="Durum dağılımı" />
            <CardBody>
              <Distribution rows={[
                ['Aktif', activePct, 'var(--status-active-dot)'],
                ['Taslak', total ? Math.round((100 * drafts) / total) : 0, 'var(--status-draft-dot)'],
                ['Arşiv', total ? Math.round((100 * archived) / total) : 0, 'var(--status-archived-dot)'],
              ]} />
            </CardBody>
          </Card>
          <Card>
            <CardHeader title="Pazaryeri" actions={<Button variant="ghost" size="sm" iconRight={I('arrow-right')} onClick={() => onNavigate('channels')}>Yönet</Button>} />
            <CardBody>
              <div className="between" style={{ marginBottom: 10 }}>
                <div className="hstack">{I('store')}<span style={{ fontWeight: 600, color: 'var(--text-strong)' }}>Trendyol</span></div>
                <Badge status="active">İçe aktarma hazır</Badge>
              </div>
              <div className="list-meta">Ürünlerini içeri aktar; gönderim (v2) eşlemeleri otomatik kurulur.</div>
            </CardBody>
          </Card>
        </div>
      </div>
    </div>
  )
}

function Distribution({ rows }) {
  return (
    <div className="stack" style={{ gap: 12 }}>
      {rows.map(([label, pct, color]) => (
        <div key={label}>
          <div className="between" style={{ fontSize: 13, marginBottom: 5 }}>
            <span className="hstack" style={{ gap: 7 }}><span style={{ width: 8, height: 8, borderRadius: 9, background: color }}></span>{label}</span>
            <span className="mono muted">%{pct}</span>
          </div>
          <div style={{ height: 7, background: 'var(--surface-sunken)', borderRadius: 99 }}>
            <div style={{ width: pct + '%', height: '100%', background: color, borderRadius: 99 }}></div>
          </div>
        </div>
      ))}
    </div>
  )
}
