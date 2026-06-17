import * as React from 'react';
import type { ButtonVariant, ButtonSize } from './Button';

/**
 * Square, icon-only button. `label` is required for accessibility (aria-label + tooltip).
 */
export interface IconButtonProps extends Omit<React.ButtonHTMLAttributes<HTMLButtonElement>, 'children'> {
  icon: React.ReactNode;
  label: string;
  variant?: ButtonVariant;
  size?: ButtonSize;
}

export declare function IconButton(props: IconButtonProps): React.JSX.Element;
