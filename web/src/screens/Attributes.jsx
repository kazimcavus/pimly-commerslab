import React, { useEffect, useState } from 'react'
import { Button, Dialog, Field, Input, Select, Checkbox } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'

const LVL = { group: 'group', product: 'product', variant: 'variant' }
const DATA_TYPES = ['text', 'number', 'bool', 'date', 'money', 'dimension', 'color', 'single_select', 'multi_select', 'metaobject_ref', 'metaobject_list']
const VALUE_SOURCES = ['none', 'inline', 'metaobject']

export function Attributes({ onToast }) {
  const [rows, setRows] = useState([])
  const [open, setOpen] = useState(false)
  const load = () => api.listAttributes().then(setRows).catch(() => {})
  useEffect(() => { load() }, [])

  return (
    <div className="page">
      <PageHeader eyebrow="Tanımlar" title="Özellikler" sub="Attribute tanımları — data_type ve value_source koşullu."
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={() => setOpen(true)}>Özellik ekle</Button>} />
      <div className="pim-table-wrap">
        <table className="pim-table">
          <thead><tr><th>Etiket</th><th>Key</th><th>data_type</th><th>value_source</th><th>binding_level</th><th>Global</th><th></th></tr></thead>
          <tbody>
            {rows.map((a) => (
              <tr key={a.id}>
                <td className="pim-td-strong">{a.label}</td>
                <td className="pim-td-mono">{a.key}</td>
                <td><span className="typechip">{a.data_type}</span></td>
                <td className="muted">{a.value_source}</td>
                <td><span className={`lvlchip lvl-${a.binding_level}`}>{LVL[a.binding_level] || a.binding_level}</span></td>
                <td>{a.is_global ? I('check') : <span className="subtle">—</span>}</td>
                <td><div className="rowact"><button className="tb__icon" style={{ width: 28, height: 28 }} title="Sil" onClick={async () => { if (!confirm('Silinsin mi?')) return; await api.deleteAttribute(a.id); load() }}>{I('trash-2')}</button></div></td>
              </tr>
            ))}
            {rows.length === 0 && <tr><td colSpan={7} className="subtle" style={{ padding: 16 }}>Henüz özellik yok.</td></tr>}
          </tbody>
        </table>
      </div>
      <CreateAttrDialog open={open} onClose={() => setOpen(false)}
        onCreate={async (body) => { try { await api.createAttribute(body); setOpen(false); load(); onToast?.({ tone: 'success', title: 'Özellik eklendi' }) } catch (e) { onToast?.({ tone: 'danger', title: 'Eklenemedi', body: e.message }) } }} />
    </div>
  )
}

function CreateAttrDialog({ open, onClose, onCreate }) {
  const [f, setF] = useState({ key: '', label: '', data_type: 'text', value_source: 'none', binding_level: 'product', is_global: false })
  useEffect(() => { if (open) setF({ key: '', label: '', data_type: 'text', value_source: 'none', binding_level: 'product', is_global: false }) }, [open])
  const set = (k, v) => setF((s) => ({ ...s, [k]: v }))
  return (
    <Dialog open={open} title="Özellik ekle" confirmLabel="Ekle" cancelLabel="İptal" onClose={onClose}
      onConfirm={() => f.key.trim() && f.label.trim() && onCreate(f)}>
      <div className="fieldgrid">
        <Field label="Etiket" required><Input value={f.label} onChange={(e) => set('label', e.target.value)} placeholder="Kumaş" /></Field>
        <Field label="Key" required><Input mono value={f.key} onChange={(e) => set('key', e.target.value)} placeholder="kumas" /></Field>
        <Field label="data_type"><Select value={f.data_type} onChange={(e) => set('data_type', e.target.value)} options={DATA_TYPES.map((v) => ({ value: v, label: v }))} /></Field>
        <Field label="value_source"><Select value={f.value_source} onChange={(e) => set('value_source', e.target.value)} options={VALUE_SOURCES.map((v) => ({ value: v, label: v }))} /></Field>
        <Field label="binding_level"><Select value={f.binding_level} onChange={(e) => set('binding_level', e.target.value)} options={['group', 'product', 'variant'].map((v) => ({ value: v, label: v }))} /></Field>
        <Field label="Global"><div style={{ paddingTop: 6 }}><Checkbox label="Tüm kategorilerde" checked={f.is_global} onChange={(e) => set('is_global', e.target.checked)} /></div></Field>
      </div>
    </Dialog>
  )
}
