import React from 'react';

/**
 * pimly IconButton — square, icon-only button. Always pass `label`
 * (used as aria-label + tooltip). Same variants as Button.
 */
export function IconButton({
  icon,
  label,
  variant = 'ghost',
  size = 'md',
  disabled = false,
  className = '',
  ...rest
}) {
  const cls = [
    'pim-btn', 'pim-btn--icon',
    `pim-btn--${variant}`,
    size !== 'md' ? `pim-btn--${size}` : '',
    className,
  ].filter(Boolean).join(' ');

  return (
    <button type="button" className={cls} disabled={disabled} aria-label={label} title={label} {...rest}>
      <span className="pim-btn__icon">{icon}</span>
    </button>
  );
}
