import process from 'node:process'

import { viteBundler } from '@vuepress/bundler-vite'
import { webpackBundler } from '@vuepress/bundler-webpack'
import { markdownChartPlugin } from '@vuepress/plugin-markdown-chart'
import { redirectPlugin } from '@vuepress/plugin-redirect'
import { searchPlugin } from '@vuepress/plugin-search'
import { shikiPlugin } from '@vuepress/plugin-shiki'
import { defaultTheme } from '@vuepress/theme-default'
import { defineUserConfig } from 'vuepress'

import type { Plugin } from 'vite'

import { headConfig } from './configs/head.js'
import { mainConfig, defaultThemeConfig } from './configs/locales_config.js'

const isProd = process.env.NODE_ENV === 'production'

function vueTemplateTolerantPlugin(): Plugin {
  let compilerSfc: typeof import('@vue/compiler-sfc') | undefined
  return {
    name: 'vue-template-tolerant',
    enforce: 'pre',
    async transform(code, id) {
      if (!id.endsWith('.html.vue') || !id.includes('/l10n/')) return
      compilerSfc ??= await import('@vue/compiler-sfc')
      const { errors } = compilerSfc.parse(code, { filename: id })
      if (errors.length === 0) return

      const lines = code.split('\n')
      const mdPath = id
        .replace(/\.vuepress\/\.temp\/pages\//, '')
        .replace(/\.html\.vue$/, '.md')

      const yellow = '\x1b[33m'
      const red = '\x1b[31m'
      const dim = '\x1b[2m'
      const cyan = '\x1b[36m'
      const reset = '\x1b[0m'

      let output = `${yellow}[vue-template-tolerant]${reset} ${errors.length} error(s) in translation page (replaced with placeholder)\n`
      output += `  ${dim}source:${reset} ${cyan}${mdPath}${reset}\n`

      for (const err of errors as any[]) {
        const msg = err.message ?? String(err)
        const loc = err.loc as { start: { line: number; column: number }; end: { line: number; column: number }; source?: string } | undefined
        if (loc) {
          output += `\n  ${red}error${reset} ${msg}\n`
          output += `  ${dim}at ${id}:${loc.start.line}:${loc.start.column}${reset}\n`
          const startLine = Math.max(0, loc.start.line - 3)
          const endLine = Math.min(lines.length, loc.start.line + 2)
          for (let i = startLine; i < endLine; i++) {
            const lineNum = String(i + 1).padStart(5)
            const marker = i + 1 === loc.start.line ? `${red}>${reset}` : ' '
            const lineContent = lines[i].length > 200 ? lines[i].slice(0, 200) + '...' : lines[i]
            output += `  ${marker} ${dim}${lineNum}${reset} | ${lineContent}\n`
            if (i + 1 === loc.start.line) {
              const col = loc.start.column
              output += `    ${' '.repeat(5)} | ${' '.repeat(col)}${red}^${reset}\n`
            }
          }
        } else {
          output += `\n  ${red}error${reset} ${msg}\n`
        }
      }

      console.warn(output)
      return {
        code: '<template><div><p>This page has HTML errors in its translation source and could not be rendered.</p></div></template>',
        map: null,
      }
    },
  }
}

export default defineUserConfig({
  // set site base to default value
  base: '/',

  pagePatterns: ['**/*.md', '!.vuepress', '!node_modules', '!superpowers'],

  // extra tags in `<head>`
  head: headConfig,

  // site-level locales config
  locales: mainConfig,

  // specify bundler via environment variable
  bundler:
    process.env.DOCS_BUNDLER === 'webpack'
      ? webpackBundler()
      : viteBundler({
          viteOptions: {
            plugins: [vueTemplateTolerantPlugin()],
          },
        }),

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
