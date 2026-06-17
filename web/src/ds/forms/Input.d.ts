import * as React from 'react';

/** Text input with optional leading icon, trailing suffix, and mono mode. */
export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  icon?: React.ReactNode;
  /** Trailing affix, e.g. "₺" or "cm". */
  suffix?: React.ReactNode;
  /** Monospace + tabular figures — use for SKU, barcode, codes. */
  mono?: boolean;
  invalid?: boolean;
  size?: 'sm' | 'md';
}
export declare function Input(props: InputProps): React.JSX.Element;
