import { computed } from 'vue'
import { useLocale } from './useLocale'

const UNITS: Array<[Intl.RelativeTimeFormatUnit, number]> = [
  ['year', 365 * 24 * 60 * 60 * 1000],
  ['month', 30 * 24 * 60 * 60 * 1000],
  ['day', 24 * 60 * 60 * 1000],
  ['hour', 60 * 60 * 1000],
  ['minute', 60 * 1000],
  ['second', 1000],
]

export function useRelativeTime() {
  const { locale } = useLocale()

  const formatter = computed(
    () => new Intl.RelativeTimeFormat(locale.value, { numeric: 'auto' }),
  )

  function formatRelativeTime(value: string): string {
    const diffMs = new Date(value).getTime() - Date.now()

    for (const [unit, unitMs] of UNITS) {
      if (Math.abs(diffMs) >= unitMs || unit === 'second') {
        return formatter.value.format(Math.round(diffMs / unitMs), unit)
      }
    }
    return formatter.value.format(0, 'second')
  }

  return { formatRelativeTime }
}
