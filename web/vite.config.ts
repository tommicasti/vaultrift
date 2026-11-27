import { defineConfig, type ProxyOptions } from 'vite'
import react from '@vitejs/plugin-react'

const api = 'https://localhost:7129'

const toLocalApi = (extra: ProxyOptions = {}): ProxyOptions => ({
  target: api,
  secure: false,
  ...extra,
  configure: proxy => {
    proxy.on('proxyRes', response => {
      const cookies = response.headers['set-cookie']
      if (cookies) response.headers['set-cookie'] = cookies.map(cookie => cookie.replace(/;\s*secure/gi, ''))
    })
  },
})

export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    proxy: {
      '/api': toLocalApi(),
      '/hubs': toLocalApi({ ws: true }),
    },
  },
})
