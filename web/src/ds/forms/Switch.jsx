import React from 'react';

/** pimly Switch — on/off toggle (e.g. module flags, taslak↔aktif quick toggle). */
export function Switch({ label, className = '', ...rest }) {
  return (
    <label className={`pim-switch ${className}`.trim()}>
      <input type="checkbox" role="switch" {...rest} />
      <span className="pim-switch__track"><span className="pim-switch__thumb" /></span>
      {label && <span className="pim-switch__label">{label}</span>}
    </label>
  );
}
