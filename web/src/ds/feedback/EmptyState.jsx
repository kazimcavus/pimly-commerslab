import React from 'react';
import { Icon } from '../../lib/icons.jsx';

/**
 * pimly EmptyState — what-goes-here + the one next step. Icon is a Lucide name.
 */
export function EmptyState({ icon = 'package', title, description, action, className = '' }) {
  return (
    <div className={`pim-empty ${className}`.trim()}>
      <span className="pim-empty__icon"><Icon name={icon} /></span>
      {title && <div className="pim-empty__title">{title}</div>}
      {description && <div className="pim-empty__desc">{description}</div>}
      {action && <div style={{ marginTop: 4 }}>{action}</div>}
    </div>
  );
}
