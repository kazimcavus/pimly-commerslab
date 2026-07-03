import React, { useEffect, useRef, useState } from 'react'
import { Button, Field, Input, Banner } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'

const MP = 'TY' // Trendyol pazaryeri kodu

const STEPS = [
  { id: 'connect', label: 'Bağlan' },
  { id: 'sync', label: 'Kategoriler' },
  { id: 'import', label: 'Ürünler' },
  { id: 'done', label: 'Özet' },
]

// Yeni müşteri onboarding'i: Trendyol'u bağla → kategori ağacını eşitle →
// ürünleri içe aktar → özet. Aynı ekran Pazaryerleri'nden yeniden import için de kullanılır.
export function TrendyolOnboarding({ onNavigate, onToast }) {
  const [step, setStep] = useState('connect')
  const [error, setError] = useState('')

  // Adım 1 — bağlantı formu
  const [sellerId, setSellerId] = useState('')
  const [apiKey, setApiKey] = useState('')
  const [apiSecret, setApiSecret] = useState('')
  const [savingConn, setSavingConn] = useState(false)
  const [existingConnection, setExistingConnection] = useState(null) // {seller_id, api_key_hint}
  const [editing, setEditing] = useState(false)                       // bağlıyken formu açar

  // Adım 2 — taksonomi durumu
  const [taxStatus, setTaxStatus] = useState(null)

  // Adım 3/4 — import run
  const [run, setRun] = useState(null)
  const pollRef = useRef(null)

  // Mevcut bağlantı varsa formu ön-doldur (secret asla geri dönmez, sadece ipucu).
  useEffect(() => {
    api.getConnection(MP).then((c) => {
      if (c?.has_api_key) {
        setExistingConnection(c)
        setSellerId(c.seller_id || '')
      }
    }).catch(() => {})
    return () => clearInterval(pollRef.current)
  }, [])

  const fail = (e, fallback) => setError(e?.message || fallback)

  // --- Adım 1: bağlantıyı kaydet ---
  const saveConnection = async (e) => {
    e.preventDefault()
    setError('')
    if (!sellerId.trim() || !apiKey.trim() || !apiSecret.trim()) {
      setError('Satıcı ID, API Key ve API Secret alanlarının üçü de gerekli.')
      return
    }
    setSavingConn(true)
    try {
      await api.putConnection(MP, {
        seller_id: sellerId.trim(),
        api_key: apiKey.trim(),
        api_secret: apiSecret.trim(),
        is_enabled: true,
      })
      startSync()
    } catch (e2) {
      fail(e2, 'Bağlantı kaydedilemedi')
    } finally {
      setSavingConn(false)
    }
  }

  // --- Adım 2: taksonomi senkronizasyonu ---
  const startSync = async () => {
    setStep('sync'); setError('')
    try {
      const status = await api.getTaxonomyStatus(MP)
      setTaxStatus(status)
      // Kategoriler zaten eşitlenmişse doğrudan import'a geç.
      if ((status?.cached_category_count || 0) > 0 && !status?.is_sync_active) {
        startImport()
        return
      }
      if (!status?.is_sync_active) {
        await api.enqueueTaxonomySync(MP).catch((e) => {
          if (e.status !== 409) throw e // zaten kuyruktaysa sorun değil
        })
      }
      pollRef.current = setInterval(async () => {
        try {
          const s = await api.getTaxonomyStatus(MP)
          setTaxStatus(s)
          if ((s?.cached_category_count || 0) > 0 && !s?.is_sync_active) {
            clearInterval(pollRef.current)
            startImport()
          }
        } catch { /* geçici hata — poll devam */ }
      }, 2000)
    } catch (e) {
      fail(e, 'Kategori senkronizasyonu başlatılamadı')
    }
  }

  // --- Adım 3: ürün import'u ---
  const startImport = async () => {
    setStep('import'); setError('')
    clearInterval(pollRef.current)
    try {
      const started = await api.startImport(MP).catch(async (e) => {
        // Zaten süren bir import varsa (409) onu izlemeye devam et.
        if (e.status === 409) {
          const runs = await api.listImportRuns(MP, 1)
          return runs?.[0] || null
        }
        throw e
      })
      if (!started) { setError('Import başlatılamadı.'); return }
      setRun(started)
      pollRef.current = setInterval(async () => {
        try {
          const r = await api.getImportRun(MP, started.id)
          setRun(r)
          if (['completed', 'completed_with_errors', 'failed'].includes(r.status)) {
            clearInterval(pollRef.current)
            setStep('done')
            onToast?.({
              tone: r.status === 'failed' ? 'danger' : 'success',
              title: r.status === 'failed' ? 'Import başarısız' : 'Import tamamlandı',
              body: r.status === 'failed' ? r.error_message : `${r.imported_products} ürün içe aktarıldı.`,
            })
          }
        } catch { /* poll devam */ }
      }, 2000)
    } catch (e) {
      fail(e, 'Import başlatılamadı')
    }
  }

  const stepIndex = STEPS.findIndex((s) => s.id === step)
  const pct = run?.total_products ? Math.min(100, Math.round((run.processed_products / run.total_products) * 100)) : 0

  return (
    <div className="page" style={{ maxWidth: 760 }}>
      <PageHeader
        crumbs={[{ label: 'Pazaryerleri', onClick: () => onNavigate('channels') }, { label: 'Trendyol Kurulumu' }]}
        eyebrow="Onboarding"
        title="Trendyol'dan ürünlerini çek"
        help="trendyol-import"
        sub="Mağazanı bağla; kategorilerin, özelliklerin ve varyantların otomatik tanımlansın, ürünlerin içeri aktarılsın."
      />

      {/* Adım göstergesi */}
      <div className="hstack" style={{ gap: 8, marginBottom: 20, flexWrap: 'wrap' }}>
        {STEPS.map((s, i) => (
          <span key={s.id} className="pim-badge" data-active={i === stepIndex}
            style={{
              display: 'inline-flex', alignItems: 'center', gap: 6,
              background: i < stepIndex ? 'var(--success-bg, #e8f5e9)' : i === stepIndex ? 'var(--surface)' : 'var(--surface-subtle)',
              border: i === stepIndex ? '1px solid var(--border-strong, var(--border-default))' : '1px solid var(--border-subtle)',
              fontWeight: i === stepIndex ? 700 : 500,
            }}>
            {i < stepIndex ? I('check', { size: 13 }) : <span style={{ fontWeight: 700 }}>{i + 1}</span>} {s.label}
          </span>
        ))}
      </div>

      {error && <div style={{ marginBottom: 16 }}><Banner tone="danger" title="Bir sorun çıktı">{error}</Banner></div>}

      {step === 'connect' && existingConnection && !editing && (
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('badge-check')}</span>
            <div><div className="bnode__title">Trendyol bağlı</div>
              <div className="list-meta">Kayıtlı bilgilerle devam edebilir ya da bilgileri değiştirebilirsin.</div></div>
          </div>
          <div className="bnode__body">
            <div className="between" style={{ padding: '10px 14px', border: '1px solid var(--border-subtle)', borderRadius: 'var(--radius-md)', flexWrap: 'wrap', gap: 8 }}>
              <div className="list-meta">
                Satıcı ID <span className="mono pim-td-strong">{existingConnection.seller_id || '—'}</span>
                {' · '}API anahtarı <span className="mono">••••{existingConnection.api_key_hint || ''}</span>
              </div>
              <Button variant="ghost" size="sm" iconLeft={I('pencil')} onClick={() => { setEditing(true); setApiKey(''); setApiSecret('') }}>Bilgileri değiştir</Button>
            </div>
            <div className="hstack" style={{ marginTop: 16, justifyContent: 'flex-end', gap: 8 }}>
              <Button variant="secondary" onClick={() => onNavigate('channels')}>İptal</Button>
              <Button variant="primary" iconLeft={I('arrow-right')} onClick={startSync}>Mevcut bağlantıyla devam et</Button>
            </div>
          </div>
        </div>
      )}

      {step === 'connect' && (!existingConnection || editing) && (
        <form className="bnode" onSubmit={saveConnection}>
          <div className="bnode__head">
            <span className="ic">{I('plug')}</span>
            <div><div className="bnode__title">{editing ? 'Trendyol bilgilerini değiştir' : 'Trendyol mağazanı bağla'}</div>
              <div className="list-meta">API bilgilerin yalnızca senin hesabında saklanır.</div></div>
          </div>
          <div className="bnode__body">
            <Banner tone="info" title="API bilgilerini nereden bulurum?">
              Trendyol Satıcı Paneli → Hesap Bilgileri → <strong>Entegrasyon Bilgileri</strong> sayfasında
              Satıcı ID (Cari ID), API Key ve API Secret bilgilerini görebilirsin.
            </Banner>
            <div className="fieldgrid" style={{ marginTop: 14 }}>
              <Field label="Satıcı ID (Cari ID)" required>
                <Input mono value={sellerId} onChange={(e) => setSellerId(e.target.value)} placeholder="Ör. 123456" />
              </Field>
              <Field label="API Key" required>
                <Input mono value={apiKey} onChange={(e) => setApiKey(e.target.value)} />
              </Field>
              <Field label="API Secret" required>
                <Input mono type="password" value={apiSecret} onChange={(e) => setApiSecret(e.target.value)} />
              </Field>
            </div>
            <div className="hstack" style={{ marginTop: 16, justifyContent: 'flex-end', gap: 8 }}>
              {editing
                ? <Button variant="secondary" onClick={() => setEditing(false)}>Vazgeç</Button>
                : <Button variant="secondary" onClick={() => onNavigate('dashboard')}>Sonra yaparım</Button>}
              <Button variant="primary" type="submit" loading={savingConn} iconLeft={I('plug')}>
                {editing ? 'Kaydet ve devam et' : 'Bağlan ve devam et'}
              </Button>
            </div>
          </div>
        </form>
      )}

      {step === 'sync' && (
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('folder-tree')}</span>
            <div><div className="bnode__title">Kategoriler eşitleniyor…</div>
              <div className="list-meta">Trendyol kategori ağacı indiriliyor; bu birkaç dakika sürebilir.</div></div>
          </div>
          <div className="bnode__body">
            <div className="list-meta" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <span className="spinner" /> Şu ana kadar <strong style={{ color: 'var(--text-strong)' }}>
                {taxStatus?.cached_category_count ?? 0}</strong> kategori alındı.
            </div>
          </div>
        </div>
      )}

      {step === 'import' && (
        <div className="bnode">
          <div className="bnode__head">
            <span className="ic">{I('download')}</span>
            <div><div className="bnode__title">Ürünlerin içeri aktarılıyor…</div>
              <div className="list-meta">Kategoriler, özellikler ve varyantlar otomatik tanımlanıyor.</div></div>
          </div>
          <div className="bnode__body">
            <div style={{ height: 8, background: 'var(--surface-subtle)', borderRadius: 4, overflow: 'hidden', marginBottom: 10 }}>
              <div style={{ height: '100%', width: `${pct}%`, background: 'var(--accent, #4f7d5a)', transition: 'width .5s' }} />
            </div>
            <div className="list-meta">
              {run?.total_products
                ? <>İşlenen: <strong style={{ color: 'var(--text-strong)' }}>{run.processed_products}/{run.total_products}</strong> ürün
                  · {run.imported_products} aktarıldı · {run.skipped_products} atlandı · {run.failed_products} hatalı</>
                : 'Ürün listesi alınıyor…'}
            </div>
          </div>
        </div>
      )}

      {step === 'done' && run && (
        <div className="stack" style={{ gap: 14 }}>
          <div className="bnode">
            <div className="bnode__head">
              <span className="ic">{I(run.status === 'failed' ? 'x-circle' : 'check-circle')}</span>
              <div><div className="bnode__title">
                {run.status === 'failed' ? 'Import başarısız oldu' : 'Import tamamlandı'}</div>
                <div className="list-meta">{run.status === 'failed' ? (run.error_message || '') : 'Ürünlerin artık Pimly kataloğunda — buradan zenginleştirebilirsin.'}</div></div>
            </div>
            <div className="bnode__body">
              <div className="hstack" style={{ gap: 24, flexWrap: 'wrap' }}>
                <Stat label="İçe aktarılan" value={run.imported_products} />
                <Stat label="Atlanan (zaten vardı)" value={run.skipped_products} />
                <Stat label="Hatalı" value={run.failed_products} tone={run.failed_products ? 'danger' : undefined} />
              </div>
              {(run.errors || []).length > 0 && (
                <div style={{ marginTop: 14, border: '1px solid var(--border-subtle)', borderRadius: 'var(--radius-md)', padding: '8px 12px', maxHeight: 220, overflowY: 'auto' }}>
                  {run.errors.map((er, i) => (
                    <div key={i} className="list-meta" style={{ padding: '4px 0', borderBottom: '1px solid var(--border-subtle)' }}>
                      <span className="mono">{er.product_main_id}</span>{er.barcode ? <span className="mono"> · {er.barcode}</span> : null} — {er.message}
                    </div>
                  ))}
                </div>
              )}
              <div className="hstack" style={{ marginTop: 16, justifyContent: 'flex-end', gap: 8 }}>
                <Button variant="secondary" onClick={() => onNavigate('channels')}>Pazaryerleri</Button>
                <Button variant="primary" iconLeft={I('package')} onClick={() => onNavigate('products')}>Ürünlerime git</Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function Stat({ label, value, tone }) {
  return (
    <div>
      <div style={{ fontSize: 26, fontWeight: 800, color: tone === 'danger' && value ? 'var(--danger-fg)' : 'var(--text-strong)' }}>{value ?? 0}</div>
      <div className="list-meta">{label}</div>
    </div>
  )
}
