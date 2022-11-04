import type { SiteLocaleData  } from '@vuepress/shared'
import type { DefaultThemeLocaleData } from '@vuepress/theme-default'
import { headConfig } from '../head.js'

const Translation = require('../../translations/$LanguageCode$.json')

export const mainConfig_$LanguageCodeEscaped$: SiteLocaleData  = {
    lang: '$LanguageCode$',
    title: Translation.title,
    description: Translation.description,
    head: headConfig
}

export const defaultThemeConfig_$LanguageCodeEscaped$: DefaultThemeLocaleData = {
    selectLanguageName: "$LanguageName$",
    selectLanguageText: Translation.theme.selectLanguageText,
    selectLanguageAriaLabel: Translation.theme.selectLanguageAriaLabel,

    navbar: [
        {
            text: Translation.navbar.AboutAndFeatures,
            link: "$PathToPage$/guide/",
        },
        
        {
            text: Translation.navbar.Installation,
            link: "$PathToPage$/guide/installation.md",
        },
      
        {
            text: Translation.navbar.Usage,
            link: "$PathToPage$/guide/usage.md",
        },
      
        {
            text: Translation.navbar.Configuration,
            link: "$PathToPage$/guide/configuration.md",
        },
      
        {
            text: Translation.navbar.ChatBots,
            link: "$PathToPage$/guide/chat-bots.md",
        },
    ],

    sidebar: [
        "$PathToPage$/guide/README.md", 
        "$PathToPage$/guide/installation.md", 
        "$PathToPage$/guide/usage.md", 
        "$PathToPage$/guide/configuration.md", 
        "$PathToPage$/guide/chat-bots.md", 
        "$PathToPage$/guide/creating-bots.md", 
        "$PathToPage$/guide/contibuting.md"
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
