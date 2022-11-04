/* This file is automatically generated by "gen_configs.py" */
import type { SiteLocaleData  } from '@vuepress/shared'
import type { DefaultThemeLocaleData } from '@vuepress/theme-default'
import { headConfig } from '../head.js'

const Translation = require('../../translations/en.json')

export const mainConfig_en: SiteLocaleData  = {
    lang: 'en',
    title: Translation.title,
    description: Translation.description,
    head: headConfig
}

export const defaultThemeConfig_en: DefaultThemeLocaleData = {
    selectLanguageName: "English",
    selectLanguageText: Translation.theme.selectLanguageText,
    selectLanguageAriaLabel: Translation.theme.selectLanguageAriaLabel,

    navbar: [
        {
            text: Translation.navbar.AboutAndFeatures,
            link: "/guide/",
        },
        
        {
            text: Translation.navbar.Installation,
            link: "/guide/installation.md",
        },
      
        {
            text: Translation.navbar.Usage,
            link: "/guide/usage.md",
        },
      
        {
            text: Translation.navbar.Configuration,
            link: "/guide/configuration.md",
        },
      
        {
            text: Translation.navbar.ChatBots,
            link: "/guide/chat-bots.md",
        },
    ],

    sidebar: [
        "/guide/README.md", 
        "/guide/installation.md", 
        "/guide/usage.md", 
        "/guide/configuration.md", 
        "/guide/chat-bots.md", 
        "/guide/creating-bots.md", 
        "/guide/contibuting.md"
    ],

    // page meta
    editLinkText: Translation.theme.editLinkText,
    lastUpdatedText: Translation.theme.lastUpdatedText,
    contributorsText: Translation.theme.contributorsText,

    // custom containers
    tip: Translation.theme.tip,
    warning: Translation.theme.warning,
    danger: Translation.theme.danger,

    // 404 page
    notFound: Translation.theme.notFound,
    backToHome: Translation.theme.backToHome,

    // a11y
    openInNewWindow: Translation.theme.openInNewWindow,
    toggleColorMode: Translation.theme.toggleColorMode,
    toggleSidebar: Translation.theme.toggleSidebar,
}
