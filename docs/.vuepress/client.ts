import { computed, defineComponent, h } from 'vue'
import {
  defineClientConfig,
  resolveRouteFullPath,
  useRoute,
  useRouter,
} from 'vuepress/client'

const guardEvent = (event: MouseEvent): boolean => {
  if (event.metaKey || event.altKey || event.ctrlKey || event.shiftKey) return false
  if (event.defaultPrevented) return false
  if (event.button !== undefined && event.button !== 0) return false

  if (event.currentTarget instanceof Element) {
    const target = event.currentTarget.getAttribute('target')
    if (target?.match(/\b_blank\b/i)) return false
  }

  event.preventDefault()
  return true
}

const SafeRouteLink = defineComponent({
  name: 'RouteLink',
  props: {
    to: {
      type: String,
      required: true,
    },
    active: Boolean,
    activeClass: {
      type: String,
      default: 'route-link-active',
    },
  },
  setup(props, { slots }) {
    const router = useRouter()
    const route = useRoute()
    const path = computed(() =>
      props.to.startsWith('#') || props.to.startsWith('?')
        ? props.to
        : `${__VUEPRESS_BASE__}${resolveRouteFullPath(props.to, route.path).substring(1)}`,
    )

    return () =>
      h(
        'a',
        {
          class: ['route-link', { [props.activeClass]: props.active }],
          href: path.value,
          onClick: (event: MouseEvent) => {
            if (guardEvent(event)) {
              void router.push(props.to).catch(() => {})
            }
          },
        },
        slots.default?.() ?? [],
      )
  },
})

export default defineClientConfig({
  enhance({ app }) {
    app.component('RouteLink', SafeRouteLink)
  },
})
