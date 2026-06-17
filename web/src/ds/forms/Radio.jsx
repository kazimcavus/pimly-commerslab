import React from 'react';

/** pimly Radio — single-choice control. Group by sharing a `name`. */
export function Radio({ label, hint, className = '', ...rest }) {
  return (
    <label className={`pim-check pim-check--radio ${className}`.trim()}>
      <input type="radio" {...rest} />
      <span className="pim-check__box"><span className="pim-check__dot" /></span>
      {(label || hint) && (
        <span className="pim-check__text">
          {label && <span>{label}</span>}
          {hint && <span className="pim-check__hint">{hint}</span>}
        </span>
      )}
    </label>
  );
}
