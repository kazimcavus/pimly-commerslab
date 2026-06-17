import * as React from 'react';
export type BannerTone = 'info' | 'success' | 'warning' | 'danger';
/**
 * Inline contextual message — page notices, readonly mode, validation summaries,
 * sync status. Mirrors the API error envelope for danger banners.
 *
 * @startingPoint section="Feedback" subtitle="Banners, toasts, tooltips, empty states" viewport="700x320"
 */
export interface BannerProps {
  tone?: BannerTone;
  title?: React.ReactNode;
  /** Override the default Lucide icon name. */
  icon?: string;
  children?: React.ReactNode;
  className?: string;
}
export declare function Banner(props: BannerProps): React.JSX.Element;
