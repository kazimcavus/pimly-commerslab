import React, { useState } from 'react'
import { Button, Field, Input, Banner } from '../ds'
import { I } from './icons.jsx'

export function Login({ onSignIn, error, loading }) {
  const [email, setEmail] = useState('owner@acme.test')
  const [password, setPassword] = useState('')
  const [tenant, setTenant] = useState('')

  const submit = (e) => {
    e.preventDefault()
    onSignIn(email, password, tenant.trim())
  }

  return (
    <div className="login">
      <div className="login__aside">
        <img src="/assets/pimly-wordmark-dark.svg" alt="pimly" style={{ height: 34, alignSelf: 'flex-start' }} />
        <div>
          <div style={{ fontSize: 32, fontWeight: 800, letterSpacing: '-.02em', lineHeight: 1.15, marginBottom: 14 }}>
            Kataloğun<br />tek doğruluk kaynağı.
          </div>
          <p style={{ color: '#a79e91', fontSize: 15, lineHeight: 1.6, maxWidth: 360 }}>
            Kategoriler, özellikler ve varyantlar — kanonik kataloğunu kur, pazaryerlerine tek yerden eşle.
          </p>
        </div>
        <div style={{ display: 'flex', gap: 18, color: '#7e7568', fontSize: 13 }}>
          <span>Multi-tenant</span><span>·</span><span>Trendyol-ready</span><span>·</span><span>v1</span>
        </div>
        <img className="login__layers" src="/assets/pimly-mark.svg" alt="" style={{ width: 320 }} />
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
          <Field label="Tenant slug" optional help="Birden fazla mağazan varsa belirt.">
            <Input mono placeholder="acme-tekstil" value={tenant} onChange={(e) => setTenant(e.target.value)} />
          </Field>
          <Button variant="primary" fullWidth type="submit" loading={loading}>Giriş yap</Button>
          <div style={{ textAlign: 'center', fontSize: 13, color: 'var(--text-muted)' }}>
            Hesabın yok mu? <a href="#" onClick={(e) => e.preventDefault()}>Başvuru yap</a>
          </div>
        </form>
      </div>
    </div>
  )
}
