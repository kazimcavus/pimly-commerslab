import React, { useEffect, useState } from 'react'
import { Button, Badge, Card, CardHeader, CardBody } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { relativeTime } from '../lib/format.js'

export function Dashboard({ onNavigate, tenant }) {
  const [groups, setGroups] = useState([])
  useEffect(() => { api.listGroups().then(setGroups).catch(() => {}) }, [])

  const activePct = groups.length
    ? Math.round((100 * groups.filter((g) => g.status === 'active').length) / groups.length)
    : 0
  const drafts = groups.filter((g) => g.status === 'draft').length
  const stats = [
    { label: 'Ürün grubu', icon: 'folder', value: String(groups.length), delta: 'canlı katalog' },
    { label: 'Aktif', icon: 'circle-check-big', value: String(groups.filter((g) => g.status === 'active').length), delta: `%${activePct} oran` },
    { label: 'Taslak', icon: 'file-pen-line', value: String(drafts), delta: 'yayına hazırlanıyor' },
    { label: 'Arşiv', icon: 'archive', value: String(groups.filter((g) => g.status === 'archived').length), delta: '' },
  ]

  return (
    <div className="page">
      <PageHeader
        eyebrow="Genel bakış"
        title="Panel"
        sub={`${tenant || 'Mağaza'} kataloğunun anlık durumu.`}
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={() => onNavigate('builder')}>Ürün Oluştur</Button>}
      />
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
          <CardHeader title="Son eklenen gruplar" actions={<Button variant="ghost" size="sm" iconRight={I('arrow-right')} onClick={() => onNavigate('products')}>Tümü</Button>} />
          <div className="pim-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
            <table className="pim-table">
              <thead><tr><th>Grup</th><th>Kod</th><th>Durum</th><th></th></tr></thead>
              <tbody>
                {groups.slice(0, 6).map((g) => (
                  <tr key={g.id} onClick={() => onNavigate('group', g.id)} style={{ cursor: 'pointer' }}>
                    <td><div className="cellrow"><span className="thumb">{I('package')}</span><span className="pim-td-strong">{g.title || '(başlıksız)'}</span></div></td>
                    <td className="pim-td-mono">{g.group_code}</td>
                    <td><StatusBadge status={g.status} /></td>
                    <td className="subtle" style={{ textAlign: 'right' }}>{relativeTime(g.updated_at)}</td>
                  </tr>
                ))}
                {groups.length === 0 && (
                  <tr><td colSpan={4} className="subtle" style={{ padding: 18 }}>Henüz grup yok — “Ürün Oluştur” ile başla.</td></tr>
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
                ['Taslak', groups.length ? Math.round((100 * drafts) / groups.length) : 0, 'var(--status-draft-dot)'],
                ['Arşiv', groups.length ? Math.round((100 * groups.filter((g) => g.status === 'archived').length) / groups.length) : 0, 'var(--status-archived-dot)'],
              ]} />
            </CardBody>
          </Card>
          <Card>
            <CardHeader title="Pazaryeri" />
            <CardBody>
              <div className="between" style={{ marginBottom: 10 }}>
                <div className="hstack">{I('store')}<span style={{ fontWeight: 600, color: 'var(--text-strong)' }}>Trendyol</span></div>
                <Badge status="draft">Yakında</Badge>
              </div>
              <div className="list-meta">Eşleme tabloları hazır · gönderim v2'de.</div>
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
