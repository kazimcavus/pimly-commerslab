import React, { createContext, useCallback, useContext, useState } from 'react'
import { Button } from '../ds'
import { I } from '../screens/icons.jsx'
import { HELP } from './content.js'

// Bağlamsal yardım altyapısı: bir ⓘ ipucuna tıklayınca, ekranın sağından
// ilgili konunun yardım çekmecesi (açıklama + video + adımlar + ipuçları) açılır.
const HelpContext = createContext({ open: () => {} })

export function useHelp() {
  return useContext(HelpContext)
}

export function HelpProvider({ children }) {
  const [topic, setTopic] = useState(null)
  const open = useCallback((key) => setTopic(key), [])
  const close = useCallback(() => setTopic(null), [])
  return (
    <HelpContext.Provider value={{ open }}>
      {children}
      <HelpDrawer topic={topic} onClose={close} />
    </HelpContext.Provider>
  )
}

// Küçük ⓘ ipucu butonu — bir alanın/başlığın yanına konur.
export function HelpHint({ topic, label = 'Yardım', size }) {
  const { open } = useHelp()
  if (!HELP[topic]) return null
  return (
    <button
      type="button"
      className={`help-hint${size === 'lg' ? ' help-hint--lg' : ''}`}
      title={label}
      aria-label={label}
      onClick={(e) => { e.preventDefault(); e.stopPropagation(); open(topic) }}
    >
      {I('help-circle')}
    </button>
  )
}

function HelpDrawer({ topic, onClose }) {
  const data = topic && HELP[topic]
  if (!data) return null
  return (
    <div className="pim-drawer__scrim" onMouseDown={(e) => { if (e.target === e.currentTarget) onClose() }}>
      <div className="pim-drawer help-drawer" role="dialog" aria-modal="true" aria-label={data.title}>
        <div className="help-drawer__head">
          <span className="help-drawer__icon">{I('book-open')}</span>
          <div>
            {data.eyebrow && <div className="help-drawer__eyebrow">{data.eyebrow}</div>}
            <div className="help-drawer__title">{data.title}</div>
          </div>
          <button className="tb__icon help-drawer__close" title="Kapat" onClick={onClose}>{I('x')}</button>
        </div>
        <div className="help-drawer__body">
          {data.lead && <div className="help-drawer__lead">{data.lead}</div>}

          {data.video && (
            <button className="help-video" type="button" title="Video yakında">
              <span className="help-video__play">{I('play')}</span>
              <span className="help-video__label">{data.video}</span>
            </button>
          )}

          {data.steps?.length > 0 && (
            <div>
              <div className="help-section__title">Nasıl yapılır</div>
              <ol className="help-steps">
                {data.steps.map((s, i) => <li key={i}>{s}</li>)}
              </ol>
            </div>
          )}

          {data.tips?.length > 0 && (
            <div>
              <div className="help-section__title">İpuçları</div>
              <ul className="help-tips">
                {data.tips.map((t, i) => <li key={i}>{I('lightbulb')}<span>{t}</span></li>)}
              </ul>
            </div>
          )}
        </div>
        <div className="pim-drawer__footer">
          <Button variant="secondary" onClick={onClose}>Kapat</Button>
        </div>
      </div>
    </div>
  )
}
