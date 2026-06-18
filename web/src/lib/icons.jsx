import * as Lucide from 'lucide-react'

// kebab-case Lucide name ("trash-2", "check-circle-2") -> PascalCase export
// key ("Trash2", "CheckCircle2"). We look the component up in the lucide-react
// module namespace (rather than its `icons` barrel) so legacy aliases the app
// still uses — alert-circle, more-horizontal, upload-cloud, etc. — keep working.
const toPascal = (name) =>
  String(name)
    .split('-')
    .map((p) => p.charAt(0).toUpperCase() + p.slice(1))
    .join('')

/**
 * Icon — renders a Lucide glyph by its kebab-case name as a real React
 * component. Because it returns an actual <svg> on every render, icons survive
 * React re-renders without any createIcons() pass. Sizing/stroke can be set via
 * the `size`/`strokeWidth` props or (where the design system does) via CSS on
 * the surrounding `svg`, which overrides these attributes.
 */
export function Icon({ name, size = 16, strokeWidth = 2, ...rest }) {
  const Glyph = Lucide[toPascal(name)]
  if (!Glyph) return null
  return <Glyph size={size} strokeWidth={strokeWidth} {...rest} />
}
