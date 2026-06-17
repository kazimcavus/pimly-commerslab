import React from 'react';

const ICONS = { info: 'info', success: 'check-circle-2', warning: 'alert-triangle', danger: 'alert-octagon' };

/**
 * pimly Banner — inline contextual message tied to the API error envelope
 * or a page-level notice (readonly mode, sync status, validation summary).
 */
export function Banner({ tone = 'info', title, children, icon, className = '' }) {
  return (
    <div className={`pim-banner pim-banner--${tone} ${className}`.trim()} role={tone === 'danger' ? 'alert' : 'status'}>
      <span className="pim-banner__icon"><i data-lucide={icon || ICONS[tone]} /></span>
      <div>
        {title && <div className="pim-banner__title">{title}</div>}
        {children && <div className="pim-banner__body">{children}</div>}
      </div>
    </div>
  );
}
