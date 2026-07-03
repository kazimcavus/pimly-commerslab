import React, { useEffect, useState } from 'react'
import { Button, Banner } from '../ds'
import { I } from './icons.jsx'
import { PageHeader, StatusBadge } from './PageHeader.jsx'
import { api } from '../lib/api.js'

const MP = 'TY'

const STATUS_TR = {
  pending: 'Sırada',
  running: 'Çalışıyor',
  completed: 'Tamamlandı',
  completed_with_errors: 'Hatalarla tamamlandı',
  failed: 'Başarısız',
  cancelled: 'İptal edildi',
}

// Pazaryerleri ekranı: bağlantı durumu + import geçmişi + yeniden import.
export function Channels({ onNavigate, onToast }) {
  const [marketplaces, setMarketplaces] = useState([])
  const [connection, setConnection] = useState(null)
  const [runs, setRuns] = useState([])
  const [loading, setLoading] = useState(true)

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
          <div style={{ marginLeft: 'auto' }} className="hstack">
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
                {runs.map((r) => (
                  <div key={r.id} className="between" style={{ padding: '10px 14px', borderBottom: '1px solid var(--border-subtle)' }}>
                    <div>
                      <div style={{ fontWeight: 600, color: 'var(--text-strong)', fontSize: 14 }}>
                        {STATUS_TR[r.status] || r.status}
                        <span className="list-meta" style={{ marginLeft: 8 }}>{new Date(r.created_at).toLocaleString('tr-TR')}</span>
                      </div>
                      <div className="list-meta">
                        {r.total_products != null
                          ? <>{r.imported_products} aktarıldı · {r.skipped_products} atlandı · {r.failed_products} hatalı ({r.processed_products}/{r.total_products})</>
                          : 'Henüz başlamadı'}
                      </div>
                    </div>
                    <StatusBadge status={r.status === 'completed' ? 'active' : r.status === 'failed' ? 'archived' : 'draft'} />
                  </div>
                ))}
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
