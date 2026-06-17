import * as React from 'react';
export interface EmptyStateProps {
  /** Lucide icon name. */
  icon?: string;
  title?: React.ReactNode;
  description?: React.ReactNode;
  /** Usually a primary/accent Button — the one next step. */
  action?: React.ReactNode;
  className?: string;
}
export declare function EmptyState(props: EmptyStateProps): React.JSX.Element;
