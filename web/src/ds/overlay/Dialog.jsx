import React from 'react';
import { Button } from '../buttons/Button.jsx';

/**
 * pimly Dialog — centered modal with scrim. Confirmations and small forms
 * (e.g. delete, status change). Controlled via `open`. `tone="danger"` styles
 * the confirm button destructively.
 */
export function Dialog({
  open,
  title,
  description,
  children,
  confirmLabel = 'Onayla',
  cancelLabel = 'İptal',
  onConfirm,
  onClose,
  tone = 'default',
  busy = false,
}) {
  if (!open) return null;
  return (
    <div className="pim-dialog__scrim" onMouseDown={(e) => { if (e.target === e.currentTarget) onClose && onClose(); }}>
      <div className="pim-dialog" role="dialog" aria-modal="true" aria-label={typeof title === 'string' ? title : undefined}>
        <div className="pim-dialog__header">
          <div className="pim-dialog__title">{title}</div>
          {description && <div className="pim-dialog__desc">{description}</div>}
        </div>
        {children && <div className="pim-dialog__body">{children}</div>}
        <div className="pim-dialog__footer">
          <Button variant="secondary" onClick={onClose}>{cancelLabel}</Button>
          <Button variant={tone === 'danger' ? 'danger-solid' : 'primary'} loading={busy} onClick={onConfirm}>
            {confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  );
}
