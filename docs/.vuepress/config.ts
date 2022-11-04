import process from 'node:process'
import { viteBundler } from '@vuepress/bundler-vite'
import { webpackBundler } from '@vuepress/bundler-webpack'
import { defineUserConfig } from '@vuepress/cli'
import { shikiPlugin } from '@vuepress/plugin-shiki'
import { defaultTheme } from '@vuepress/theme-default'
import { getDirname, path } from '@vuepress/utils'
import { backToTopPlugin } from "@vuepress/plugin-back-to-top"
import { externalLinkIconPlugin } from "@vuepress/plugin-external-link-icon"
import { nprogressPlugin } from "@vuepress/plugin-nprogress"
import { searchPlugin } from "@vuepress/plugin-search";
import { activeHeaderLinksPlugin } from '@vuepress/plugin-active-header-links'

import { headConfig } from './configs/head.js'
import { mainConfig, defaultThemeConfig } from './configs/locales_config.js'

const __dirname = getDirname(import.meta.url)
const isProd = process.env.NODE_ENV === 'production'

export default defineUserConfig({
  // set site base to default value
  base: '/',

  // extra tags in `<head>`
  head: headConfig,

  // site-level locales config
  locales: mainConfig,

  // specify bundler via environment variable
  bundler: process.env.DOCS_BUNDLER === 'webpack' ? webpackBundler() : viteBundler(),

  // configure default theme
  theme: defaultTheme({
    logo: "/images/MCC_logo.png",
    repo: "MCCTeam/Minecraft-Console-Client",
    docsBranch: 'master',
    docsDir: 'docs',

    // theme-level locales config
    locales: defaultThemeConfig,

    themePlugins: {
      // only enable git plugin in production mode
      git: isProd,
      // use shiki plugin in production mode instead
      prismjs: !isProd,
    },
  }),

  // configure markdown
  markdown: {
    importCode: {
      handleImportPath: (str) =>
        str.replace(/^@vuepress/, path.resolve(__dirname, '../../ecosystem')),
    },
  },

  // use plugins
  plugins: [
    backToTopPlugin(),
    externalLinkIconPlugin(),
    nprogressPlugin(),
    // only enable shiki plugin in production mode
    isProd ? shikiPlugin({ theme: 'dark-plus' }) : [],
    searchPlugin({
        maxSuggestions: 15,
        hotKeys: ["s", "/"],
        locales: {
            "/": {
                placeholder: "Search",
            },
        },
    }),
    activeHeaderLinksPlugin(),
  ],
})
