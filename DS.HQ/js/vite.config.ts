import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath } from 'url'
import VitePluginVueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig(({ command }) => {
  return {
    plugins: [vue(), VitePluginVueDevTools()],
    resolve: {
      alias: {
        "@": fileURLToPath(new URL("./src", import.meta.url))
      }
    },
    base: command === "build" ? "/dist/" : "/",
    build: {
      outDir: "../wwwroot/dist/",
      emptyOutDir: true,
      cssMinify: "esbuild",
      rollupOptions: {
        output: {
          manualChunks: undefined
        }
      }
    },
    server: {
      proxy: {
        '/api': {
          target: "https://localhost:44311",
          changeOrigin: true,
          secure: false
        }
      }
    }
  }
})
