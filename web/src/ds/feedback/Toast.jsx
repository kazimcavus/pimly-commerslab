import React from 'react';

const ICONS = { success: 'check-circle-2', danger: 'alert-octagon', info: 'info' };

/**
 * pimly Toast — transient confirmation (e.g. "Ürün kaydedildi"). Render inside
 * a fixed-position stack; pair with your own timeout/dismiss logic.
 */
export function Toast({ tone = 'success', title, children, onClose, className = '' }) {
  return (
    <div className={`pim-toast pim-toast--${tone} ${className}`.trim()} role="status">
      <span className="pim-toast__icon"><i data-lucide={ICONS[tone]} /></span>
      <div style={{ flex: 1 }}>
        <div className="pim-toast__title">{title}</div>
        {children && <div className="pim-toast__body">{children}</div>}
      </div>
      {onClose && (
        <button type="button" className="pim-tag__x" onClick={onClose} aria-label="Kapat">
          <i data-lucide="x" />
        </button>
      )}
    </div>
  );
}
