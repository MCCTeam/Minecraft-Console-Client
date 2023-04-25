/* This file is automatically generated by "gen_configs.py" */
import type { SiteLocaleData  } from '@vuepress/shared'
import type { DefaultThemeLocaleData } from '@vuepress/theme-default'
import { headConfig } from '../head.js'

const Translation = require('../../translations/fr.json')

export const mainConfig_fr: SiteLocaleData  = {
    lang: 'fr',
    title: Translation.title,
    description: Translation.description,
    head: headConfig
}

export const defaultThemeConfig_fr: DefaultThemeLocaleData = {
    selectLanguageName: "Français",
    selectLanguageText: Translation.theme.selectLanguageText,
    selectLanguageAriaLabel: Translation.theme.selectLanguageAriaLabel,

    navbar: [
        {
            text: Translation.navbar.AboutAndFeatures,
            link: "/l10n/fr/guide/",
            activeMatch: "/l10n/fr/index.html",
        },
        
        {
            text: Translation.navbar.Installation,
            link: "/l10n/fr/guide/installation.md",
            activeMatch: "/l10n/fr/guide/installation.html",
        },
      
        {
            text: Translation.navbar.Usage,
            link: "/l10n/fr/guide/usage.md",
            activeMatch: "/l10n/fr/guide/usage.html",
        },
      
        {
            text: Translation.navbar.Configuration,
            link: "/l10n/fr/guide/configuration.md",
            activeMatch: "/l10n/fr/guide/configuration.html",
        },
      
        {
            text: Translation.navbar.ChatBots,
            link: "/l10n/fr/guide/chat-bots.md",
            activeMatch: "/l10n/fr/guide/chat-bots.html",
        },
      
        {
            text: Translation.helpUsTranslate,
            link: "https://crwd.in/minecraft-console-client",
        },
    ],

    sidebar: [
        "/l10n/fr/guide/README.md", 
        "/l10n/fr/guide/installation.md", 
        "/l10n/fr/guide/usage.md", 
        "/l10n/fr/guide/configuration.md", 
        "/l10n/fr/guide/creating-text-script.md",
        "/l10n/fr/guide/chat-bots.md", 
        "/l10n/fr/guide/creating-bots.md", 
        "/l10n/fr/guide/contibuting.md"
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
