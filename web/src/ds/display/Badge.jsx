import React from 'react';

/**
 * pimly Badge — status pill (draft/active/archived/danger/info) or a neutral
 * count. Status badges render a colored dot + word. `status` maps to the
 * group/product/variant lifecycle.
 */
export function Badge({ status, children, dot = true, count = false, className = '' }) {
  const cls = [
    'pim-badge',
    status ? `pim-badge--${status}` : '',
    count ? 'pim-badge--count' : '',
    className,
  ].filter(Boolean).join(' ');
  return (
    <span className={cls}>
      {status && dot && !count && <span className="pim-badge__dot" />}
      {children}
    </span>
  );
}
