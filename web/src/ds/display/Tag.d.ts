import * as React from 'react';
/** Compact chip for metaobject values / filters. Optional hex `swatch` + `onRemove`. */
export interface TagProps {
  children?: React.ReactNode;
  /** Hex color for a leading swatch (Renk metaobject). */
  swatch?: string;
  onRemove?: () => void;
  className?: string;
}
export declare function Tag(props: TagProps): React.JSX.Element;
