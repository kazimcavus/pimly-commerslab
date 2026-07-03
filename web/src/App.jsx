import React, { useEffect, useState } from 'react'
import { Toast } from './ds'
import { api, setToken, getToken } from './lib/api.js'
import { AppShell } from './screens/Shell.jsx'
import { Login } from './screens/Login.jsx'
import { Register } from './screens/Register.jsx'
import { Dashboard } from './screens/Dashboard.jsx'
import { ProductList } from './screens/ProductList.jsx'
import { ProductBuilder } from './screens/ProductBuilder.jsx'
import { Categories } from './screens/Categories.jsx'
import { Attributes } from './screens/Attributes.jsx'
import { Variants } from './screens/Variants.jsx'
import { Settings } from './screens/Settings.jsx'
import { Channels } from './screens/Channels.jsx'
import { TrendyolOnboarding } from './screens/TrendyolOnboarding.jsx'
import { HelpProvider } from './help/Help.jsx'

export function App() {
  const [session, setSession] = useState(null) // { user: { id, email, name }, tenant: { id, name } }
  const [route, setRoute] = useState('dashboard')
  const [authView, setAuthView] = useState('login') // 'login' | 'register'
  const [toast, setToast] = useState(null)
  const [authError, setAuthError] = useState('')
  const [loading, setLoading] = useState(false)
  const [booting, setBooting] = useState(true)

  // Restore a session from a stored token on first load.
  // /me → MeDto: { user, tenant }.
  useEffect(() => {
    if (getToken()) {
      api.me()
        .then((m) => setSession({ user: m.user || m, tenant: m.tenant || null }))
        .catch(() => setToken(''))
        .finally(() => setBooting(false))
    } else {
      setBooting(false)
    }
  }, [])

  const navigate = (r) => {
    setRoute(r)
    document.querySelector('.app__content')?.scrollTo(0, 0)
  }

  const showToast = (t) => {
    setToast(t)
    clearTimeout(window.__pt)
    window.__pt = setTimeout(() => setToast(null), 3800)
  }

  const signIn = async (email, password) => {
    setLoading(true); setAuthError('')
    try {
      const r = await api.login(email, password)
      setToken(r.token)
      setSession({ user: r.user, tenant: r.tenant || null })
      setRoute('dashboard')
    } catch (e) {
      setAuthError(e.message || 'Giriş başarısız')
    } finally {
      setLoading(false)
    }
  }

  // Kayıt: hesap + çalışma alanı (tenant) oluşturur, otomatik giriş yapar
  // ve yeni müşteriyi Trendyol onboarding sihirbazına götürür.
  const signUp = async (form) => {
    setLoading(true); setAuthError('')
    try {
      const r = await api.register(form)
      setToken(r.token)
      setSession({ user: r.user, tenant: r.tenant || null })
      setRoute('onboarding')
    } catch (e) {
      setAuthError(e.message || 'Kayıt başarısız')
    } finally {
      setLoading(false)
    }
  }

  const logout = () => { setToken(''); setSession(null); setRoute('dashboard'); setAuthView('login') }

  const toggleTheme = () => {
    const el = document.documentElement
    el.setAttribute('data-theme', el.getAttribute('data-theme') === 'dark' ? 'light' : 'dark')
  }

  if (booting) return null
  if (!session) {
    return authView === 'register'
      ? <Register onSignUp={signUp} onShowLogin={() => { setAuthView('login'); setAuthError('') }} error={authError} loading={loading} />
      : <Login onSignIn={signIn} onShowRegister={() => { setAuthView('register'); setAuthError('') }} error={authError} loading={loading} />
  }

  const user = session.user
  const tenant = session.tenant

  const screens = {
    dashboard: <Dashboard onNavigate={navigate} user={user} />,
    products: <ProductList onNavigate={navigate} onToast={showToast} />,
    builder: <ProductBuilder onNavigate={navigate} onSaved={(msg) => { navigate('products'); showToast({ tone: 'success', title: 'Ürün kaydedildi', body: msg }) }} />,
    categories: <Categories onToast={showToast} />,
    attributes: <Attributes onToast={showToast} />,
    variants: <Variants onToast={showToast} />,
    settings: <Settings onToast={showToast} />,
    channels: <Channels onNavigate={navigate} onToast={showToast} />,
    onboarding: <TrendyolOnboarding onNavigate={navigate} onToast={showToast} />,
  }
  const navRoute = route === 'builder' ? 'products' : route === 'onboarding' ? 'channels' : route

  return (
    <HelpProvider>
      <AppShell route={navRoute} onNavigate={navigate} onLogout={logout} onToggleTheme={toggleTheme} user={user} tenant={tenant}>
        {screens[route] || screens.dashboard}
      </AppShell>
      {toast && (
        <div style={{ position: 'fixed', right: 20, bottom: 20, zIndex: 200 }}>
          <Toast tone={toast.tone} title={toast.title} onClose={() => setToast(null)}>{toast.body}</Toast>
        </div>
      )}
    </HelpProvider>
  )
}
