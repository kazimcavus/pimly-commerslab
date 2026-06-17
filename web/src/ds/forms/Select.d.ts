import * as React from 'react';

export interface SelectOption { value: string; label: string; }

/** Native select styled with the brand chevron. Provide `options` or `<option>` children. */
export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  options?: SelectOption[];
  placeholder?: string;
  invalid?: boolean;
}
export declare function Select(props: SelectProps): React.JSX.Element;
