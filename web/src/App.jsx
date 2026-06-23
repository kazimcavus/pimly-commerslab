import React, { useEffect, useState } from 'react'
import { Toast } from './ds'
import { api, setToken, getToken } from './lib/api.js'
import { AppShell } from './screens/Shell.jsx'
import { Login } from './screens/Login.jsx'
import { Dashboard } from './screens/Dashboard.jsx'
import { ProductList } from './screens/ProductList.jsx'
import { ProductBuilder } from './screens/ProductBuilder.jsx'
import { Categories } from './screens/Categories.jsx'
import { Attributes } from './screens/Attributes.jsx'
import { Variants } from './screens/Variants.jsx'
import { Settings } from './screens/Settings.jsx'
import { HelpProvider } from './help/Help.jsx'

export function App() {
  const [session, setSession] = useState(null) // { user: { id, email, name } }
  const [route, setRoute] = useState('dashboard')
  const [toast, setToast] = useState(null)
  const [authError, setAuthError] = useState('')
  const [loading, setLoading] = useState(false)
  const [booting, setBooting] = useState(true)

  // Restore a session from a stored token on first load.
  useEffect(() => {
    if (getToken()) {
      api.me().then((m) => setSession({ user: m })).catch(() => setToken('')).finally(() => setBooting(false))
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
      setSession({ user: r.user })
      setRoute('dashboard')
    } catch (e) {
      setAuthError(e.message || 'Giriş başarısız')
    } finally {
      setLoading(false)
    }
  }

  const logout = () => { setToken(''); setSession(null); setRoute('dashboard') }

  const toggleTheme = () => {
    const el = document.documentElement
    el.setAttribute('data-theme', el.getAttribute('data-theme') === 'dark' ? 'light' : 'dark')
  }

  if (booting) return null
  if (!session) return <Login onSignIn={signIn} error={authError} loading={loading} />

  const user = session.user

  const screens = {
    dashboard: <Dashboard onNavigate={navigate} user={user} />,
    products: <ProductList onNavigate={navigate} onToast={showToast} />,
    builder: <ProductBuilder onNavigate={navigate} onSaved={(msg) => { navigate('products'); showToast({ tone: 'success', title: 'Ürün kaydedildi', body: msg }) }} />,
    categories: <Categories onToast={showToast} />,
    attributes: <Attributes onToast={showToast} />,
    variants: <Variants onToast={showToast} />,
    settings: <Settings onToast={showToast} />,
  }
  const navRoute = route === 'builder' ? 'products' : route

  return (
    <HelpProvider>
      <AppShell route={navRoute} onNavigate={navigate} onLogout={logout} onToggleTheme={toggleTheme} user={user}>
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
