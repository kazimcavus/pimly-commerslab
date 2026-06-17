import * as React from 'react';
export interface RadioProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: React.ReactNode;
  hint?: React.ReactNode;
}
export declare function Radio(props: RadioProps): React.JSX.Element;
