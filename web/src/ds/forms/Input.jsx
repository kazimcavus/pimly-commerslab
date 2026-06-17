import React from 'react';

/**
 * pimly Input — text field. Supports a leading icon, a trailing suffix
 * (e.g. "₺", "cm"), mono mode (SKU/barcode), and invalid state.
 */
export function Input({
  icon,
  suffix,
  mono = false,
  invalid = false,
  size = 'md',
  className = '',
  ...rest
}) {
  const input = (
    <input
      className={[
        'pim-input',
        mono ? 'pim-input--mono' : '',
        invalid ? 'pim-input--invalid' : '',
        size === 'sm' ? 'pim-input--sm' : '',
        className,
      ].filter(Boolean).join(' ')}
      aria-invalid={invalid || undefined}
      {...rest}
    />
  );

  if (!icon && !suffix) return input;

  return (
    <div className="pim-input-group">
      {icon && <span className="pim-input-group__icon">{icon}</span>}
      {input}
      {suffix && <span className="pim-input-group__suffix">{suffix}</span>}
    </div>
  );
}
