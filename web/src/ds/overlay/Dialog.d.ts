import * as React from 'react';
/**
 * Centered modal dialog for confirmations and small forms.
 * @startingPoint section="Overlay" subtitle="Confirmation & small-form modal" viewport="700x420"
 */
export interface DialogProps {
  open: boolean;
  title?: React.ReactNode;
  description?: React.ReactNode;
  children?: React.ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm?: () => void;
  onClose?: () => void;
  /** `danger` styles the confirm button destructively. */
  tone?: 'default' | 'danger';
  /** Shows a spinner on the confirm button. */
  busy?: boolean;
}
export declare function Dialog(props: DialogProps): React.JSX.Element | null;
