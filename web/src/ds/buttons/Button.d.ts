import * as React from 'react';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'accent' | 'danger' | 'danger-solid';
export type ButtonSize = 'sm' | 'md' | 'lg';

/**
 * Primary call-to-action and standard buttons for pimly.
 *
 * `primary` = ink/near-black (the main commit action, e.g. "Kaydet").
 * `accent`  = emerald (affirmative create, e.g. "Ürün Oluştur").
 * `secondary` = bordered neutral. `ghost` = borderless. `danger`/`danger-solid` = destructive.
 *
 * @startingPoint section="Buttons" subtitle="Ink primary + emerald accent button set" viewport="700x150"
 */
export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  /** Icon node rendered before the label (e.g. a Lucide <i>/<svg>). */
  iconLeft?: React.ReactNode;
  /** Icon node rendered after the label. */
  iconRight?: React.ReactNode;
  /** Shows a spinner and disables the button. */
  loading?: boolean;
  fullWidth?: boolean;
}

export declare function Button(props: ButtonProps): React.JSX.Element;
