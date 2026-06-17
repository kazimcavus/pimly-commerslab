import React, { useEffect, useState } from 'react'
import { Button, Dialog, Field, Input, Select } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'

const FIELD_TYPES = [
  { value: 'text', label: 'Metin' },
  { value: 'number', label: 'Sayı' },
  { value: 'color', label: 'Renk (hex)' },
  { value: 'boolean', label: 'Evet / Hayır' },
]

export function Metaobjects({ onToast }) {
  const [defs, setDefs] = useState([])
  const [sel, setSel] = useState(null)
  const [fields, setFields] = useState([])
  const [entries, setEntries] = useState([])
  const [defOpen, setDefOpen] = useState(false)
  const [fieldOpen, setFieldOpen] = useState(false)
  const [entryOpen, setEntryOpen] = useState(false)

  const loadDefs = () => api.listMetaDefs().then((d) => { setDefs(d); if (!sel && d.length) setSel(d[0].id) }).catch(() => {})
  useEffect(() => { loadDefs() }, [])

  const loadFields = (id) => api.listMetaFields(id).then(setFields).catch(() => setFields([]))
  const loadEntries = (id) => api.listMetaEntries(id).then(setEntries).catch(() => setEntries([]))

  useEffect(() => {
    if (!sel) { setFields([]); setEntries([]); return }
    loadFields(sel)
    loadEntries(sel)
  }, [sel])

  const active = defs.find((d) => d.id === sel)

  const parseValues = (raw) => { try { return typeof raw === 'string' ? JSON.parse(raw) : (raw || {}) } catch { return {} } }

  return (
    <div className="page">
      <PageHeader eyebrow="Tanımlar" title="Metaobject'ler" sub="Yapılandırılmış değer kümeleri — Renk, Beden, Materyal."
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={() => setDefOpen(true)}>Tanım ekle</Button>} />
      <div className="split">
        <div className="tree">
          {defs.map((m) => (
            <div key={m.id} className="tree__node" data-active={sel === m.id} onClick={() => setSel(m.id)}>
              {I('boxes')}<span>{m.label}</span>
            </div>
          ))}
          {defs.length === 0 && <div className="list-meta" style={{ padding: 12 }}>Tanım yok.</div>}
        </div>
        {active && (
          <div className="pim-card">
            <div className="pim-card__header">
              <div className="hstack"><span className="pim-card__title">{active.label}</span><span className="typechip">{active.key}</span></div>
              <div className="hstack">
                <Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={() => setFieldOpen(true)}>Alan ekle</Button>
                <Button variant="secondary" size="sm" iconLeft={I('plus')} disabled={fields.length === 0} onClick={() => setEntryOpen(true)}>Kayıt ekle</Button>
              </div>
            </div>
            <div className="pim-card__body">
              {/* Fields — the columns/schema of this definition. */}
              <div className="list-meta" style={{ marginBottom: 12, display: 'flex', flexWrap: 'wrap', gap: 6, alignItems: 'center' }}>
                <span>Alanlar:</span>
                {fields.map((f) => (
                  <span className="typechip" key={f.id} style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                    {f.key} · {f.data_type}
                    <button className="tb__icon" style={{ width: 18, height: 18 }} title="Alanı sil"
                      onClick={async () => {
                        try { await api.deleteMetaField(f.id); loadFields(sel); onToast?.({ tone: 'success', title: 'Alan silindi' }) }
                        catch (e) { onToast?.({ tone: 'danger', title: 'Silinemedi', body: e.message }) }
                      }}>{I('x')}</button>
                  </span>
                ))}
                {fields.length === 0 && <span>—</span>}
              </div>

              {fields.length === 0 ? (
                <div className="subtle" style={{ padding: 14, border: '1px dashed var(--border-default)', borderRadius: 'var(--radius-md)' }}>
                  Bu tanımın henüz alanı yok. Kayıt ekleyebilmek için önce <strong>Alan ekle</strong> ile en az bir alan (örn. <code>ad</code>, <code>hex</code>) tanımlayın.
                </div>
              ) : (
                <div className="pim-table-wrap">
                  <table className="pim-table">
                    <thead><tr>{fields.map((f) => <th key={f.id}>{f.label}</th>)}<th></th></tr></thead>
                    <tbody>
                      {entries.map((e) => {
                        const v = parseValues(e.values)
                        return (
                          <tr key={e.id}>
                            {fields.map((f) => (
                              <td key={f.id} className={f.data_type === 'color' ? 'pim-td-mono' : ''}>
                                <div className="cellrow">
                                  {(f.data_type === 'color' || f.key === 'hex') && v[f.key] && <span className="swatch-sm" style={{ background: v[f.key] }}></span>}
                                  <span className={f.key === 'ad' ? 'pim-td-strong' : ''}>{v[f.key] != null ? String(v[f.key]) : '—'}</span>
                                </div>
                              </td>
                            ))}
                            <td><div className="rowact"><button className="tb__icon" style={{ width: 28, height: 28 }} title="Sil" onClick={async () => { await api.deleteMetaEntry(e.id); setEntries(entries.filter((x) => x.id !== e.id)) }}>{I('trash-2')}</button></div></td>
                          </tr>
                        )
                      })}
                      {entries.length === 0 && <tr><td colSpan={fields.length + 1} className="subtle" style={{ padding: 14 }}>Kayıt yok.</td></tr>}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      <DefDialog open={defOpen} onClose={() => setDefOpen(false)}
        onCreate={async (body) => { try { await api.createMetaDef(body); setDefOpen(false); loadDefs(); onToast?.({ tone: 'success', title: 'Tanım eklendi' }) } catch (e) { onToast?.({ tone: 'danger', title: 'Eklenemedi', body: e.message }) } }} />

      <FieldDialog open={fieldOpen} onClose={() => setFieldOpen(false)}
        onCreate={async (body) => { try { await api.createMetaField(sel, body); setFieldOpen(false); loadFields(sel); onToast?.({ tone: 'success', title: 'Alan eklendi' }) } catch (e) { onToast?.({ tone: 'danger', title: 'Eklenemedi', body: e.message }) } }} />

      <EntryDialog open={entryOpen} fields={fields} onClose={() => setEntryOpen(false)}
        onCreate={async (values) => { try { await api.createMetaEntry(sel, values); setEntryOpen(false); loadEntries(sel); onToast?.({ tone: 'success', title: 'Kayıt eklendi' }) } catch (e) { onToast?.({ tone: 'danger', title: 'Eklenemedi', body: e.message }) } }} />
    </div>
  )
}

function DefDialog({ open, onClose, onCreate }) {
  const [key, setKey] = useState('')
  const [label, setLabel] = useState('')
  useEffect(() => { if (open) { setKey(''); setLabel('') } }, [open])
  return (
    <Dialog open={open} title="Tanım ekle" confirmLabel="Ekle" cancelLabel="İptal" onClose={onClose}
      onConfirm={() => key.trim() && label.trim() && onCreate({ key: key.trim(), label: label.trim() })}>
      <Field label="Key" required><Input mono value={key} onChange={(e) => setKey(e.target.value)} placeholder="materyal" /></Field>
      <Field label="Etiket" required><Input value={label} onChange={(e) => setLabel(e.target.value)} placeholder="Materyal" /></Field>
    </Dialog>
  )
}

function FieldDialog({ open, onClose, onCreate }) {
  const [key, setKey] = useState('')
  const [label, setLabel] = useState('')
  const [dataType, setDataType] = useState('text')
  useEffect(() => { if (open) { setKey(''); setLabel(''); setDataType('text') } }, [open])
  return (
    <Dialog open={open} title="Alan ekle" description="Bu tanımdaki kayıtların bir sütunu." confirmLabel="Ekle" cancelLabel="İptal" onClose={onClose}
      onConfirm={() => key.trim() && label.trim() && onCreate({ key: key.trim(), label: label.trim(), data_type: dataType })}>
      <Field label="Key" required><Input mono value={key} onChange={(e) => setKey(e.target.value)} placeholder="ad" /></Field>
      <Field label="Etiket" required><Input value={label} onChange={(e) => setLabel(e.target.value)} placeholder="Ad" /></Field>
      <Field label="Tip" required>
        <Select value={dataType} onChange={(e) => setDataType(e.target.value)} options={FIELD_TYPES} />
      </Field>
    </Dialog>
  )
}

function EntryDialog({ open, fields, onClose, onCreate }) {
  const [vals, setVals] = useState({})
  useEffect(() => { if (open) setVals({}) }, [open])
  return (
    <Dialog open={open} title="Kayıt ekle" confirmLabel="Ekle" cancelLabel="İptal" onClose={onClose}
      onConfirm={() => onCreate(vals)}>
      {fields.map((f) => (
        <Field key={f.id} label={`${f.label} (${f.data_type})`}>
          <Input value={vals[f.key] || ''} onChange={(e) => setVals((s) => ({ ...s, [f.key]: e.target.value }))} placeholder={f.key} />
        </Field>
      ))}
      {fields.length === 0 && <div className="list-meta">Önce bu tanıma alan eklemelisin.</div>}
    </Dialog>
  )
}
