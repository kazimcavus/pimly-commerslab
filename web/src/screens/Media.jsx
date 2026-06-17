import React, { useEffect, useRef, useState } from 'react'
import { Button, Banner, Dialog, Field, Select } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'

export function Media({ onToast }) {
  const bulkInput = useRef(null)
  const [result, setResult] = useState(null)
  const [busy, setBusy] = useState(false)
  const [singleOpen, setSingleOpen] = useState(false)

  const doBulk = async (files) => {
    if (!files || !files.length) return
    setBusy(true)
    try {
      const res = await api.bulkUploadMedia(files)
      setResult(res)
      onToast?.({ tone: 'success', title: 'Toplu yükleme tamamlandı', body: `${res.attached?.length || 0} eşleşti, ${res.skipped?.length || 0} atlandı.` })
    } catch (e) {
      onToast?.({ tone: 'danger', title: 'Yükleme başarısız', body: e.message })
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page">
      <PageHeader eyebrow="Katalog" title="Medya" sub="Görseller ürün seviyesinde; varyantlar miras alır."
        actions={<>
          <Button variant="secondary" iconLeft={I('upload')} onClick={() => setSingleOpen(true)}>Tekil yükle</Button>
          <Button variant="accent" iconLeft={I('upload-cloud')} loading={busy} onClick={() => bulkInput.current?.click()}>Toplu yükle</Button>
        </>} />
      <input ref={bulkInput} type="file" multiple accept="image/*" style={{ display: 'none' }} onChange={(e) => doBulk(Array.from(e.target.files || []))} />

      <div style={{ marginBottom: 16 }}>
        <div className="dropzone"
          onDragOver={(e) => e.preventDefault()}
          onDrop={(e) => { e.preventDefault(); doBulk(Array.from(e.dataTransfer.files || [])) }}
          onClick={() => bulkInput.current?.click()}
          style={{ cursor: 'pointer' }}>
          {I('upload-cloud')}
          <div style={{ fontWeight: 600, color: 'var(--text-strong)', marginTop: 8 }}>Dosyaları buraya sürükle</div>
          <div className="list-meta" style={{ marginTop: 4 }}>Toplu yüklemede dosya adı = <span className="mono">product_sku</span> ile otomatik eşleşir. Eşleşmeyenler atlanır.</div>
        </div>
      </div>

      {result && (
        <div style={{ marginBottom: 16 }}>
          <Banner tone={result.skipped?.length ? 'warning' : 'success'} title="Toplu yükleme sonucu">
            {(result.attached?.length || 0)} görsel eşleşti{result.skipped?.length ? `, ${result.skipped.length} dosya atlandı (SKU bulunamadı)` : ''}.
          </Banner>
        </div>
      )}

      {result?.attached?.length > 0 && (
        <div className="media-grid">
          {result.attached.map((a, i) => (
            <div className="media-tile" key={i}>
              <div className="media-tile__img">{I('image')}</div>
              <div className="media-tile__meta"><div className="mono" style={{ color: 'var(--text-strong)' }}>{a.product_sku}</div><div className="subtle">{a.filename}</div></div>
            </div>
          ))}
        </div>
      )}
      {result?.skipped?.length > 0 && (
        <div className="list-meta" style={{ marginTop: 12 }}>Atlananlar: {result.skipped.map((s) => s.filename).join(', ')}</div>
      )}

      <SingleUploadDialog open={singleOpen} onClose={() => setSingleOpen(false)} onToast={onToast} />
    </div>
  )
}

function SingleUploadDialog({ open, onClose, onToast }) {
  const [groups, setGroups] = useState([])
  const [groupId, setGroupId] = useState('')
  const [products, setProducts] = useState([])
  const [productId, setProductId] = useState('')
  const [file, setFile] = useState(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => { if (open) { api.listGroups().then(setGroups).catch(() => {}); setGroupId(''); setProducts([]); setProductId(''); setFile(null) } }, [open])
  useEffect(() => { if (groupId) api.getGroup(groupId).then((g) => setProducts(g.products || [])).catch(() => setProducts([])) }, [groupId])

  const submit = async () => {
    if (!productId || !file) return
    setBusy(true)
    try { await api.uploadMedia(productId, file); onToast?.({ tone: 'success', title: 'Görsel yüklendi' }); onClose() }
    catch (e) { onToast?.({ tone: 'danger', title: 'Yüklenemedi', body: e.message }) }
    finally { setBusy(false) }
  }

  return (
    <Dialog open={open} title="Tekil görsel yükle" confirmLabel="Yükle" cancelLabel="İptal" onClose={onClose} onConfirm={submit} busy={busy}>
      <Field label="Grup" required><Select value={groupId} placeholder="Seç…" onChange={(e) => setGroupId(e.target.value)} options={groups.map((g) => ({ value: g.id, label: `${g.title || g.group_code}` }))} /></Field>
      <Field label="Ürün" required><Select value={productId} placeholder={groupId ? 'Seç…' : 'Önce grup seç'} onChange={(e) => setProductId(e.target.value)} options={products.map((p) => ({ value: p.id, label: p.product_sku }))} /></Field>
      <Field label="Dosya" required><input type="file" accept="image/*" onChange={(e) => setFile(e.target.files?.[0] || null)} /></Field>
    </Dialog>
  )
}
