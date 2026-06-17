import React from 'react';

/**
 * pimly Tag — compact chip for metaobject values (Renk/Beden), selected
 * filters, etc. Optional color `swatch` (hex) and `onRemove`.
 */
export function Tag({ children, swatch, onRemove, className = '' }) {
  return (
    <span className={`pim-tag ${className}`.trim()}>
      {swatch && <span className="pim-tag__swatch" style={{ background: swatch }} />}
      {children}
      {onRemove && (
        <button type="button" className="pim-tag__x" onClick={onRemove} aria-label="Kaldır">
          <i data-lucide="x" />
        </button>
      )}
    </span>
  );
}
