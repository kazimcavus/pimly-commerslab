import React from 'react';

/** pimly Tooltip — CSS hover/focus tooltip. Wraps a trigger; shows `label` above. */
export function Tooltip({ label, children, className = '' }) {
  return (
    <span className={`pim-tooltip-wrap ${className}`.trim()}>
      {children}
      <span className="pim-tooltip" role="tooltip">{label}</span>
    </span>
  );
}
