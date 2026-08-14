import React, { useEffect, useRef, useState } from 'react'
import { Button, Banner } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'
import { friendlyError } from '../lib/errors.js'

const MP = 'TY'

const STATUS_TR = {
  pending: 'Sırada',
  running: 'Çalışıyor',
  completed: 'Tamamlandı',
  completed_with_errors: 'Hatalarla tamamlandı',
  failed: 'Başarısız',
  cancelled: 'İptal edildi',
}

// Backend teknik hata mesajlarını kullanıcı diline çevir. Bilinmeyen mesajlar
// ham haliyle gösterilir; teknik metin ayrıca "Teknik" satırında saklanır.
const ERROR_MAP = [
  { test: /variant value key must be unique/i, friendly: 'Bu üründe aynı varyant türü içinde çakışan (yinelenen) bir varyant değeri var; ürün içe aktarılamadı.' },
  { test: /category .*not.*(found|mapped)|kategori.*eşle/i, friendly: 'Ürünün Trendyol kategorisi Pimly kategorisine eşlenemedi.' },
  { test: /barcode.*(exist|duplicate)|barkod.*(var|çak)/i, friendly: 'Bu barkod kataloğunda zaten kullanılıyor.' },
]
function friendlyImportError(message) {
  if (!message) return 'Bilinmeyen bir hata oluştu.'
  for (const e of ERROR_MAP) if (e.test.test(message)) return e.friendly
  return message
}

// Pazaryerleri ekranı: bağlantı durumu + import geçmişi + yeniden import.
export function Channels({ onNavigate, onToast }) {
  const [marketplaces, setMarketplaces] = useState([])
  const [connection, setConnection] = useState(null)
  const [runs, setRuns] = useState([])
  const [loading, setLoading] = useState(true)
  const [expandedId, setExpandedId] = useState(null)      // hataları açık olan run
  const [details, setDetails] = useState({})              // runId -> { loading, data, error }
  const pollRef = useRef(null)

  const refresh = async () => {
    setLoading(true)
    try {
      const [mps, conn, history] = await Promise.all([
        api.listMarketplaces().catch(() => []),
        api.getConnection(MP).catch(() => null),
        api.listImportRuns(MP).catch(() => []),
      ])
      setMarketplaces(mps)
      setConnection(conn)
      setRuns(Array.isArray(history) ? history : [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { refresh() }, [])

  const connected = !!connection?.has_api_key
  const hasActiveRun = runs.some((r) => r.status === 'pending' || r.status === 'running')
  const expandedRun = runs.find((r) => r.id === expandedId) || null

  // Import sürerken geçmişi canlı güncelle: aktif run varken 2 sn'de bir import
  // geçmişini tazele (loading spinner'ı tetiklemeden), run bitince durdur.
  useEffect(() => {
    if (!hasActiveRun) return
    pollRef.current = setInterval(async () => {
      try {
        const history = await api.listImportRuns(MP)
        setRuns(Array.isArray(history) ? history : [])
      } catch { /* geçici hata — poll devam */ }
    }, 2000)
    return () => clearInterval(pollRef.current)
  }, [hasActiveRun])

  // Bir run'ın hataları açıldığında (ya da açıkken hata sayısı/durumu değiştiğinde)
  // ayrıntı DTO'sunu çek — özet listesi hata kayıtlarını taşımaz, detay ucu taşır.
  useEffect(() => {
    if (!expandedId) return
    let cancelled = false
    setDetails((d) => ({ ...d, [expandedId]: { ...(d[expandedId] || {}), loading: !d[expandedId]?.data } }))
    api.getImportRun(MP, expandedId)
      .then((full) => { if (!cancelled) setDetails((d) => ({ ...d, [expandedId]: { loading: false, data: full } })) })
      .catch((e) => { if (!cancelled) setDetails((d) => ({ ...d, [expandedId]: { loading: false, error: friendlyError(e) } })) })
    return () => { cancelled = true }
  }, [expandedId, expandedRun?.failed_products, expandedRun?.status])

  const toggleErrors = (runId) => setExpandedId((cur) => (cur === runId ? null : runId))

  return (
    <div className="page" style={{ maxWidth: 900 }}>
      <PageHeader
        eyebrow="Platform"
        title="Pazaryerleri"
        help="channels"
        sub="Mağazalarını bağla, ürünlerini içeri aktar; gönderim (v2) için eşlemeler import sırasında otomatik kurulur."
      />

      {/* Trendyol kartı */}
      <div className="bnode">
        <div className="bnode__head">
          <span className="ic">{I('store')}</span>
          <div>
            <div className="bnode__title">Trendyol</div>
            <div className="list-meta">
              {connected
                ? <>Bağlı · Satıcı ID <span className="mono">{connection.seller_id || '—'}</span> · API anahtarı <span className="mono">••••{connection.api_key_hint || ''}</span></>
                : 'Henüz bağlı değil'}
            </div>
          </div>
          <div className="hstack" style={{ marginLeft: 'auto', gap: 8 }}>
            {connected && (
              <Button variant="ghost" iconLeft={I('pencil')} onClick={() => onNavigate('onboarding')}>Düzenle</Button>
            )}
            <Button variant={connected ? 'secondary' : 'primary'} iconLeft={I(connected ? 'refresh-cw' : 'plug')}
              disabled={hasActiveRun}
              onClick={() => onNavigate('onboarding')}>
              {connected ? (hasActiveRun ? 'Import sürüyor…' : 'Yeniden içe aktar') : 'Bağla ve içe aktar'}
            </Button>
          </div>
        </div>
        <div className="bnode__body">
          {!connected && (
            <Banner tone="info" title="Ürünlerini Trendyol'dan çek">
              Mağazanı bağladığında kategorilerin, özelliklerin ve varyantların otomatik tanımlanır;
              ürünlerin fiyat, stok ve barkodlarıyla birlikte içeri aktarılır.
            </Banner>
          )}

          {runs.length > 0 && (
            <div style={{ marginTop: connected ? 0 : 14 }}>
              <div className="list-meta" style={{ marginBottom: 8, fontWeight: 600 }}>İçe aktarma geçmişi</div>
              <div style={{ border: '1px solid var(--border-subtle)', borderRadius: 'var(--radius-md)', overflow: 'hidden' }}>
                {runs.map((r) => {
                  const hasErrors = (r.failed_products || 0) > 0
                  const open = expandedId === r.id
                  const det = details[r.id]
                  return (
                    <div key={r.id} style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                      <div className="between" style={{ padding: '10px 14px' }}>
                        <div>
                          <div style={{ fontWeight: 600, color: 'var(--text-strong)', fontSize: 14 }}>
                            {STATUS_TR[r.status] || r.status}
                            <span className="list-meta" style={{ marginLeft: 8 }}>{new Date(r.created_at).toLocaleString('tr-TR')}</span>
                          </div>
                          <div className="list-meta">
                            {r.total_products != null
                              ? <>
                                  {r.imported_products} aktarıldı · {r.skipped_products} atlandı ·{' '}
                                  {hasErrors
                                    ? <button type="button" onClick={() => toggleErrors(r.id)}
                                        style={{ background: 'none', border: 0, padding: 0, cursor: 'pointer', font: 'inherit',
                                          color: 'var(--danger-fg)', fontWeight: 600, textDecoration: 'underline',
                                          display: 'inline-flex', alignItems: 'center', gap: 2 }}>
                                        {r.failed_products} hatalı {I(open ? 'chevron-up' : 'chevron-down', { size: 13 })}
                                      </button>
                                    : <>{r.failed_products} hatalı</>}
                                  {' '}({r.processed_products}/{r.total_products})
                                </>
                              : 'Henüz başlamadı'}
                          </div>
                        </div>
                        <StatusBadge status={r.status === 'completed' ? 'active' : r.status === 'failed' ? 'archived' : 'draft'} />
                      </div>
                      {open && (
                        <div style={{ padding: '0 14px 12px' }}>
                          {det?.loading && !det?.data && <div className="list-meta">Hata ayrıntıları yükleniyor…</div>}
                          {det?.error && <div className="list-meta">Ayrıntılar alınamadı: {det.error}</div>}
                          {det?.data && (
                            <div style={{ border: '1px solid var(--border-subtle)', borderRadius: 'var(--radius-md)', overflow: 'hidden', background: 'var(--surface-subtle)' }}>
                              {(det.data.errors || []).map((er, i) => {
                                const friendly = friendlyImportError(er.message)
                                const showTech = friendly !== er.message
                                return (
                                  <div key={i} style={{ display: 'flex', gap: 10, alignItems: 'flex-start', padding: '9px 12px', borderBottom: '1px solid var(--border-subtle)' }}>
                                    <span style={{ color: 'var(--danger-fg)', marginTop: 1, flexShrink: 0 }}>{I('alert-triangle', { size: 14 })}</span>
                                    <div>
                                      <div style={{ fontSize: 13, color: 'var(--text-strong)' }}>{friendly}</div>
                                      <div className="list-meta">
                                        {er.barcode ? <>Barkod <span className="mono">{er.barcode}</span> · </> : null}
                                        Ürün kodu <span className="mono">{er.product_main_id}</span>
                                        {showTech ? <> · <span style={{ opacity: 0.7 }}>Teknik: {er.message}</span></> : null}
                                      </div>
                                    </div>
                                  </div>
                                )
                              })}
                              {(det.data.errors || []).length === 0 && (
                                <div className="list-meta" style={{ padding: '9px 12px' }}>Hata kaydı bulunamadı.</div>
                              )}
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            </div>
          )}

          {!loading && runs.length === 0 && connected && (
            <div className="list-meta">Henüz içe aktarma yapılmadı.</div>
          )}
        </div>
      </div>

      {/* Diğer pazaryerleri */}
      <div className="bnode" style={{ marginTop: 14 }}>
        <div className="bnode__head">
          <span className="ic">{I('layers')}</span>
          <div><div className="bnode__title">Sırada ne var?</div>
            <div className="list-meta">Hepsiburada ve diğer pazaryerleri yakında; ürün gönderimi (v2) eşlemeler üzerinden çalışacak.</div></div>
        </div>
      </div>
    </div>
  )
}
