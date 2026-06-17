import React from 'react';

/**
 * pimly Select — native select with the brand chevron. Pass `options`
 * as [{value, label}] or render <option> children directly.
 */
export function Select({ options, placeholder, invalid = false, className = '', children, ...rest }) {
  return (
    <div className="pim-select-wrap">
      <select
        className={['pim-select', invalid ? 'pim-select--invalid' : '', className].filter(Boolean).join(' ')}
        aria-invalid={invalid || undefined}
        {...rest}
      >
        {placeholder && <option value="" disabled>{placeholder}</option>}
        {options
          ? options.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)
          : children}
      </select>
      <span className="pim-select-wrap__chev"><i data-lucide="chevron-down" /></span>
    </div>
  );
}
