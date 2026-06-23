import React from 'react'
import { Badge } from '../ds'
import { I } from './icons.jsx'
import { HelpHint } from '../help/Help.jsx'

export function PageHeader({ eyebrow, title, sub, actions, crumbs, help }) {
  return (
    <div>
      {crumbs && (
        <div className="crumbs">
          {crumbs.map((c, i) => (
            <React.Fragment key={i}>
              {i > 0 && I('chevron-right')}
              {c.onClick ? (
                <a href="#" onClick={(e) => { e.preventDefault(); c.onClick() }}>{c.label}</a>
              ) : (
                <span style={{ color: 'var(--text-strong)' }}>{c.label}</span>
              )}
            </React.Fragment>
          ))}
        </div>
      )}
      <div className="ph">
        <div>
          {eyebrow && <div className="ph__eyebrow">{eyebrow}</div>}
          <div className="ph__title">{title}{help && <HelpHint topic={help} size="lg" />}</div>
          {sub && <div className="ph__sub">{sub}</div>}
        </div>
        {actions && <div className="ph__actions">{actions}</div>}
      </div>
    </div>
  )
}

const STATUS_LABEL = { active: 'Aktif', draft: 'Taslak', archived: 'Arşiv' }

export function StatusBadge({ status }) {
  return <Badge status={status}>{STATUS_LABEL[status] || status}</Badge>
}
