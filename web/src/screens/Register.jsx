import React, { useState } from 'react'
import { Button, Field, Input, Banner } from '../ds'
import { I } from './icons.jsx'

// Yeni müşteri kaydı: hesap + çalışma alanı (şirket) birlikte oluşturulur,
// kayıt sonrası otomatik giriş yapılıp Trendyol onboarding sihirbazına geçilir.
export function Register({ onSignUp, onShowLogin, error, loading }) {
  const [name, setName] = useState('')
  const [company, setCompany] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  const submit = (e) => {
    e.preventDefault()
    onSignUp({ name: name.trim() || null, tenant_name: company.trim() || null, email: email.trim(), password })
  }

  return (
    <div className="login">
      <div className="login__aside">
        <img src="/assets/pimly-wordmark-dark.svg" alt="pimly" style={{ height: 34, alignSelf: 'flex-start' }} />
        <div>
          <div style={{ fontSize: 32, fontWeight: 800, letterSpacing: '-.02em', lineHeight: 1.15, marginBottom: 14 }}>
            Dakikalar içinde<br />kataloğun hazır.
          </div>
          <p style={{ color: '#a79e91', fontSize: 15, lineHeight: 1.6, maxWidth: 360 }}>
            Hesabını aç, Trendyol mağazanı bağla — ürünlerin, kategorilerin ve varyantların otomatik içeri aktarılsın.
          </p>
        </div>
        <div style={{ display: 'flex', gap: 18, color: '#7e7568', fontSize: 13 }}>
          <span>PIM</span><span>·</span><span>Trendyol-ready</span><span>·</span><span>v1</span>
        </div>
        <img className="login__layers" src="/assets/pimly-mark.svg" alt="" style={{ width: 320 }} />
      </div>
      <div className="login__form">
        <form className="login__card" onSubmit={submit}>
          <div>
            <div style={{ fontSize: 22, fontWeight: 700, color: 'var(--text-strong)', letterSpacing: '-.02em' }}>Hesap oluştur</div>
            <div style={{ fontSize: 14, color: 'var(--text-muted)', marginTop: 5 }}>Ücretsiz başla — kredi kartı gerekmez.</div>
          </div>
          {error && <Banner tone="danger" title="Kayıt başarısız">{error}</Banner>}
          <Field label="Adın">
            <Input value={name} onChange={(e) => setName(e.target.value)} icon={I('user')} placeholder="Ör. Kâzım Çavuş" />
          </Field>
          <Field label="Şirket / mağaza adı" hint="Çalışma alanının adı olur; sonradan değiştirilebilir.">
            <Input value={company} onChange={(e) => setCompany(e.target.value)} icon={I('store')} placeholder="Ör. Acme Tekstil" />
          </Field>
          <Field label="E-posta" required>
            <Input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} icon={I('mail')} />
          </Field>
          <Field label="Şifre" required hint="En az 8 karakter.">
            <Input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} icon={I('lock')} />
          </Field>
          <Button variant="primary" fullWidth type="submit" loading={loading}>Hesabımı oluştur</Button>
          <div style={{ textAlign: 'center', fontSize: 13, color: 'var(--text-muted)' }}>
            Zaten hesabın var mı? <a href="#" onClick={(e) => { e.preventDefault(); onShowLogin?.() }}>Giriş yap</a>
          </div>
        </form>
      </div>
    </div>
  )
}
