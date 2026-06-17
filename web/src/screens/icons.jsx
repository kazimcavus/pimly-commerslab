import React from 'react'

// Lucide placeholder; renderIcons() (in App) swaps these for SVGs after commit.
export const I = (n, extra) => <i data-lucide={n} {...(extra || {})} />
