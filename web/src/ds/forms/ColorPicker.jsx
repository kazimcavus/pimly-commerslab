import React, { useEffect, useRef, useState } from 'react'

// Design-consistent HSV color picker: saturation/value square + hue slider +
// hex input. Controlled via `value` (hex) / `onChange`.

function hexToHsv(hex) {
  let h = (hex || '').replace('#', '')
  if (h.length === 3) h = h.split('').map((c) => c + c).join('')
  if (h.length !== 6) return { h: 0, s: 0, v: 0.83 }
  const r = parseInt(h.slice(0, 2), 16) / 255
  const g = parseInt(h.slice(2, 4), 16) / 255
  const b = parseInt(h.slice(4, 6), 16) / 255
  const max = Math.max(r, g, b), min = Math.min(r, g, b), d = max - min
  let hue = 0
  if (d !== 0) {
    if (max === r) hue = ((g - b) / d) % 6
    else if (max === g) hue = (b - r) / d + 2
    else hue = (r - g) / d + 4
    hue *= 60
    if (hue < 0) hue += 360
  }
  return { h: hue, s: max === 0 ? 0 : d / max, v: max }
}

function hsvToHex({ h, s, v }) {
  const c = v * s, x = c * (1 - Math.abs(((h / 60) % 2) - 1)), m = v - c
  let r, g, b
  if (h < 60) [r, g, b] = [c, x, 0]
  else if (h < 120) [r, g, b] = [x, c, 0]
  else if (h < 180) [r, g, b] = [0, c, x]
  else if (h < 240) [r, g, b] = [0, x, c]
  else if (h < 300) [r, g, b] = [x, 0, c]
  else [r, g, b] = [c, 0, x]
  const to = (n) => Math.round((n + m) * 255).toString(16).padStart(2, '0')
  return '#' + to(r) + to(g) + to(b)
}

const clamp = (n) => Math.min(1, Math.max(0, n))

export function ColorPicker({ value, onChange }) {
  const [hsv, setHsv] = useState(() => hexToHsv(value))
  const svRef = useRef(null)
  const hueRef = useRef(null)
  const draggingRef = useRef(null)

  // Sync from an external value change (e.g. typed hex) without clobbering hue.
  useEffect(() => {
    if (hsvToHex(hsv).toLowerCase() !== (value || '').toLowerCase()) setHsv(hexToHsv(value))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value])

  const emit = (next) => { setHsv(next); onChange?.(hsvToHex(next)) }

  const onSvMove = (e) => {
    const r = svRef.current.getBoundingClientRect()
    emit({ ...hsv, s: clamp((e.clientX - r.left) / r.width), v: clamp(1 - (e.clientY - r.top) / r.height) })
  }
  const onHueMove = (e) => {
    const r = hueRef.current.getBoundingClientRect()
    emit({ ...hsv, h: clamp((e.clientX - r.left) / r.width) * 360 })
  }

  useEffect(() => {
    const move = (e) => { if (draggingRef.current === 'sv') onSvMove(e); else if (draggingRef.current === 'hue') onHueMove(e) }
    const up = () => { draggingRef.current = null }
    window.addEventListener('pointermove', move)
    window.addEventListener('pointerup', up)
    return () => { window.removeEventListener('pointermove', move); window.removeEventListener('pointerup', up) }
  })

  const hex = hsvToHex(hsv)
  const hueColor = hsvToHex({ h: hsv.h, s: 1, v: 1 })

  return (
    <div className="pim-cp" onMouseDown={(e) => e.stopPropagation()}>
      <div
        ref={svRef}
        className="pim-cp__sv"
        style={{ background: `linear-gradient(to top, #000, transparent), linear-gradient(to right, #fff, transparent), ${hueColor}` }}
        onPointerDown={(e) => { draggingRef.current = 'sv'; onSvMove(e) }}
      >
        <div className="pim-cp__thumb" style={{ left: `${hsv.s * 100}%`, top: `${(1 - hsv.v) * 100}%`, background: hex }} />
      </div>
      <div ref={hueRef} className="pim-cp__hue" onPointerDown={(e) => { draggingRef.current = 'hue'; onHueMove(e) }}>
        <div className="pim-cp__huethumb" style={{ left: `${(hsv.h / 360) * 100}%`, background: hueColor }} />
      </div>
      <div className="pim-cp__hex">
        <span className="swatch-sm" style={{ background: hex, width: 18, height: 18 }} />
        <input
          className="pim-input pim-input--sm pim-input--mono"
          value={hex}
          onChange={(e) => {
            const val = e.target.value
            onChange?.(val)
            if (/^#?[0-9a-fA-F]{6}$/.test(val)) setHsv(hexToHsv(val.startsWith('#') ? val : '#' + val))
          }}
        />
      </div>
    </div>
  )
}
