import { computed } from 'vue'
import { i18n } from '../i18n'

type Locale = 'en' | 'ar'

const RTL_LOCALES: Locale[] = ['ar']

export function useLocale() {
  const locale = computed<Locale>({
    get: () => i18n.global.locale.value as Locale,
    set: (value: Locale) => {
      i18n.global.locale.value = value
      document.documentElement.lang = value
      document.documentElement.dir = RTL_LOCALES.includes(value) ? 'rtl' : 'ltr'
    },
  })

  return {
    locale,
    isRtl: computed(() => RTL_LOCALES.includes(locale.value)),
  }
}