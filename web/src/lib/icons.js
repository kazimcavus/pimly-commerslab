import { createIcons, icons } from 'lucide'

// The design-system components render Lucide glyphs as <i data-lucide="name" />.
// renderIcons() swaps those placeholders for SVGs; call it after each commit
// (see useIcons in App) to mirror the prototype's behavior.
export function renderIcons() {
  try {
    createIcons({ icons })
  } catch {
    /* ignore icons that fail to resolve */
  }
}
