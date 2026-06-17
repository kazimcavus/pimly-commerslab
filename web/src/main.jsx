import React from 'react'
import { createRoot } from 'react-dom/client'
import './styles/styles.css'
import './styles/kit.css'
import './styles/app.css'
import { App } from './App.jsx'

document.documentElement.setAttribute('data-theme', 'light')
createRoot(document.getElementById('root')).render(<App />)
