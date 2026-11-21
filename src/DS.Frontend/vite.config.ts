import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import ui from '@nuxt/ui/vite'
import { fileURLToPath } from 'url'

export default defineConfig(({ command }) => {
  return {
    plugins: [
      vue(),
      ui({
        ui: {
          colors: {
            primary: 'blue',
            neutral: 'slate'
          }
        },
        icon: {
          clientBundle: {
            scan: true
          }
        }
      })
    ],
    resolve: {
      alias: {
        "@": fileURLToPath(new URL("./src", import.meta.url))
      }
    },
    base: command === "build" ? "/dist/" : "/",
    build: {
      outDir: "../DS.Backend/wwwroot/dist/",
      emptyOutDir: true
    },
    server: {
      port: 5174,
      proxy: {
        '/api': {
          target: "https://localhost:44310",
          changeOrigin: true,
          secure: false,
          ws: true
        }
      }
    }
  }
})
