import * as React from 'react';
export interface TabItem { value: string; label: React.ReactNode; icon?: string; count?: number; }
/**
 * Underline tabs for section switching.
 * @startingPoint section="Navigation" subtitle="Underline tabs" viewport="700x150"
 */
export interface TabsProps {
  tabs: TabItem[];
  value: string;
  onChange?: (value: string) => void;
  className?: string;
}
export declare function Tabs(props: TabsProps): React.JSX.Element;
