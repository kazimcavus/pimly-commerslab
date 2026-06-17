import * as React from 'react';
export interface TooltipProps {
  label: React.ReactNode;
  children?: React.ReactNode;
  className?: string;
}
export declare function Tooltip(props: TooltipProps): React.JSX.Element;
