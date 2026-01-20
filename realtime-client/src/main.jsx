import React from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'

const urlParams = new URLSearchParams(window.location.search)
const token = urlParams.get('token') || ''

createRoot(document.getElementById('root')).render(<App initialToken={token} />)
