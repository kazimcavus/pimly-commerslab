import React from 'react';
import { Check } from 'lucide-react';

/** pimly Checkbox — accent-filled box with hint text support. */
export function Checkbox({ label, hint, className = '', ...rest }) {
  return (
    <label className={`pim-check ${className}`.trim()}>
      <input type="checkbox" {...rest} />
      <span className="pim-check__box"><Check /></span>
      {(label || hint) && (
        <span className="pim-check__text">
          {label && <span>{label}</span>}
          {hint && <span className="pim-check__hint">{hint}</span>}
        </span>
      )}
    </label>
  );
}
