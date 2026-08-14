import React, { useEffect, useState } from 'react'
import { Dialog } from '../ds'

// Uygulama içi onay modali — tarayıcının confirm() penceresi yerine kullanılır
// (bkz. web/docs/ui-feedback.md). Ekranlar askConfirm() çağırır; App kökünde bir
// kez render edilen ConfirmHost istekleri DS Dialog'a çevirir ve sonucu döndürür.
//
//   const ok = await askConfirm({
//     title: 'Varyantı sil',
//     body: '"80 x 150" varyantı kalıcı olarak silinecek.',
//     tone: 'danger', confirmLabel: 'Sil',
//   })
//   if (!ok) return
let handler = null

export function askConfirm(opts) {
  const o = typeof opts === 'string' ? { title: opts } : (opts || {})
  // Host henüz mount olmadıysa (test/edge) tarayıcıya düş — davranış kaybolmasın.
  if (!handler) return Promise.resolve(window.confirm(o.body || o.title || 'Emin misin?'))
  return handler(o)
}

export function ConfirmHost() {
  const [req, setReq] = useState(null) // { opts, resolve }

  useEffect(() => {
    handler = (opts) => new Promise((resolve) => setReq({ opts, resolve }))
    return () => { handler = null }
  }, [])

  useEffect(() => {
    if (!req) return
    const onKey = (e) => { if (e.key === 'Escape') done(false) }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [req])

  if (!req) return null
  const { title, body, confirmLabel = 'Onayla', cancelLabel = 'Vazgeç', tone = 'default' } = req.opts
  const done = (ok) => { req.resolve(ok); setReq(null) }

  return (
    <Dialog
      open
      title={title}
      description={body}
      confirmLabel={confirmLabel}
      cancelLabel={cancelLabel}
      tone={tone}
      onConfirm={() => done(true)}
      onClose={() => done(false)}
    />
  )
}
