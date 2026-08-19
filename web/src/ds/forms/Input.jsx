import React from 'react';
import { Eye, EyeOff } from 'lucide-react';

/**
 * pimly Input — text field. Supports a leading icon, a trailing suffix
 * (e.g. "₺", "cm"), mono mode (SKU/barcode), and invalid state.
 * `reveal` on a password field adds the show/hide toggle; the browser's
 * own reveal control is suppressed in CSS so only ours shows.
 */
export function Input({
  icon,
  suffix,
  reveal = false,
  mono = false,
  invalid = false,
  size = 'md',
  className = '',
  type = 'text',
  ...rest
}) {
  const [revealed, setRevealed] = React.useState(false);
  const canReveal = reveal && type === 'password';

  const input = (
    <input
      type={canReveal && revealed ? 'text' : type}
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

  if (!icon && !suffix && !canReveal) return input;

  return (
    <div
      className={[
        'pim-input-group',
        icon ? 'pim-input-group--icon' : '',
        canReveal ? 'pim-input-group--reveal' : '',
      ].filter(Boolean).join(' ')}
    >
      {icon && <span className="pim-input-group__icon">{icon}</span>}
      {input}
      {suffix && <span className="pim-input-group__suffix">{suffix}</span>}
      {canReveal && (
        <button
          type="button"
          className="pim-input-group__reveal"
          onClick={() => setRevealed((v) => !v)}
          aria-label={revealed ? 'Şifreyi gizle' : 'Şifreyi göster'}
          aria-pressed={revealed}
        >
          {revealed ? <EyeOff /> : <Eye />}
        </button>
      )}
    </div>
  );
}
