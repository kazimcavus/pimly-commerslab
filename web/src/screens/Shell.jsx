import React from 'react'
import { Avatar } from '../ds'
import { I } from './icons.jsx'

const NAV = [
  { type: 'item', id: 'dashboard', label: 'Panel', icon: 'layout-dashboard' },
  { type: 'section', label: 'Tanımlar' },
  { type: 'item', id: 'categories', label: 'Kategoriler', icon: 'folder-tree' },
  { type: 'item', id: 'attributes', label: 'Özellikler', icon: 'tags' },
  { type: 'item', id: 'variants', label: 'Varyantlar', icon: 'layers' },
  { type: 'item', id: 'metaobjects', label: "Metaobject'ler", icon: 'boxes' },
  { type: 'section', label: 'Katalog' },
  { type: 'item', id: 'products', label: 'Ürünler', icon: 'package' },
  { type: 'item', id: 'media', label: 'Medya', icon: 'image' },
  { type: 'section', label: 'Platform' },
  { type: 'item', id: 'settings', label: 'Ayarlar', icon: 'settings' },
  { type: 'item', id: 'admin', label: 'Admin', icon: 'shield' },
]

function Sidebar({ route, onNavigate }) {
  return (
    <aside className="sb">
      <div className="sb__brand">
        <img src="/assets/pimly-wordmark.svg" alt="pimly" className="pim-light-logo" />
        <img src="/assets/pimly-wordmark-dark.svg" alt="pimly" className="pim-dark-logo" />
      </div>
      <nav className="sb__nav">
        {NAV.map((n, i) =>
          n.type === 'section' ? (
            <div key={i} className="sb__section">{n.label}</div>
          ) : (
            <button key={n.id} className="sb__item" data-active={route === n.id} onClick={() => onNavigate(n.id)}>
              {I(n.icon)}
              <span>{n.label}</span>
            </button>
          )
        )}
      </nav>
      <div className="sb__foot">
        <button className="sb__item" onClick={() => onNavigate('dashboard')}>
          {I('life-buoy')}<span>Yardım</span>
        </button>
      </div>
    </aside>
  )
}

function TopBar({ tenant, role, onLogout, onToggleTheme }) {
  return (
    <header className="tb">
      <div className="tb__tenant">
        <span className="tdot"></span>
        {tenant || 'pimly'}
        <span className="tb__chev">{I('chevrons-up-down')}</span>
      </div>
      <div className="tb__spacer"></div>
      <div className="tb__search">
        <div className="pim-input-group">
          <span className="pim-input-group__icon">{I('search')}</span>
          <input className="pim-input pim-input--sm" placeholder="Ürün, SKU, barkod ara…" />
        </div>
      </div>
      {role && <span className="tb__role">{role}</span>}
      <button className="tb__icon" title="Tema" onClick={onToggleTheme}>{I('sun-moon')}</button>
      <button className="tb__icon" title="Bildirimler">{I('bell')}</button>
      <button className="tb__icon" title="Çıkış" onClick={onLogout} style={{ marginRight: -6 }}>{I('log-out')}</button>
      <Avatar name={tenant || 'pimly'} />
    </header>
  )
}

export function AppShell({ route, onNavigate, onLogout, onToggleTheme, tenant, role, children }) {
  return (
    <div className="app">
      <Sidebar route={route} onNavigate={onNavigate} />
      <div className="app__main">
        <TopBar tenant={tenant} role={role} onLogout={onLogout} onToggleTheme={onToggleTheme} />
        <div className="app__content">{children}</div>
      </div>
    </div>
  )
}
