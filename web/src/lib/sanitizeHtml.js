// Minik, bağımlılıksız HTML temizleyici. Zengin metin açıklamalarında (özellikle
// Trendyol import'undan gelen HTML) yalnızca güvenli/anlamlı etiketlere izin verir;
// script/style, on* olay öznitelikleri ve javascript: url'leri temizlenir.
// DOMParser tarayıcıda mevcut; ağır bir DOMPurify bağımlılığına gerek yok.

const ALLOWED_TAGS = new Set([
  'p', 'br', 'b', 'strong', 'i', 'em', 'u', 's',
  'ul', 'ol', 'li', 'a', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'span', 'div', 'blockquote', 'pre', 'code',
  'img', 'table', 'thead', 'tbody', 'tr', 'td', 'th', 'caption', 'colgroup', 'col',
  'iframe', 'video', 'source',
])

// Etiket başına izinli öznitelikler; listelenmeyen etiket için yalnızca güvenli `style` tutulur.
const ALLOWED_ATTRS = {
  a: ['href', 'title', 'target', 'rel'],
  img: ['src', 'alt', 'width', 'height'],
  td: ['colspan', 'rowspan'],
  th: ['colspan', 'rowspan'],
  col: ['span'],
  iframe: ['src', 'width', 'height', 'allowfullscreen', 'frameborder'],
  video: ['src', 'controls', 'width', 'height', 'poster'],
  source: ['src', 'type'],
}

// iframe (video gömme) yalnızca bilinen güvenli hostlardan; img/video src yerel /media ya da https.
const IFRAME_HOSTS = /^https:\/\/(www\.youtube-nocookie\.com|www\.youtube\.com|player\.vimeo\.com|www\.dailymotion\.com)\//i
const safeMediaSrc = (v) => /^\/media\//.test(v) || /^https:\/\//i.test(v)

// Inline style'da izinli CSS özellikleri (hizalama/renk/temel biçim). Diğerleri atılır.
const ALLOWED_STYLE_PROPS = new Set(['text-align', 'color', 'background-color', 'font-weight', 'font-style', 'text-decoration'])

// `color:` değeri güvenli mi? (url()/expression() gibi kaçışları reddet.)
const safeCssValue = (v) => !/url\(|expression\(|javascript:|@import/i.test(v)

function sanitizeStyle(style) {
  const kept = []
  for (const decl of style.split(';')) {
    const idx = decl.indexOf(':')
    if (idx === -1) continue
    const prop = decl.slice(0, idx).trim().toLowerCase()
    const val = decl.slice(idx + 1).trim()
    if (ALLOWED_STYLE_PROPS.has(prop) && val && safeCssValue(val)) kept.push(`${prop}: ${val}`)
  }
  return kept.join('; ')
}

function cleanNode(node, doc) {
  // Element dışı düğümler (metin, yorum): yorumları at, metni koru.
  if (node.nodeType === 8) { node.remove(); return }
  if (node.nodeType !== 1) return

  let tag = node.tagName.toLowerCase()

  // <font color="..."> → renkli <span style="color:..."> (execCommand foreColor bazı tarayıcılarda böyle üretir).
  if (tag === 'font') {
    const span = doc.createElement('span')
    const color = node.getAttribute('color')
    if (color && safeCssValue(color)) span.setAttribute('style', `color: ${color}`)
    while (node.firstChild) span.appendChild(node.firstChild)
    node.parentNode.replaceChild(span, node)
    cleanNode(span, doc)
    return
  }

  if (!ALLOWED_TAGS.has(tag)) {
    // İzinsiz etiket: çocuklarını yerine koyup kendisini kaldır (metni kaybetme).
    const parent = node.parentNode
    while (node.firstChild) parent.insertBefore(node.firstChild, node)
    parent.removeChild(node)
    return
  }

  // iframe yalnızca bilinen video hostlarından; değilse tamamen atılır (XSS'i önle).
  if (tag === 'iframe' && !IFRAME_HOSTS.test((node.getAttribute('src') || '').trim())) {
    node.remove(); return
  }

  // Öznitelikleri süz.
  const allowed = ALLOWED_ATTRS[tag] || []
  for (const attr of Array.from(node.attributes)) {
    const name = attr.name.toLowerCase()
    if (name === 'style') {
      const clean = sanitizeStyle(attr.value)
      if (clean) node.setAttribute('style', clean); else node.removeAttribute('style')
      continue
    }
    if (name === 'src') {
      const v = attr.value.trim()
      const ok = tag === 'iframe' ? IFRAME_HOSTS.test(v) : safeMediaSrc(v)
      if (!ok) node.removeAttribute('src')
      continue
    }
    if (name === 'align') {
      // eski align="center" → style
      const a = attr.value.trim().toLowerCase()
      node.removeAttribute('align')
      if (['left', 'center', 'right', 'justify'].includes(a)) {
        const cur = node.getAttribute('style') || ''
        node.setAttribute('style', sanitizeStyle(`${cur};text-align:${a}`))
      }
      continue
    }
    if (!allowed.includes(name)) { node.removeAttribute(attr.name); continue }
    if (name === 'href') {
      const v = attr.value.trim().toLowerCase()
      if (v.startsWith('javascript:') || v.startsWith('data:')) node.removeAttribute(attr.name)
    }
  }

  // Çocukları (canlı koleksiyon değişebileceği için kopya üzerinden) temizle.
  for (const child of Array.from(node.childNodes)) cleanNode(child, doc)
}

/** Verilen HTML'i izinli etiket/öznitelik allow-list'ine göre temizleyip döndürür. */
export function sanitizeHtml(html) {
  if (!html || typeof html !== 'string') return ''
  const doc = new DOMParser().parseFromString(html, 'text/html')
  for (const child of Array.from(doc.body.childNodes)) cleanNode(child, doc)
  return doc.body.innerHTML
}

/** Zengin metin boş mu (etiketler soyulunca görünür içerik kalmıyor mu)? */
export function isHtmlEmpty(html) {
  if (!html) return true
  const text = html.replace(/<[^>]*>/g, '').replace(/&nbsp;/g, ' ').trim()
  return text.length === 0
}
