import React, { useEffect, useState } from 'react'
import { Toast } from './ds'
import { api, setToken, getToken } from './lib/api.js'
import { AppShell } from './screens/Shell.jsx'
import { Login } from './screens/Login.jsx'
import { Register } from './screens/Register.jsx'
import { Dashboard } from './screens/Dashboard.jsx'
import { ProductList } from './screens/ProductList.jsx'
import { ProductDetail } from './screens/ProductDetail.jsx'
import { ProductBuilder } from './screens/ProductBuilder.jsx'
import { Categories } from './screens/Categories.jsx'
import { Attributes } from './screens/Attributes.jsx'
import { Variants } from './screens/Variants.jsx'
import { Brands } from './screens/Brands.jsx'
import { PriceDefinitions } from './screens/PriceDefinitions.jsx'
import { Settings } from './screens/Settings.jsx'
import { Channels } from './screens/Channels.jsx'
import { TrendyolOnboarding } from './screens/TrendyolOnboarding.jsx'
import { HelpProvider } from './help/Help.jsx'
import { ConfirmHost, askConfirm } from './lib/confirm.jsx'
import { friendlyError } from './lib/errors.js'
import { hasUnsavedChanges } from './lib/navGuard.js'

export function App() {
  const [session, setSession] = useState(null) // { user: { id, email, name }, tenant: { id, name } }
  // Aktif ekranı localStorage'da tut ki sayfa yenilenince aynı yerde kalınsın.
  const [route, setRoute] = useState(() => localStorage.getItem('pimly_route') || 'dashboard')
  // Ekran parametresi (ör. ürün detayının id'si); yenilemede o da korunur.
  const [routeParam, setRouteParam] = useState(() => localStorage.getItem('pimly_route_param') || null)
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

  // popstate'te "geri dönülecek" mevcut ekranı bilmek için ref'te tut.
  const routeRef = React.useRef({ route, param: routeParam })

  const applyRoute = (r, param) => {
    setRoute(r)
    setRouteParam(param)
    routeRef.current = { route: r, param }
    localStorage.setItem('pimly_route', r)
    if (param) localStorage.setItem('pimly_route_param', param)
    else localStorage.removeItem('pimly_route_param')
    document.querySelector('.app__content')?.scrollTo(0, 0)
  }

  // Kaydedilmemiş değişiklik varsa ekran değiştirmeden önce sor.
  const confirmLeave = () => askConfirm({
    title: 'Kaydedilmemiş değişiklikler var',
    body: 'Bu sayfadan ayrılırsan yaptığın değişiklikler kaybolacak.',
    tone: 'danger', confirmLabel: 'Değişiklikleri sil ve çık', cancelLabel: 'Sayfada kal',
  })

  const navigate = async (r, param = null) => {
    if (hasUnsavedChanges() && !(await confirmLeave())) return
    applyRoute(r, param)
    window.history.pushState({ pimly: true, route: r, param }, '')
  }

  // Tarayıcı geri/ileri desteği: URL değişmez, ekran history state'inde taşınır.
  // İlk kayıt replaceState ile damgalanır; popstate'te ekran geri yüklenir.
  // Kirli formda geri tuşuna basılırsa onay istenir; vazgeçilirse mevcut ekran
  // history'ye geri itilir (pointer zaten hareket etmiştir).
  useEffect(() => {
    window.history.replaceState({ pimly: true, route, param: routeParam }, '')
    const onPop = async (e) => {
      if (!e.state?.pimly) return
      if (hasUnsavedChanges() && !(await confirmLeave())) {
        const cur = routeRef.current
        window.history.pushState({ pimly: true, route: cur.route, param: cur.param }, '')
        return
      }
      applyRoute(e.state.route, e.state.param ?? null)
    }
    window.addEventListener('popstate', onPop)
    // Sekme kapatma/yenileme: burada tarayıcının kendi uyarısı zorunludur
    // (özel modal teknik olarak mümkün değil).
    const onBeforeUnload = (e) => {
      if (hasUnsavedChanges()) { e.preventDefault(); e.returnValue = '' }
    }
    window.addEventListener('beforeunload', onBeforeUnload)
    return () => {
      window.removeEventListener('popstate', onPop)
      window.removeEventListener('beforeunload', onBeforeUnload)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // Bildirimler: hata nesnesi `error` alanıyla geçilir, gövde kullanıcı dostu
  // metne burada çevrilir (ham e.message asla gösterilmez — bkz. docs/ui-feedback.md).
  const showToast = (t) => {
    const body = t.error ? friendlyError(t.error) : t.body
    setToast({ ...t, body })
    clearTimeout(window.__pt)
    window.__pt = setTimeout(() => setToast(null), t.tone === 'danger' ? 6500 : 3800)
  }

  const signIn = async (email, password) => {
    setLoading(true); setAuthError('')
    try {
      const r = await api.login(email, password)
      setToken(r.token)
      setSession({ user: r.user, tenant: r.tenant || null })
      navigate('dashboard')
    } catch (e) {
      setAuthError(e.status === 401 ? 'E-posta ya da şifre hatalı.' : friendlyError(e))
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
      navigate('onboarding')
    } catch (e) {
      setAuthError(friendlyError(e))
    } finally {
      setLoading(false)
    }
  }

  const logout = () => { setToken(''); setSession(null); navigate('dashboard'); setAuthView('login') }

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
    products: <ProductList onNavigate={navigate} onToast={showToast} initialFilter={routeParam} />,
    product: <ProductDetail productId={routeParam} onNavigate={navigate} onToast={showToast} />,
    builder: <ProductBuilder onNavigate={navigate} onToast={showToast} onSaved={(msg) => { navigate('products'); showToast({ tone: 'success', title: 'Ürün kaydedildi', body: msg }) }} />,
    categories: <Categories onToast={showToast} />,
    attributes: <Attributes onToast={showToast} />,
    variants: <Variants onToast={showToast} />,
    brands: <Brands onToast={showToast} />,
    prices: <PriceDefinitions onToast={showToast} />,
    settings: <Settings onToast={showToast} />,
    channels: <Channels onNavigate={navigate} onToast={showToast} />,
    onboarding: <TrendyolOnboarding onNavigate={navigate} onToast={showToast} />,
  }
  const navRoute = route === 'builder' || route === 'product' ? 'products' : route === 'onboarding' ? 'channels' : route

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
      <ConfirmHost />
    </HelpProvider>
  )
}
