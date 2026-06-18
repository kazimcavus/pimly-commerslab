import React, { useEffect, useState } from 'react'
import { Button } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { relativeTime } from '../lib/format.js'

export function ProductList({ onNavigate, onToast }) {
  const [groups, setGroups] = useState([])
  const [cats, setCats] = useState({})
  const [filter, setFilter] = useState('all')
  const [q, setQ] = useState('')

  const load = () => {
    api.listGroups().then(setGroups).catch(() => {})
    api.listCategories().then((cs) => {
      const m = {}
      for (const c of cs) m[c.id] = c.name
      setCats(m)
    }).catch(() => {})
  }
  useEffect(() => { load() }, [])

  const remove = async (e, id) => {
    e.stopPropagation()
    if (!confirm('Bu grup ve tüm ürün/varyantları silinecek. Emin misin?')) return
    try {
      await api.deleteGroup(id)
      onToast?.({ tone: 'success', title: 'Grup silindi' })
      load()
    } catch (err) {
      onToast?.({ tone: 'danger', title: 'Silinemedi', body: err.message })
    }
  }

  let shown = filter === 'all' ? groups : groups.filter((g) => g.status === filter)
  if (q.trim()) {
    const needle = q.trim().toLocaleLowerCase('tr')
    shown = shown.filter((g) =>
      (g.title || '').toLocaleLowerCase('tr').includes(needle) ||
      (g.group_code || '').toLocaleLowerCase('tr').includes(needle))
  }

  return (
    <div className="page">
      <PageHeader
        eyebrow="Katalog"
        title="Ürünler"
        sub="Grup → ürün → varyant ağacı. Tek formdan toplu oluştur."
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
            <input className="pim-input pim-input--sm" placeholder="Grup ara…" value={q} onChange={(e) => setQ(e.target.value)} />
          </div>
        </div>
      </div>
      <div className="pim-table-wrap">
        <table className="pim-table">
          <thead><tr>
            <th>Grup</th><th>Kod</th><th>Kategori</th><th>Durum</th><th>Güncellenme</th><th></th>
          </tr></thead>
          <tbody>
            {shown.map((g) => (
              <tr key={g.id} onClick={() => onNavigate('group', g.id)} style={{ cursor: 'pointer' }}>
                <td><div className="cellrow"><span className="thumb">{I('package')}</span><span className="pim-td-strong">{g.title || '(başlıksız)'}</span></div></td>
                <td className="pim-td-mono">{g.group_code}</td>
                <td className="muted">{g.category_id ? (cats[g.category_id] || '—') : '—'}</td>
                <td><StatusBadge status={g.status} /></td>
                <td className="subtle">{relativeTime(g.updated_at)}</td>
                <td onClick={(e) => e.stopPropagation()}>
                  <div className="rowact">
                    <button className="tb__icon" title="Düzenle" style={{ width: 28, height: 28 }} onClick={() => onNavigate('group', g.id)}>{I('pencil')}</button>
                    <button className="tb__icon" title="Sil" style={{ width: 28, height: 28 }} onClick={(e) => remove(e, g.id)}>{I('trash-2')}</button>
                  </div>
                </td>
              </tr>
            ))}
            {shown.length === 0 && (
              <tr><td colSpan={6} className="subtle" style={{ padding: 18 }}>Grup bulunamadı.</td></tr>
            )}
          </tbody>
        </table>
      </div>
      <div className="list-meta" style={{ marginTop: 12 }}>{shown.length} grup gösteriliyor · {groups.length} toplam</div>
    </div>
  )
}
