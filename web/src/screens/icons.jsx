import React from 'react'
import { Icon } from '../lib/icons.jsx'

// Render a Lucide glyph by kebab-case name as a real lucide-react component.
// `extra` may carry size/strokeWidth/style/className, e.g. I('plus', { size: 18 }).
export const I = (n, extra) => <Icon name={n} {...(extra || {})} />
