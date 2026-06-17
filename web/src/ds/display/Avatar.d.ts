import * as React from 'react';
/** User initials or image avatar (top bar / user menu). */
export interface AvatarProps {
  name?: string;
  src?: string;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}
export declare function Avatar(props: AvatarProps): React.JSX.Element;
