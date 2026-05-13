import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath } from 'url'
import VitePluginVueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue(), VitePluginVueDevTools()],
  resolve: {
    alias: {
       "@": fileURLToPath(new URL("./src", import.meta.url))
    }
  },
  base: "/dist/",
  build: {
    outDir: "../wwwroot/dist/",
    emptyOutDir: true,
    cssMinify: "esbuild",
    rolldownOptions: {
      output: {
        codeSplitting: true
      }
    }
  }
})
