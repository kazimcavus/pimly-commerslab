import React from 'react';

/** pimly Textarea — multi-line text input with invalid state. */
export function Textarea({ invalid = false, className = '', ...rest }) {
  return (
    <textarea
      className={['pim-textarea', invalid ? 'pim-textarea--invalid' : '', className].filter(Boolean).join(' ')}
      aria-invalid={invalid || undefined}
      {...rest}
    />
  );
}
