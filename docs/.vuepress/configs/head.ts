import type { HeadConfig } from '@vuepress/core'

export const headConfig: HeadConfig[] = [
    [
        'link',
        {
            rel: 'icon',
            type: 'image/png',
            sizes: '16x16',
            href: `/icons/favicon-16x16.png`,
        },
    ],
    [
        'link',
        {
            rel: 'icon',
            type: 'image/png',
            sizes: '32x32',
            href: `/icons/favicon-32x32.png`,
        },
    ],
    ['link', { rel: 'manifest', href: '/manifest.webmanifest' }],
    ['meta', { name: 'application-name', content: 'MCC Doc' }],
    ['meta', { name: 'apple-mobile-web-app-title', content: 'MCC Doc' }],
    ['meta', { name: 'apple-mobile-web-app-status-bar-style', content: 'black' }],
    [
        'link',
        { rel: 'apple-touch-icon', href: `/icons/apple-touch-icon.png` },
    ],
    ['meta', { name: 'msapplication-TileColor', content: '#3eaf7c' }],
    ['meta', { name: 'theme-color', content: '#3eaf7c' }],
]
