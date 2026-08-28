import { describe, it, expect, afterEach } from 'vitest'
import en from '@/i18n/en/common.json'
import ar from '@/i18n/ar/common.json'
import { useLocale } from '@/composables/useLocale'

function collectKeyPaths(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    return [prefix]
  }
  return Object.entries(value as Record<string, unknown>).flatMap(([key, child]) =>
    collectKeyPaths(child, prefix ? `${prefix}.${key}` : key),
  )
}

describe('i18n key parity', () => {
  it('en and ar carry exactly the same set of keys', () => {
    const enKeys = collectKeyPaths(en).sort()
    const arKeys = collectKeyPaths(ar).sort()

    expect(arKeys).toEqual(enKeys)
  })

  it('has a portal.* namespace with the keys this story introduces', () => {
    const enKeys = new Set(collectKeyPaths(en))

    const expectedKeys = [
      'portal.dashboard.welcome',
      'portal.dashboard.metrics.open',
      'portal.dashboard.metrics.pending',
      'portal.dashboard.metrics.resolved',
      'portal.dashboard.recentTickets',
      'portal.dashboard.submitCta',
      'portal.dashboard.viewAllCta',
      'portal.tickets.title',
      'portal.tickets.empty',
      'portal.tickets.emptyCta',
      'portal.tickets.loading',
      'portal.tickets.error',
      'portal.tickets.retry',
      'portal.ticket.submit.title',
      'portal.ticket.submit.fields.subject',
      'portal.ticket.submit.fields.description',
      'portal.ticket.submit.fields.priority',
      'portal.ticket.submit.success',
      'portal.ticket.details.title',
      'portal.ticket.details.status',
      'portal.ticket.details.conversation',
      'portal.errors.unauthorized',
      'portal.errors.notFound',
      'portal.errors.generic',
    ]

    for (const key of expectedKeys) {
      expect(enKeys.has(key), `missing key: ${key}`).toBe(true)
    }
  })

  it('has a reports.* namespace and nav.reports key that this story introduces', () => {
    const enKeys = new Set(collectKeyPaths(en))

    const expectedKeys = [
      'nav.reports',
      'reports.title',
      'reports.refresh',
      'reports.volume.total',
      'reports.volume.open',
      'reports.volume.resolved',
      'reports.agents.title',
      'reports.agents.empty',
      'reports.agents.headers.name',
      'reports.agents.headers.count',
      'reports.sla.title',
      'reports.sla.withinSla',
      'reports.sla.atRisk',
      'reports.sla.breached',
      'reports.resolution.title',
      'reports.resolution.averageLabel',
      'reports.resolution.none',
      'reports.errors.loadFailed',
    ]

    for (const key of expectedKeys) {
      expect(enKeys.has(key), `missing key: ${key}`).toBe(true)
    }
  })

  it('has a security.* namespace that this story introduces', () => {
    const enKeys = new Set(collectKeyPaths(en))

    const expectedKeys = [
      'security.nav.users',
      'security.nav.auditLog',
      'security.users.title',
      'security.users.confirmSelf',
      'security.users.actions.assignRole',
      'security.users.actions.disable',
      'security.users.actions.enable',
      'security.audit.title',
      'security.audit.filters.action',
      'security.audit.action.user.login.succeeded',
      'security.audit.action.user.login.failed',
      'security.audit.action.user.role.assigned',
      'security.audit.action.user.disabled',
      'security.audit.action.user.enabled',
      'security.audit.action.security.access.denied',
    ]

    for (const key of expectedKeys) {
      expect(enKeys.has(key), `missing key: ${key}`).toBe(true)
    }
  })

  it('has sla.escalation.* and notificationCenter.* namespaces that CRM-63 introduces', () => {
    const enKeys = new Set(collectKeyPaths(en))

    const expectedKeys = [
      'sla.escalation.title',
      'sla.escalation.create',
      'sla.escalation.rule',
      'sla.escalation.trigger.label',
      'sla.escalation.trigger.atRisk',
      'sla.escalation.trigger.breached',
      'sla.escalation.actions.notifyAgent',
      'sla.escalation.actions.notifyManager',
      'sla.escalation.status.active',
      'sla.escalation.status.inactive',
      'sla.escalation.validation.atLeastOneAction',
      'notificationCenter.title',
      'notificationCenter.markAllRead',
      'notificationCenter.empty',
      'notificationCenter.loading',
      'notificationCenter.error',
      'notificationCenter.item.sla.atRisk',
      'notificationCenter.item.sla.breached',
    ]

    for (const key of expectedKeys) {
      expect(enKeys.has(key), `missing key: ${key}`).toBe(true)
    }
  })

  it('has a knowledgeBase.categories.* namespace that CRM-65 introduces, with non-empty values in en and ar', () => {
    const enKeys = new Set(collectKeyPaths(en))

    const expectedKeys = [
      'knowledgeBase.categories.nav',
      'knowledgeBase.categories.title',
      'knowledgeBase.categories.new',
      'knowledgeBase.categories.editTitle',
      'knowledgeBase.categories.edit',
      'knowledgeBase.categories.activate',
      'knowledgeBase.categories.deactivate',
      'knowledgeBase.categories.empty',
      'knowledgeBase.categories.loading',
      'knowledgeBase.categories.emptyPortal',
      'knowledgeBase.categories.emptyArticles',
      'knowledgeBase.categories.articleCount',
      'knowledgeBase.categories.columns.name',
      'knowledgeBase.categories.columns.status',
      'knowledgeBase.categories.columns.actions',
      'knowledgeBase.categories.fields.name',
      'knowledgeBase.categories.fields.description',
      'knowledgeBase.categories.fields.status',
      'knowledgeBase.categories.status.active',
      'knowledgeBase.categories.status.inactive',
      'knowledgeBase.categories.validation.nameRequired',
      'knowledgeBase.categories.validation.nameTooLong',
      'knowledgeBase.categories.validation.descriptionTooLong',
      'knowledgeBase.categories.errors.duplicate',
      'knowledgeBase.categories.errors.saveFailed',
      'knowledgeBase.filters.category',
      'knowledgeBase.filters.allCategories',
      'knowledgeBase.fields.category',
      'knowledgeBase.fields.categoryPlaceholder',
    ]

    for (const key of expectedKeys) {
      expect(enKeys.has(key), `missing key: ${key}`).toBe(true)
    }

    function getPath(obj: unknown, path: string): unknown {
      return path.split('.').reduce<unknown>((acc, segment) => {
        if (acc && typeof acc === 'object') {
          return (acc as Record<string, unknown>)[segment]
        }
        return undefined
      }, obj)
    }

    for (const key of expectedKeys) {
      const enValue = getPath(en, key)
      const arValue = getPath(ar, key)
      expect(typeof enValue === 'string' && enValue.trim().length > 0, `en.${key} should be non-empty`).toBe(true)
      expect(typeof arValue === 'string' && arValue.trim().length > 0, `ar.${key} should be non-empty`).toBe(true)
    }
  })
})

describe('RTL direction toggling (CRM-92)', () => {
  afterEach(() => {
    const { locale } = useLocale()
    locale.value = 'en'
  })

  it('flips document.documentElement.dir to rtl for the ar locale and back to ltr for en', () => {
    const { locale } = useLocale()

    locale.value = 'ar'
    expect(document.documentElement.dir).toBe('rtl')
    expect(document.documentElement.lang).toBe('ar')

    locale.value = 'en'
    expect(document.documentElement.dir).toBe('ltr')
    expect(document.documentElement.lang).toBe('en')
  })
})
