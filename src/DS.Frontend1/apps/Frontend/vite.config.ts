import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath } from 'url'
import VitePluginVueDevTools from 'vite-plugin-vue-devtools'
import basicSsl from '@vitejs/plugin-basic-ssl'

export default defineConfig(({ command }) => {
  return {
    plugins: [vue(), basicSsl(), VitePluginVueDevTools()],
    resolve: {
      alias: {
        "@": fileURLToPath(new URL("./src", import.meta.url))
      }
    },
    base: command === "build" ? "/dist/" : "/",
    build: {
      outDir: fileURLToPath(new URL("../../../DS.Backend/wwwroot/dist/", import.meta.url)),
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
          target: "https://localhost:44310",
          changeOrigin: true,
          secure: false
        },
        '/hangfire': {
          target: "https://localhost:44310",
          changeOrigin: true,
          secure: false
        }
      }
    }
  }
})
