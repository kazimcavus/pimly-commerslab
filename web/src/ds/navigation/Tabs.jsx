import React from 'react';
import { Icon } from '../../lib/icons.jsx';

/**
 * pimly Tabs — underline tabs for section switching (e.g. group detail:
 * Ürünler / Medya / Pazaryeri). Controlled via `value` + `onChange`.
 * `tabs` = [{ value, label, icon?, count? }].
 */
export function Tabs({ tabs = [], value, onChange, className = '' }) {
  return (
    <div className={`pim-tabs ${className}`.trim()} role="tablist">
      {tabs.map((t) => (
        <button
          key={t.value}
          type="button"
          role="tab"
          aria-selected={value === t.value}
          className="pim-tab"
          onClick={() => onChange && onChange(t.value)}
        >
          {t.icon && <Icon name={t.icon} />}
          {t.label}
          {t.count != null && <span className="pim-badge pim-badge--count">{t.count}</span>}
        </button>
      ))}
    </div>
  );
}
