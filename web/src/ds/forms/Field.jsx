import React from 'react';

/**
 * pimly Field — label + help/error/auto-generate scaffolding around any control.
 * Use it to wrap Input, Select, Textarea, etc. so the dynamic attribute forms
 * stay consistent (required markers, validation messages, "otomatik üretilecek").
 */
export function Field({
  label,
  htmlFor,
  required = false,
  optional = false,
  help,
  error,
  auto,
  children,
  className = '',
}) {
  return (
    <div className={`pim-field ${className}`.trim()}>
      {label && (
        <label className="pim-field__label" htmlFor={htmlFor}>
          {label}
          {required && <span className="pim-field__req" aria-hidden="true">*</span>}
          {optional && !required && <span className="pim-field__opt">(opsiyonel)</span>}
        </label>
      )}
      {children}
      {auto && !error && <span className="pim-field__auto">{auto}</span>}
      {help && !error && <span className="pim-field__help">{help}</span>}
      {error && (
        <span className="pim-field__error">
          <i data-lucide="alert-circle" style={{ width: 13, height: 13 }} />
          {error}
        </span>
      )}
    </div>
  );
}
