import * as React from 'react';

/**
 * Field scaffolding: label, required/optional markers, help text, validation
 * error, and an "otomatik üretilecek" auto-generate hint. Wraps any control.
 *
 * @startingPoint section="Forms" subtitle="Dynamic attribute form controls" viewport="700x420"
 */
export interface FieldProps {
  label?: React.ReactNode;
  htmlFor?: string;
  required?: boolean;
  optional?: boolean;
  /** Helper text shown when there is no error. */
  help?: React.ReactNode;
  /** Validation error (mirrors the API error envelope message). Replaces help. */
  error?: React.ReactNode;
  /** Auto-generation hint, e.g. "Boş bırakılırsa otomatik üretilecek". */
  auto?: React.ReactNode;
  children?: React.ReactNode;
  className?: string;
}
export declare function Field(props: FieldProps): React.JSX.Element;
