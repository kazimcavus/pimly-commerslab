import React from 'react';

/**
 * pimly Card — bordered white surface, the default container for grouped
 * content. Compose with `CardHeader` + `CardBody`, or pass `pad` for a simple
 * padded box. Flat by default (border, no shadow).
 */
export function Card({ children, pad = false, className = '', ...rest }) {
  return (
    <div className={['pim-card', pad ? 'pim-card--pad' : '', className].filter(Boolean).join(' ')} {...rest}>
      {children}
    </div>
  );
}

export function CardHeader({ title, actions, className = '' }) {
  return (
    <div className={`pim-card__header ${className}`.trim()}>
      <span className="pim-card__title">{title}</span>
      {actions && <div style={{ display: 'flex', gap: 'var(--space-4)', alignItems: 'center' }}>{actions}</div>}
    </div>
  );
}

export function CardBody({ children, className = '' }) {
  return <div className={`pim-card__body ${className}`.trim()}>{children}</div>;
}
