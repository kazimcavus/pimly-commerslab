import React, { useEffect, useState } from 'react'
import { Button, Badge, Switch, Banner, Input, Dialog, Field } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api, getAdminToken, setAdminToken } from '../lib/api.js'

export function Admin({ onToast }) {
  const [token, setTok] = useState(getAdminToken())
  const [authed, setAuthed] = useState(!!getAdminToken())
  const [apps, setApps] = useState([])
  const [tenants, setTenants] = useState([])
  const [createOpen, setCreateOpen] = useState(false)
  const [approved, setApproved] = useState(null)

  const load = () => {
    api.adminListApplications().then(setApps).catch((e) => { setAuthed(false); onToast?.({ tone: 'danger', title: 'Admin erişimi reddedildi', body: e.message }) })
    api.adminListTenants().then(setTenants).catch(() => {})
  }
  useEffect(() => { if (authed) load() }, [authed])

  const connect = () => { setAdminToken(token.trim()); setAuthed(!!token.trim()); }

  const approve = async (id) => {
    try {
      const res = await api.adminApprove(id)
      setApproved(res)
      onToast?.({ tone: 'success', title: 'Başvuru onaylandı', body: `Tenant: ${res.tenant?.slug}` })
      load()
    } catch (e) { onToast?.({ tone: 'danger', title: 'Onaylanamadı', body: e.message }) }
  }

  const setModule = async (tenantId, mod, enabled) => {
    try { await api.adminSetModule(tenantId, mod, enabled); load() }
    catch (e) { onToast?.({ tone: 'danger', title: 'Modül güncellenemedi', body: e.message }) }
  }

  return (
    <div className="page">
      <PageHeader eyebrow="Platform" title="Admin" sub="Başvuru onayı, tenant yönetimi, modül flag'leri."
        actions={<span className="tb__role" style={{ background: 'var(--surface-sunken)', color: 'var(--text-muted)' }}>X-Admin-Token</span>} />

      <div style={{ marginBottom: 16 }}>
        <Banner tone="info" title="Ayrı yetkilendirme">Bu bölüm kullanıcı JWT'si değil, <span className="mono">X-Admin-Token</span> başlığı ile korunur.</Banner>
      </div>

      <div className="pim-card" style={{ marginBottom: 18 }}>
        <div className="pim-card__body">
          <div className="hstack" style={{ gap: 10, alignItems: 'flex-end' }}>
            <div style={{ flex: 1, maxWidth: 360 }}>
              <Field label="Admin token"><Input mono type="password" value={token} onChange={(e) => setTok(e.target.value)} placeholder="PIMLY_ADMIN_TOKEN" /></Field>
            </div>
            <Button variant="primary" onClick={connect}>{authed ? 'Yenile' : 'Bağlan'}</Button>
          </div>
        </div>
      </div>

      {approved?.generated_password && (
        <div style={{ marginBottom: 16 }}>
          <Banner tone="success" title="Tenant provision edildi">
            <span className="mono">{approved.owner_email}</span> için şifre: <b className="mono">{approved.generated_password}</b> — bir kez gösterilir, şimdi sakla.
          </Banner>
        </div>
      )}

      {authed && (
        <>
          <div className="pim-card" style={{ marginBottom: 18 }}>
            <div className="pim-card__header">
              <span className="pim-card__title">Başvurular</span>
              <Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={() => setCreateOpen(true)}>Başvuru ekle</Button>
            </div>
            <div className="pim-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="pim-table">
                <thead><tr><th>Mağaza</th><th>E-posta</th><th>Durum</th><th></th></tr></thead>
                <tbody>
                  {apps.map((a) => (
                    <tr key={a.id}>
                      <td className="pim-td-strong">{a.company_name}</td>
                      <td className="pim-td-mono">{a.email}</td>
                      <td>{a.status === 'pending' ? <Badge status="draft">Beklemede</Badge> : a.status === 'approved' ? <Badge status="active">Onaylandı</Badge> : <Badge status="archived">{a.status}</Badge>}</td>
                      <td style={{ textAlign: 'right' }}>{a.status === 'pending' && <Button variant="accent" size="sm" iconLeft={I('check')} onClick={() => approve(a.id)}>Onayla</Button>}</td>
                    </tr>
                  ))}
                  {apps.length === 0 && <tr><td colSpan={4} className="subtle" style={{ padding: 14 }}>Başvuru yok.</td></tr>}
                </tbody>
              </table>
            </div>
          </div>

          <div className="pim-card">
            <div className="pim-card__header"><span className="pim-card__title">Tenant'lar & modüller</span></div>
            <div className="pim-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="pim-table">
                <thead><tr><th>Mağaza</th><th>Slug</th><th>Durum</th><th>pim</th><th>integration</th><th>wms</th></tr></thead>
                <tbody>
                  {tenants.map((t) => (
                    <tr key={t.id}>
                      <td><div className="cellrow"><span className="thumb">{I('store')}</span><span className="pim-td-strong">{t.name}</span></div></td>
                      <td className="pim-td-mono">{t.slug}</td>
                      <td><Badge status={t.status === 'active' ? 'active' : 'draft'}>{t.status}</Badge></td>
                      <td><Switch checked disabled /></td>
                      <td><Switch defaultChecked={false} onChange={(e) => setModule(t.id, 'integration', e.target.checked)} /></td>
                      <td><Switch defaultChecked={false} onChange={(e) => setModule(t.id, 'wms', e.target.checked)} /></td>
                    </tr>
                  ))}
                  {tenants.length === 0 && <tr><td colSpan={6} className="subtle" style={{ padding: 14 }}>Tenant yok.</td></tr>}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}

      <CreateAppDialog open={createOpen} onClose={() => setCreateOpen(false)}
        onCreate={async (body) => { try { await api.adminCreateApplication(body); setCreateOpen(false); load(); onToast?.({ tone: 'success', title: 'Başvuru eklendi' }) } catch (e) { onToast?.({ tone: 'danger', title: 'Eklenemedi', body: e.message }) } }} />
    </div>
  )
}

function CreateAppDialog({ open, onClose, onCreate }) {
  const [email, setEmail] = useState('')
  const [company, setCompany] = useState('')
  useEffect(() => { if (open) { setEmail(''); setCompany('') } }, [open])
  return (
    <Dialog open={open} title="Başvuru ekle" confirmLabel="Ekle" cancelLabel="İptal" onClose={onClose}
      onConfirm={() => email.trim() && company.trim() && onCreate({ email: email.trim(), company_name: company.trim() })}>
      <Field label="Mağaza adı" required><Input value={company} onChange={(e) => setCompany(e.target.value)} placeholder="Moda Butik A.Ş." /></Field>
      <Field label="E-posta" required><Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="info@modabutik.com" /></Field>
    </Dialog>
  )
}
