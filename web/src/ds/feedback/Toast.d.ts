import * as React from 'react';
export interface ToastProps {
  tone?: 'success' | 'danger' | 'info';
  title: React.ReactNode;
  children?: React.ReactNode;
  onClose?: () => void;
  className?: string;
}
export declare function Toast(props: ToastProps): React.JSX.Element;
