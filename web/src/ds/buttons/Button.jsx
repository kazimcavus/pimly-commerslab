import React from 'react';

/**
 * pimly Button — primary action is ink/near-black; accent (emerald) for
 * affirmative create actions; secondary, ghost, and danger round out the set.
 */
export function Button({
  children,
  variant = 'secondary',
  size = 'md',
  iconLeft,
  iconRight,
  loading = false,
  disabled = false,
  fullWidth = false,
  type = 'button',
  className = '',
  ...rest
}) {
  const cls = [
    'pim-btn',
    `pim-btn--${variant}`,
    size !== 'md' ? `pim-btn--${size}` : '',
    fullWidth ? 'pim-btn--full' : '',
    className,
  ].filter(Boolean).join(' ');

  return (
    <button type={type} className={cls} disabled={disabled || loading} {...rest}>
      {loading && <span className="pim-btn__spinner" aria-hidden="true" />}
      {!loading && iconLeft && <span className="pim-btn__icon">{iconLeft}</span>}
      {children && <span className="pim-btn__label">{children}</span>}
      {!loading && iconRight && <span className="pim-btn__icon">{iconRight}</span>}
    </button>
  );
}
