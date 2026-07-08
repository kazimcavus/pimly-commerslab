import React, { useEffect, useRef, useState } from 'react';
import {
  Bold, Italic, Underline, Strikethrough, List, ListOrdered, Link2, RemoveFormatting,
  AlignLeft, AlignCenter, AlignRight, AlignJustify, Code, Eye, Maximize2, Minimize2,
  Type, Baseline, Table as TableIcon, Image as ImageIcon, Video, ChevronDown,
} from 'lucide-react';
import { sanitizeHtml } from '../../lib/sanitizeHtml.js';
import { ColorPicker } from './ColorPicker.jsx';

// Google-docs benzeri kompakt palet (gri satırı + renk satırları).
const PALETTE = [
  '#000000', '#434343', '#666666', '#999999', '#b7b7b7', '#cccccc', '#e0e0e0', '#ffffff',
  '#980000', '#ff0000', '#ff9900', '#ffd966', '#00a651', '#00b0f0', '#0070c0', '#7030a0',
  '#e6b8af', '#f4cccc', '#fce5cd', '#fff2cc', '#d9ead3', '#d0e0e3', '#cfe2f3', '#ead1dc',
  '#dd7e6b', '#ea9999', '#f9cb9c', '#ffe599', '#b6d7a8', '#a2c4c9', '#9fc5e8', '#d5a6bd',
];

const BLOCKS = [
  { tag: 'p', label: 'Normal', style: { fontSize: 14 } },
  { tag: 'h1', label: 'Başlık 1', style: { fontSize: 22, fontWeight: 700 } },
  { tag: 'h2', label: 'Başlık 2', style: { fontSize: 18, fontWeight: 700 } },
  { tag: 'h3', label: 'Başlık 3', style: { fontSize: 16, fontWeight: 700 } },
  { tag: 'h4', label: 'Başlık 4', style: { fontSize: 14, fontWeight: 700 } },
  { tag: 'blockquote', label: 'Alıntı', style: { fontSize: 14, fontStyle: 'italic', color: 'var(--text-muted)' } },
  { tag: 'pre', label: 'Kod', style: { fontFamily: 'var(--font-mono)', fontSize: 13 } },
];

// Video URL → gömülü oynatıcı (YouTube/Vimeo iframe veya doğrudan video dosyası).
function videoEmbed(url) {
  if (!url) return '';
  let m;
  if ((m = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([\w-]{11})/)))
    return `<p><iframe src="https://www.youtube-nocookie.com/embed/${m[1]}" width="560" height="315" frameborder="0" allowfullscreen></iframe></p>`;
  if ((m = url.match(/vimeo\.com\/(?:video\/)?(\d+)/)))
    return `<p><iframe src="https://player.vimeo.com/video/${m[1]}" width="560" height="315" frameborder="0" allowfullscreen></iframe></p>`;
  if (/\.(mp4|webm|ogg)(\?|#|$)/i.test(url))
    return `<p><video src="${url}" controls></video></p>`;
  return `<p><a href="${url}">${url}</a></p>`;
}

/**
 * pimly RichText — bağımlılıksız zengin metin editörü (contentEditable + araç çubuğu).
 * Modlar: WYSIWYG ve HTML (code view). Başlık/renk/hizalama/liste/link/tablo/görsel/video.
 * `uploadImage(file)` verilirse görsel yükleme etkinleşir. HTML `value` sanitize edilerek
 * seed edilir, her değişimde `onChange(html)` yayar; Trendyol import HTML'ini render eder.
 */
export function RichText({ value = '', onChange, placeholder = 'Açıklama…', minHeight = 200, uploadImage }) {
  const ref = useRef(null);
  const fileRef = useRef(null);
  const [mode, setMode] = useState('rich');
  const [full, setFull] = useState(false);
  const [code, setCode] = useState('');
  const [menu, setMenu] = useState(null);          // 'block'|'color'|'align'|'table'|'video'|null
  const [hover, setHover] = useState({ r: 0, c: 0 });
  const [videoUrl, setVideoUrl] = useState('');
  const [custom, setCustom] = useState('#2563eb');
  const selRef = useRef(null);

  // Seçili aralığı sakla/geri yükle: renk popover'ında picker'la etkileşince editör
  // seçimi kaybolmasın (execCommand seçime uygulanır).
  const saveSel = () => {
    const s = window.getSelection();
    if (s && s.rangeCount && ref.current && ref.current.contains(s.anchorNode)) selRef.current = s.getRangeAt(0).cloneRange();
  };
  const restoreSel = () => {
    ref.current?.focus();
    const r = selRef.current; if (!r) return;
    const s = window.getSelection(); s.removeAllRanges(); s.addRange(r);
  };
  const applyColor = (cmd, color) => {
    restoreSel();
    document.execCommand('styleWithCSS', false, true);
    document.execCommand(cmd, false, color);
    emit();
  };

  useEffect(() => {
    if (mode !== 'rich') return;
    const el = ref.current;
    if (!el || document.activeElement === el) return;
    const incoming = sanitizeHtml(value || '');
    if (incoming !== el.innerHTML) el.innerHTML = incoming;
  }, [value, mode]);

  const emit = () => { const el = ref.current; if (el) onChange?.(sanitizeHtml(el.innerHTML)); };

  const run = (cmd, arg = null) => {
    ref.current?.focus();
    document.execCommand('styleWithCSS', false, true); // renk/vurgu inline style üretsin
    document.execCommand(cmd, false, arg);
    emit();
  };
  const insertHTML = (html) => { ref.current?.focus(); document.execCommand('insertHTML', false, html); emit(); };

  const addLink = () => { const u = window.prompt('Bağlantı adresi (https://…)'); if (u) run('createLink', u.trim()); };
  const onPickImage = async (e) => {
    const f = e.target.files?.[0]; e.target.value = '';
    if (!f || !uploadImage) return;
    try { const url = await uploadImage(f); if (url) insertHTML(`<img src="${url}" alt="" />`); } catch { /* sessiz */ }
  };
  const insertTable = (rows, cols) => {
    let html = '<table><tbody>';
    for (let r = 0; r < rows; r++) { html += '<tr>'; for (let c = 0; c < cols; c++) html += '<td><br></td>'; html += '</tr>'; }
    html += '</tbody></table><p><br></p>';
    insertHTML(html); setMenu(null);
  };
  const addVideo = () => { const html = videoEmbed(videoUrl.trim()); if (html) { insertHTML(html); setVideoUrl(''); setMenu(null); } };

  const toCode = () => { setCode(sanitizeHtml(ref.current ? ref.current.innerHTML : value || '')); setMode('code'); setMenu(null); };
  const toRich = () => {
    const clean = sanitizeHtml(code); setMode('rich'); onChange?.(clean);
    requestAnimationFrame(() => { if (ref.current) ref.current.innerHTML = clean; });
  };

  const toggle = (name) => setMenu((m) => (m === name ? null : name));
  const btn = (Icon, title, onClick, active) => (
    <button type="button" className="pim-richtext__btn" data-on={active || undefined}
      title={title} onMouseDown={(e) => e.preventDefault()} onClick={onClick} disabled={mode === 'code' && title !== 'Görünüm' && title !== 'Tam ekran' && title !== 'Küçült'}>
      <Icon size={15} strokeWidth={2} />
    </button>
  );
  return (
    <div className={`pim-richtext${full ? ' pim-richtext--full' : ''}`}>
      <div className="pim-richtext__toolbar">
        {/* Blok biçimi */}
        <span className="pim-richtext__mw">
          <button type="button" className="pim-richtext__btn pim-richtext__btn--menu" data-on={menu === 'block' || undefined}
            title="Paragraf biçimi" onMouseDown={(e) => e.preventDefault()} onClick={() => toggle('block')} disabled={mode === 'code'}>
            <Type size={15} /><ChevronDown size={12} />
          </button>
          {menu === 'block' && (
            <div className="pim-richtext__pop pim-richtext__pop--block">
              {BLOCKS.map((b) => (
                <button key={b.tag} type="button" className="pim-richtext__blockopt"
                  onMouseDown={(e) => e.preventDefault()} onClick={() => { run('formatBlock', b.tag); setMenu(null); }}>
                  <span style={b.style}>{b.label}</span>
                </button>
              ))}
            </div>
          )}
        </span>
        <span className="pim-richtext__sep" />
        {btn(Bold, 'Kalın', () => run('bold'))}
        {btn(Italic, 'İtalik', () => run('italic'))}
        {btn(Underline, 'Altı çizili', () => run('underline'))}
        {btn(Strikethrough, 'Üstü çizili', () => run('strikeThrough'))}

        {/* Renk */}
        <span className="pim-richtext__mw">
          <button type="button" className="pim-richtext__btn pim-richtext__btn--menu" data-on={menu === 'color' || undefined}
            title="Renk" onMouseDown={(e) => e.preventDefault()} onClick={() => { saveSel(); toggle('color'); }} disabled={mode === 'code'}>
            <Baseline size={15} /><ChevronDown size={12} />
          </button>
          {menu === 'color' && (
            <div className="pim-richtext__pop pim-richtext__pop--color" onMouseDown={(e) => e.stopPropagation()}>
              <div className="pim-richtext__collabel">Metin rengi</div>
              <div className="pim-richtext__swatches">
                {PALETTE.map((c) => <button key={c} type="button" className="pim-richtext__csw" style={{ background: c }} title={c}
                  onMouseDown={(e) => e.preventDefault()} onClick={() => { applyColor('foreColor', c); setMenu(null); }} />)}
              </div>
              <div className="pim-richtext__collabel">Arka plan (vurgu)</div>
              <div className="pim-richtext__swatches">
                {PALETTE.map((c) => <button key={c} type="button" className="pim-richtext__csw" style={{ background: c }} title={c}
                  onMouseDown={(e) => e.preventDefault()} onClick={() => { applyColor('hiliteColor', c); setMenu(null); }} />)}
              </div>
              <div className="pim-richtext__collabel">Özel renk</div>
              <ColorPicker value={custom} onChange={setCustom} />
              <div className="hstack" style={{ gap: 6, marginTop: 8 }}>
                <button type="button" className="pim-richtext__clearcolor" style={{ marginTop: 0 }} onMouseDown={(e) => e.preventDefault()}
                  onClick={() => { applyColor('foreColor', custom); setMenu(null); }}>Metne uygula</button>
                <button type="button" className="pim-richtext__clearcolor" style={{ marginTop: 0 }} onMouseDown={(e) => e.preventDefault()}
                  onClick={() => { applyColor('hiliteColor', custom); setMenu(null); }}>Arka plana</button>
              </div>
              <button type="button" className="pim-richtext__clearcolor" onMouseDown={(e) => e.preventDefault()}
                onClick={() => { restoreSel(); run('removeFormat'); setMenu(null); }}>Rengi temizle</button>
            </div>
          )}
        </span>
        <span className="pim-richtext__sep" />
        {btn(List, 'Madde listesi', () => run('insertUnorderedList'))}
        {btn(ListOrdered, 'Sıralı liste', () => run('insertOrderedList'))}

        {/* Hizalama */}
        <span className="pim-richtext__mw">
          <button type="button" className="pim-richtext__btn pim-richtext__btn--menu" data-on={menu === 'align' || undefined}
            title="Hizalama" onMouseDown={(e) => e.preventDefault()} onClick={() => toggle('align')} disabled={mode === 'code'}>
            <AlignLeft size={15} /><ChevronDown size={12} />
          </button>
          {menu === 'align' && (
            <div className="pim-richtext__pop pim-richtext__pop--align">
              {btn(AlignLeft, 'Sola', () => { run('justifyLeft'); setMenu(null); })}
              {btn(AlignCenter, 'Ortala', () => { run('justifyCenter'); setMenu(null); })}
              {btn(AlignRight, 'Sağa', () => { run('justifyRight'); setMenu(null); })}
              {btn(AlignJustify, 'İki yana', () => { run('justifyFull'); setMenu(null); })}
            </div>
          )}
        </span>
        <span className="pim-richtext__sep" />
        {btn(Link2, 'Bağlantı ekle', addLink)}
        {uploadImage && btn(ImageIcon, 'Görsel yükle', () => fileRef.current?.click())}
        {btn(Video, 'Video ekle', () => toggle('video'))}

        {/* Tablo */}
        <span className="pim-richtext__mw">
          <button type="button" className="pim-richtext__btn pim-richtext__btn--menu" data-on={menu === 'table' || undefined}
            title="Tablo ekle" onMouseDown={(e) => e.preventDefault()} onClick={() => toggle('table')} disabled={mode === 'code'}>
            <TableIcon size={15} /><ChevronDown size={12} />
          </button>
          {menu === 'table' && (
            <div className="pim-richtext__pop pim-richtext__pop--table">
              <div className="pim-richtext__grid" onMouseLeave={() => setHover({ r: 0, c: 0 })}>
                {Array.from({ length: 8 }).map((_, r) => (
                  <div key={r} className="pim-richtext__grow">
                    {Array.from({ length: 8 }).map((_, c) => (
                      <span key={c} className="pim-richtext__gcell" data-on={r < hover.r && c < hover.c || undefined}
                        onMouseEnter={() => setHover({ r: r + 1, c: c + 1 })} onMouseDown={(e) => e.preventDefault()}
                        onClick={() => insertTable(r + 1, c + 1)} />
                    ))}
                  </div>
                ))}
              </div>
              <div className="list-meta" style={{ textAlign: 'center', marginTop: 4 }}>{hover.r || 0} × {hover.c || 0}</div>
            </div>
          )}
        </span>

        {menu === 'video' && (
          <span className="pim-richtext__mw">
            <div className="pim-richtext__pop pim-richtext__pop--video">
              <div className="list-meta" style={{ marginBottom: 6 }}>Video linki (YouTube, Vimeo, mp4)</div>
              <div className="hstack" style={{ gap: 6 }}>
                <input className="pim-input pim-input--sm" style={{ width: 220 }} placeholder="https://…"
                  value={videoUrl} onChange={(e) => setVideoUrl(e.target.value)}
                  onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addVideo(); } }} />
                <button type="button" className="pim-richtext__addbtn" onMouseDown={(e) => e.preventDefault()} onClick={addVideo}>Ekle</button>
              </div>
            </div>
          </span>
        )}

        <span style={{ marginLeft: 'auto' }} />
        {btn(RemoveFormatting, 'Biçimi temizle', () => run('removeFormat'))}
        {btn(mode === 'code' ? Eye : Code, mode === 'code' ? 'Görünüm' : 'HTML (kaynak)', mode === 'code' ? toRich : toCode, mode === 'code')}
        {btn(full ? Minimize2 : Maximize2, full ? 'Küçült' : 'Tam ekran', () => setFull((f) => !f))}
      </div>

      {menu && <div className="pim-richtext__scrim" onMouseDown={() => setMenu(null)} />}

      {mode === 'code' ? (
        <textarea className="pim-richtext__code" style={{ minHeight: full ? '70vh' : minHeight }} value={code} spellCheck={false}
          placeholder="<p>HTML içerik…</p>" onChange={(e) => { setCode(e.target.value); onChange?.(e.target.value); }}
          onBlur={() => { const c = sanitizeHtml(code); setCode(c); onChange?.(c); }} />
      ) : (
        <div ref={ref} className="pim-richtext__area" contentEditable suppressContentEditableWarning role="textbox"
          aria-multiline="true" data-placeholder={placeholder} style={{ minHeight: full ? '70vh' : minHeight }}
          onInput={emit} onBlur={emit} onMouseUp={saveSel} onKeyUp={saveSel} />
      )}
      <input ref={fileRef} type="file" accept="image/*" hidden onChange={onPickImage} />
    </div>
  );
}
