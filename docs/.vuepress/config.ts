import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { viteBundler } from '@vuepress/bundler-vite'
import { defaultTheme } from '@vuepress/theme-default'
import { searchPlugin } from '@vuepress/plugin-search'
import { markdownChartPlugin } from '@vuepress/plugin-markdown-chart'
import { defineUserConfig } from 'vuepress'

const docsRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const labels = JSON.parse(fs.readFileSync(path.join(docsRoot, '.vuepress/ui/en.json'), 'utf8'))
const groups = ['getting-started', 'client', 'commands', 'beacon', 'plugins', 'marketplaces', 'deployment', 'contributing', 'troubleshooting']
function pages(folder: string): any[] {
  if (!fs.existsSync(folder)) return []
  return fs.readdirSync(folder, { withFileTypes: true })
    .filter(item => item.name !== 'node_modules' && !item.name.startsWith('.'))
    .sort((a, b) => a.name.localeCompare(b.name))
    .flatMap(item => {
      const full = path.join(folder, item.name)
      if (item.isDirectory()) return pages(full)
      if (!item.name.endsWith('.md')) return []
      const content = fs.readFileSync(full, 'utf8')
      const title = content.match(/^# (.+)$/m)?.[1] ?? item.name.replace(/\.md$/, '')
      return [{ text: title, link: '/' + path.relative(docsRoot, full).replaceAll(path.sep, '/').replace(/\.md$/, '.html') }]
    })
}
function themeLocale(prefix: string, ui: Record<string, string>): any {
  return {
    selectLanguageName: ui.language,
    navbar: groups.slice(0, 6).map(group => ({ text: ui[group] ?? labels[group], link: prefix + group + '/index.html' })),
    sidebar: groups.map(group => ({ text: ui[group] ?? labels[group], collapsible: true, children: pages(path.join(docsRoot, prefix.slice(1), group)) })),
    editLink: false,
    contributors: false,
    lastUpdated: false,
  }
}
const locales: Record<string, any> = { '/': { lang: 'en-US', title: 'MCC 2.0', description: 'Minecraft Console Client guides and reference' } }
const themeLocales: Record<string, any> = { '/': themeLocale('/', labels) }
const translatedRoot = path.join(docsRoot, 'l10n')
if (fs.existsSync(translatedRoot)) {
  for (const language of fs.readdirSync(translatedRoot)) {
    if (!fs.existsSync(path.join(translatedRoot, language, 'index.md'))) continue
    const prefix = '/l10n/' + language + '/'
    const uiPath = path.join(docsRoot, '.vuepress/ui', language + '.json')
    const ui = fs.existsSync(uiPath) ? { ...labels, ...JSON.parse(fs.readFileSync(uiPath, 'utf8')) } : { ...labels, language }
    locales[prefix] = { lang: language, title: 'MCC 2.0', description: ui.description ?? locales['/'].description }
    themeLocales[prefix] = themeLocale(prefix, ui)
  }
}
export default defineUserConfig({
  base: process.env.DOCS_BASE ?? '/',
  locales,
  pagePatterns: ['**/*.md', '!.vuepress', '!node_modules'],
  head: [['link', { rel: 'icon', href: '/icons/favicon-32x32.png' }]],
  markdown: { component: false },
  bundler: viteBundler(),
  theme: defaultTheme({ logo: '/images/MCC_logo.png', repo: 'MCCTeam/Minecraft-Console-Client', locales: themeLocales }),
  plugins: [{ name: "mcc-literal-markdown", extendsMarkdown: (md) => { md.set({ html: false }) } }, searchPlugin({ maxSuggestions: 12 }), markdownChartPlugin({ mermaid: true })],
})
