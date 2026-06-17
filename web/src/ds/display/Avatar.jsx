import React from 'react';

/** pimly Avatar — user initials or image. Used in the top bar / user menu. */
export function Avatar({ name = '', src, size = 'md', className = '' }) {
  const initials = name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0])
    .join('')
    .toUpperCase();
  return (
    <span className={['pim-avatar', size !== 'md' ? `pim-avatar--${size}` : '', className].filter(Boolean).join(' ')} title={name}>
      {src ? <img src={src} alt={name} /> : initials || '?'}
    </span>
  );
}
