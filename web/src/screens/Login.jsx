import React, { useState } from 'react'
import { Button, Field, Input, Banner } from '../ds'
import { I } from './icons.jsx'

export function Login({ onSignIn, onShowRegister, error, loading }) {
  const [email, setEmail] = useState('owner@acme.test')
  const [password, setPassword] = useState('')

  const submit = (e) => {
    e.preventDefault()
    onSignIn(email, password)
  }

  return (
    <div className="login">
      <div className="login__aside">
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, alignSelf: 'flex-start' }}>
          <img src="/brand/helpy-logo-dark.webp" alt="Helpy" style={{ height: 28, display: 'block' }} />
          <span className="brand-dot brand-dot--lg" />
          <span className="brand-suffix brand-suffix--lg" style={{ color: '#fff' }}>Connect</span>
        </div>
        <div>
          <div style={{ fontSize: 32, fontWeight: 800, letterSpacing: '-.02em', lineHeight: 1.15, marginBottom: 14 }}>
            Kataloğun<br />tek doğruluk kaynağı.
          </div>
          <p style={{ color: '#a79e91', fontSize: 15, lineHeight: 1.6, maxWidth: 360 }}>
            Kategoriler, özellikler ve varyantlar — kanonik kataloğunu kur, pazaryerlerine tek yerden eşle.
          </p>
        </div>
        <div style={{ display: 'flex', gap: 18, color: '#7e7568', fontSize: 13 }}>
          <span>PIM</span><span>·</span><span>Trendyol-ready</span><span>·</span><span>v1</span>
        </div>
      </div>
      <div className="login__form">
        <form className="login__card" onSubmit={submit}>
          <div>
            <div style={{ fontSize: 22, fontWeight: 700, color: 'var(--text-strong)', letterSpacing: '-.02em' }}>Giriş yap</div>
            <div style={{ fontSize: 14, color: 'var(--text-muted)', marginTop: 5 }}>Hesabınla mağazana eriş.</div>
          </div>
          {error && <Banner tone="danger" title="Giriş başarısız">{error}</Banner>}
          <Field label="E-posta">
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} icon={I('mail')} />
          </Field>
          <Field label="Şifre">
            <Input type="password" value={password} onChange={(e) => setPassword(e.target.value)} icon={I('lock')} />
          </Field>
          <Button variant="primary" fullWidth type="submit" loading={loading}>Giriş yap</Button>
          <div style={{ textAlign: 'center', fontSize: 13, color: 'var(--text-muted)' }}>
            Hesabın yok mu? <a href="#" onClick={(e) => { e.preventDefault(); onShowRegister?.() }}>Hesap oluştur</a>
          </div>
        </form>
      </div>
    </div>
  )
}
