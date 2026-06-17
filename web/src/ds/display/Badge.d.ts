import * as React from 'react';

export type BadgeStatus = 'draft' | 'active' | 'archived' | 'danger' | 'info';

/**
 * Status pill (colored dot + word) or neutral count badge. Status maps to the
 * group/product/variant lifecycle: draft=gri, active=yeşil, archived=sarı.
 *
 * @startingPoint section="Display" subtitle="Status badges, tags & avatars" viewport="700x150"
 */
export interface BadgeProps {
  status?: BadgeStatus;
  /** Render as a neutral monospace count (e.g. variant count). */
  count?: boolean;
  /** Show the leading status dot (default true). */
  dot?: boolean;
  children?: React.ReactNode;
  className?: string;
}
export declare function Badge(props: BadgeProps): React.JSX.Element;
