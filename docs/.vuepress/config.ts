import process from 'node:process'

import { viteBundler } from '@vuepress/bundler-vite'
import { webpackBundler } from '@vuepress/bundler-webpack'
import { markdownChartPlugin } from '@vuepress/plugin-markdown-chart'
import { redirectPlugin } from '@vuepress/plugin-redirect'
import { searchPlugin } from '@vuepress/plugin-search'
import { shikiPlugin } from '@vuepress/plugin-shiki'
import { defaultTheme } from '@vuepress/theme-default'
import { defineUserConfig } from 'vuepress'

import { headConfig } from './configs/head.js'
import { mainConfig, defaultThemeConfig } from './configs/locales_config.js'

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
    hostname: 'https://mccteam.github.io',
    logo: '/images/MCC_logo.png',
    repo: 'MCCTeam/Minecraft-Console-Client',
    docsBranch: 'master',
    docsDir: 'docs',

    // theme-level locales config
    locales: defaultThemeConfig,

    themePlugins: {
      // only enable git plugin in production mode
      git: isProd,
      // use shiki plugin in production mode instead
      prismjs: !isProd,
      seo: isProd
        ? {
            canonical: 'https://mccteam.github.io/',
          }
        : false,
      sitemap: isProd
        ? {
            changefreq: 'weekly',
          }
        : false,
    },
  }),

  // use plugins
  plugins: [
    redirectPlugin({
      hostname: 'https://mccteam.github.io',
      config: {
        '/r/entity.html': 'https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Mapping/EntityType.cs',
        '/r/entity/index.html': 'https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Mapping/EntityType.cs',

        '/r/item.html': 'https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Inventory/ItemType.cs',
        '/r/item/index.html': 'https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Inventory/ItemType.cs',

        '/r/block.html': 'https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Mapping/Material.cs',
        '/r/block/index.html': 'https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Mapping/Material.cs',

        '/r/l-code.html': 'https://github.com/MCCTeam/Minecraft-Console-Client/discussions/2239#discussion-4447461',
        '/r/l-code/index.html': 'https://github.com/MCCTeam/Minecraft-Console-Client/discussions/2239#discussion-4447461',

        '/r/dc-fmt.html': 'https://www.writebots.com/discord-text-formatting/',
        '/r/dc-fmt/index.html': 'https://www.writebots.com/discord-text-formatting/',

        '/r/tg-fmt.html': 'https://sendpulse.com/blog/telegram-text-formatting',
        '/r/tg-fmt/index.html': 'https://sendpulse.com/blog/telegram-text-formatting',
      },
    }),

    ...(isProd ? [shikiPlugin({ theme: 'dark-plus' })] : []),

    searchPlugin({
      maxSuggestions: 15,
      hotKeys: ['s', '/'],
      locales: {
        '/': {
          placeholder: 'Search',
        },
      },
    }),

    markdownChartPlugin({
      mermaid: true,
    }),
  ],
})
