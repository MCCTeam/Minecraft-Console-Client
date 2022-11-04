/* This file is automatically generated by "gen_configs.py" */
import type { SiteLocaleData  } from '@vuepress/shared'
import type { DefaultThemeLocaleData } from '@vuepress/theme-default'
import { headConfig } from '../head.js'

const Translation = require('../../translations/nl.json')

export const mainConfig_nl: SiteLocaleData  = {
    lang: 'nl',
    title: Translation.title,
    description: Translation.description,
    head: headConfig
}

export const defaultThemeConfig_nl: DefaultThemeLocaleData = {
    selectLanguageName: "Nederlands",
    selectLanguageText: Translation.theme.selectLanguageText,
    selectLanguageAriaLabel: Translation.theme.selectLanguageAriaLabel,

    navbar: [
        {
            text: Translation.navbar.AboutAndFeatures,
            link: "/l10n/nl/guide/",
        },
        
        {
            text: Translation.navbar.Installation,
            link: "/l10n/nl/guide/installation.md",
        },
      
        {
            text: Translation.navbar.Usage,
            link: "/l10n/nl/guide/usage.md",
        },
      
        {
            text: Translation.navbar.Configuration,
            link: "/l10n/nl/guide/configuration.md",
        },
      
        {
            text: Translation.navbar.ChatBots,
            link: "/l10n/nl/guide/chat-bots.md",
        },
    ],

    sidebar: [
        "/l10n/nl/guide/README.md", 
        "/l10n/nl/guide/installation.md", 
        "/l10n/nl/guide/usage.md", 
        "/l10n/nl/guide/configuration.md", 
        "/l10n/nl/guide/chat-bots.md", 
        "/l10n/nl/guide/creating-bots.md", 
        "/l10n/nl/guide/contibuting.md"
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
