import React from 'react'
import { Button } from '../buttons/Button.jsx'

/**
 * pimly Drawer — right-side slide-in panel for create/edit flows with a
 * scrollable body and a sticky footer. Controlled via `open`.
 */
export function Drawer({ open, title, children, confirmLabel = 'Kaydet', cancelLabel = 'Vazgeç', onConfirm, onClose, busy = false }) {
  if (!open) return null
  return (
    <div className="pim-drawer__scrim" onMouseDown={(e) => { if (e.target === e.currentTarget) onClose && onClose() }}>
      <div className="pim-drawer" role="dialog" aria-modal="true" aria-label={typeof title === 'string' ? title : undefined}>
        <div className="pim-drawer__header">
          <div className="pim-drawer__title">{title}</div>
        </div>
        <div className="pim-drawer__body">{children}</div>
        <div className="pim-drawer__footer">
          <Button variant="secondary" onClick={onClose}>{cancelLabel}</Button>
          <Button variant="primary" loading={busy} onClick={onConfirm}>{confirmLabel}</Button>
        </div>
      </div>
    </div>
  )
}
