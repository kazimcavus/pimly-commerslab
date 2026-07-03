import React, { useEffect, useRef, useState } from 'react'
import { Avatar } from '../ds'
import { I } from './icons.jsx'

// Navigasyon grupları. Tek elemanlı gruplar düz bağlantı; çok elemanlılar
// (örn. Tanımlar) açılır/kapanır akordeon başlık olur — depo arayüzündeki gibi.
const NAV = [
  { id: 'home', items: [{ id: 'dashboard', label: 'Panel', icon: 'layout-dashboard' }] },
  {
    id: 'definitions', title: 'Tanımlar', icon: 'shapes', items: [
      { id: 'categories', label: 'Kategoriler', icon: 'folder-tree' },
      { id: 'attributes', label: 'Özellikler', icon: 'tags' },
      { id: 'variants', label: 'Varyantlar', icon: 'layers' },
    ],
  },
  { id: 'catalog', title: 'Katalog', icon: 'package', items: [{ id: 'products', label: 'Ürünler', icon: 'package' }] },
  { id: 'marketplaces', title: 'Pazaryerleri', icon: 'store', items: [{ id: 'channels', label: 'Pazaryerleri', icon: 'store' }] },
  { id: 'platform', title: 'Platform', icon: 'settings', items: [{ id: 'settings', label: 'Ayarlar', icon: 'settings' }] },
]

const EXPANDED_W = 252
const RAIL_W = 66
const PIN_KEY = 'pimly_sidebar_pinned'
const GROUPS_KEY = 'pimly_sidebar_groups'

const readBool = (k, d) => { try { const v = localStorage.getItem(k); return v === null ? d : v === '1' } catch { return d } }
const readJson = (k) => { try { return JSON.parse(localStorage.getItem(k) || '{}') || {} } catch { return {} } }

// Hangi grupta hangi route var (aktif grubu otomatik açmak için).
const groupOfRoute = (route) => NAV.find((g) => g.items.some((it) => it.id === route))?.id

function Sidebar({ route, onNavigate, collapsed, pinned, onTogglePin, onHover }) {
  const [groups, setGroups] = useState(() => readJson(GROUPS_KEY))

  // Aktif route'un grubunu otomatik aç.
  useEffect(() => {
    const gid = groupOfRoute(route)
    const grp = NAV.find((g) => g.id === gid)
    if (grp && grp.items.length > 1 && groups[gid] === false) {
      setGroups((p) => ({ ...p, [gid]: true }))
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [route])

  const isOpen = (id) => groups[id] !== false // varsayılan açık
  const toggleGroup = (id) => setGroups((p) => {
    const next = { ...p, [id]: !(p[id] !== false) }
    try { localStorage.setItem(GROUPS_KEY, JSON.stringify(next)) } catch {}
    return next
  })

  const Item = ({ it }) => (
    <button
      className="sb__item" data-active={route === it.id} title={collapsed ? it.label : undefined}
      onClick={() => onNavigate(it.id)}
    >
      {I(it.icon)}
      {!collapsed && <span className="sb__label">{it.label}</span>}
      {!collapsed && route === it.id && <span className="sb__dot" />}
    </button>
  )

  return (
    <aside
      className="sb" data-collapsed={collapsed}
      onMouseEnter={() => onHover(true)}
      onMouseLeave={() => onHover(false)}
    >
      <div className="sb__brand">
        {collapsed ? (
          <img src="/assets/pimly-mark.svg" alt="pimly" className="sb__mark" />
        ) : (
          <>
            <img src="/assets/pimly-wordmark.svg" alt="pimly" className="pim-light-logo" />
            <img src="/assets/pimly-wordmark-dark.svg" alt="pimly" className="pim-dark-logo" />
          </>
        )}
        {!collapsed && (
          <button
            className="sb__pin" data-on={pinned} title={pinned ? 'Sabitlemeyi kaldır' : 'Sabitle (açık tut)'}
            onClick={onTogglePin}
          >
            {I('pin')}
          </button>
        )}
      </div>

      <nav className="sb__nav">
        {NAV.map((g, gi) => {
          const multi = g.items.length > 1

          // Daraltılmış ray: tüm öğeler ikon-only, başlıksız.
          if (collapsed) {
            return (
              <React.Fragment key={g.id}>
                {gi > 0 && <div className="sb__sep" />}
                {g.items.map((it) => <Item key={it.id} it={it} />)}
              </React.Fragment>
            )
          }

          // Tek elemanlı grup → düz bağlantı.
          if (!multi) {
            return (
              <React.Fragment key={g.id}>
                {gi > 0 && <div className="sb__sep" />}
                <Item it={g.items[0]} />
              </React.Fragment>
            )
          }

          // Çok elemanlı grup → akordeon.
          const open = isOpen(g.id)
          return (
            <div key={g.id} className="sb__group">
              {gi > 0 && <div className="sb__sep" />}
              <button className="sb__group-btn" onClick={() => toggleGroup(g.id)} aria-expanded={open}>
                {I(g.icon || g.items[0].icon)}
                <span className="sb__label">{g.title}</span>
                <span className="sb__chev" data-open={open}>{I('chevron-down', { size: 15 })}</span>
              </button>
              {open && (
                <div className="sb__sub">
                  {g.items.map((it) => <Item key={it.id} it={it} />)}
                </div>
              )}
            </div>
          )
        })}
      </nav>

      <div className="sb__foot">
        <button className="sb__item" title={collapsed ? 'Yardım' : undefined} onClick={() => onNavigate('dashboard')}>
          {I('life-buoy')}{!collapsed && <span className="sb__label">Yardım</span>}
        </button>
      </div>
    </aside>
  )
}

function TopBar({ user, tenant, onLogout, onToggleTheme }) {
  const name = user?.name || user?.email || 'pimly'
  return (
    <header className="tb">
      <div className="tb__tenant" title={tenant?.name ? `Çalışma alanı: ${tenant.name}` : undefined}>
        <span className="tdot"></span>
        {tenant?.name || name}
      </div>
      <div className="tb__spacer"></div>
      <div className="tb__search">
        <div className="pim-input-group">
          <span className="pim-input-group__icon">{I('search')}</span>
          <input className="pim-input pim-input--sm" placeholder="Ürün, SKU, barkod ara…" />
        </div>
      </div>
      <button className="tb__icon" title="Tema" onClick={onToggleTheme}>{I('sun-moon')}</button>
      <button className="tb__icon" title="Bildirimler">{I('bell')}</button>
      <button className="tb__icon" title="Çıkış" onClick={onLogout} style={{ marginRight: -6 }}>{I('log-out')}</button>
      <Avatar name={name} />
    </header>
  )
}

export function AppShell({ route, onNavigate, onLogout, onToggleTheme, user, tenant, children }) {
  const [pinned, setPinned] = useState(() => readBool(PIN_KEY, true))
  const [hover, setHover] = useState(false)
  const hoverTimer = useRef(null)

  const collapsed = !pinned && !hover
  const width = collapsed ? RAIL_W : EXPANDED_W

  const togglePin = () => {
    setPinned((p) => {
      const next = !p
      try { localStorage.setItem(PIN_KEY, next ? '1' : '0') } catch {}
      // Sabitlemeyi kaldırırken imleç hâlâ kenar çubuğunda — fare çıkana dek açık kalsın.
      if (!next) setHover(true)
      return next
    })
  }

  // Küçük gecikmeyle hover bırak (fareyle gezerken titreme olmasın).
  const onHover = (v) => {
    if (pinned) return
    if (hoverTimer.current) { clearTimeout(hoverTimer.current); hoverTimer.current = null }
    if (v) setHover(true)
    else hoverTimer.current = setTimeout(() => setHover(false), 140)
  }

  return (
    <div className="app" data-rail={collapsed} style={{ gridTemplateColumns: `${width}px 1fr` }}>
      <Sidebar
        route={route} onNavigate={onNavigate}
        collapsed={collapsed} pinned={pinned} onTogglePin={togglePin} onHover={onHover}
      />
      <div className="app__main">
        <TopBar user={user} tenant={tenant} onLogout={onLogout} onToggleTheme={onToggleTheme} />
        <div className="app__content">{children}</div>
      </div>
    </div>
  )
}
