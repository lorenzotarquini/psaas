import type { Account } from '@/entities/account'
import type { Category } from '@/entities/category'
import type { Debt } from '@/entities/debt'
import type { Membership } from '@/entities/membership'
import type { ProjectionData, ProjectionParameters } from '@/entities/projection'
import type { Subscription } from '@/entities/subscription'
import type { Transaction } from '@/entities/transaction'
import { inScope, type PortfolioGroup } from '@/shared/lib/portfolio-scope'

// The same mapping as PortfolioScope in the domain, for the fake only.
const groupOf = (kind: Account['kind']): PortfolioGroup =>
  kind === 'Valuables' ? 'Stable' : ['DepositAccount', 'Investments', 'SavingsPlan', 'VirtualCurrency'].includes(kind) ? 'Investments' : 'Everyday'

/** UC13: a small catalog, traditional currencies and cryptocurrencies. */
export const fakeCurrencies = [
  { code: 'EUR', name: 'Euro', symbol: '€', type: 'Fiat' },
  { code: 'USD', name: 'United States Dollar', symbol: '$', type: 'Fiat' },
  { code: 'BTC', name: 'Bitcoin', symbol: '₿', type: 'Crypto' },
  { code: 'ETH', name: 'Ethereum', symbol: 'Ξ', type: 'Crypto' },
] as const

type Json = Record<string, unknown>

/** An in-memory MySync backend behind fetch: just enough of the API contract for the UI scenarios. */
export class FakeApi {
  accounts: Account[] = []
  debts: Debt[] = []
  categories: Category[] = []
  transactions: Transaction[] = []
  subscriptions: Subscription[] = []
  limits = { maxAccounts: 2, maxDebts: 1 }
  /** UC16-R4. */
  maxSubscriptions = 10
  /** UC13-R4: the euro until chosen. */
  preferences = { displayCurrency: 'EUR', chosen: false }
  /** UC14: what GET /api/projection answers beyond the starting point, which comes from the accounts. */
  projection: Omit<ProjectionData, 'start' | 'currency'> = {
    today: '2026-11-17',
    debts: [],
    subscriptions: [],
    suggested: { income: 0, fixedExpenses: 0, variableExpenses: 0, saving: 0, annualReturn: 2, inflation: 2, payments: [], subscriptions: [] },
    monthsOfData: 6,
    saved: null,
  }
  /** UC17: subscribed by default, so the other scenarios write freely. */
  membership: Membership = {
    status: 'Active',
    canWrite: true,
    price: 4.99,
    currency: 'EUR',
    interval: 'Month',
    currentPeriodEnd: '2026-12-15T09:00:00+00:00',
    cancelAtPeriodEnd: false,
    card: { brand: 'visa', last4: '4242', expMonth: 12, expYear: 2030 },
    lastFailure: null,
    payUrl: null,
    hasCustomer: true,
  }
  /** UC17 step 4: what Stripe says when MySync reads the state again. */
  membershipAfterRefresh: Membership | null = null
  requests: { method: string; path: string; body?: Json }[] = []
  /** Path and query of every call, in order. */
  urls: string[] = []
  private nextId = 1

  givenAccount(name: string, balance = 1000, kind: Account['kind'] = 'CurrentAccount', currency = 'EUR'): Account {
    const account: Account = {
      id: this.id(),
      kind,
      name,
      description: null,
      tag: null,
      initialBalance: balance,
      openedOn: '2026-11-01',
      balance,
      group: groupOf(kind),
      currency,
      displayBalance: balance,
    }
    this.accounts.push(account)
    return account
  }

  givenTransaction(account: Account, amount: number, categoryName = 'Spesa'): Transaction {
    const transaction: Transaction = {
      id: this.id(),
      kind: 'Expense',
      date: '2026-11-05',
      amount,
      accountId: account.id,
      accountName: account.name,
      counterAccountId: null,
      counterAccountName: null,
      categoryId: this.id(),
      categoryName,
      debtId: null,
      debtName: null,
      note: null,
      recordedAt: '2026-11-05T10:00:00Z',
      currency: account.currency,
      counterAmount: null,
      counterCurrency: null,
    }
    this.transactions.push(transaction)
    return transaction
  }

  /** UC16: a subscription of 'Abbonamenti' on the account; `dueOn` set means a charge to register. */
  givenSubscription(account: Account, name: string, amount: number, overrides: Partial<Subscription> = {}): Subscription {
    const subscription: Subscription = {
      id: this.id(),
      name,
      description: null,
      accountId: account.id,
      accountName: account.name,
      categoryId: this.id(),
      categoryName: 'Abbonamenti',
      amount,
      currency: account.currency,
      displayAmount: amount,
      firstChargeOn: '2026-10-05',
      endsOn: null,
      createdOn: '2026-10-01',
      active: true,
      nextChargeOn: '2026-12-05',
      dueOn: null,
      hasPayments: false,
      ...overrides,
    }
    this.subscriptions.push(subscription)
    return subscription
  }

  readonly fetch = async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const url = new URL(String(input), 'http://localhost')
    const method = init?.method ?? 'GET'
    const body = init?.body ? (JSON.parse(String(init.body)) as Json) : undefined
    this.requests.push({ method, path: url.pathname, body })
    this.urls.push(url.pathname + url.search)
    return this.route(method, url, body)
  }

  private route(method: string, url: URL, body?: Json): Response {
    const path = url.pathname
    if (method === 'GET' && path === '/api/accounts') return ok({ items: this.accounts, max: this.limits.maxAccounts })
    if (method === 'GET' && path === '/api/debts') return ok({ items: this.debts, max: this.limits.maxDebts })
    if (method === 'GET' && path === '/api/tags') return ok([])
    if (method === 'GET' && path === '/api/subscriptions') {
      const monthlyTotal = this.subscriptions.filter((s) => s.active).reduce((sum, s) => sum + s.displayAmount, 0)
      return ok({ items: this.subscriptions, monthlyTotal, currency: this.preferences.displayCurrency, max: this.maxSubscriptions })
    }
    if (method === 'POST' && path === '/api/subscriptions') {
      const account = this.accounts.find((a) => a.id === body!.accountId)!
      const created = this.givenSubscription(account, String(body!.name), Number(body!.amount), { firstChargeOn: String(body!.firstChargeOn), categoryId: String(body!.categoryId) })
      return ok(created, 201)
    }
    if (method === 'POST' && path.startsWith('/api/subscriptions/')) {
      const [, , , id, action] = path.split('/')
      const subscription = this.subscriptions.find((s) => s.id === id)!
      if (action === 'skip' || action === 'resume') Object.assign(subscription, action === 'skip' ? { dueOn: null } : { endsOn: null, active: true })
      if (action === 'cancel') subscription.endsOn = String(body!.endsOn)
      return ok(subscription)
    }
    if (method === 'GET' && path === '/api/membership') return ok(this.membership)
    if (method === 'POST' && path === '/api/membership/checkout') return ok({ url: 'https://checkout.stripe.test/session' })
    if (method === 'POST' && path === '/api/membership/portal') return ok({ url: 'https://billing.stripe.test/session' })
    if (method === 'POST' && path === '/api/membership/refresh') {
      if (this.membershipAfterRefresh) this.membership = this.membershipAfterRefresh
      return ok(this.membership)
    }
    if (method === 'GET' && path === '/api/currencies') return ok(fakeCurrencies)
    if (method === 'GET' && path === '/api/preferences') return ok(this.preferences)
    if (method === 'PUT' && path === '/api/preferences') {
      this.preferences = { displayCurrency: String(body!.displayCurrency), chosen: true }
      return ok(this.preferences)
    }
    if (method === 'GET' && path === '/api/categories') {
      const term = (url.searchParams.get('term') ?? '').toLowerCase()
      const kind = url.searchParams.get('kind')
      return ok(this.categories.filter((c) => (!kind || c.kind === kind) && c.name.toLowerCase().includes(term)))
    }
    if (method === 'GET' && path === '/api/projection') {
      const sum = (group: PortfolioGroup) => this.accounts.filter((a) => a.group === group).reduce((total, a) => total + a.balance, 0)
      return ok({ ...this.projection, start: { liquidity: sum('Everyday'), investments: sum('Investments'), stable: sum('Stable') }, currency: this.preferences.displayCurrency })
    }
    if (method === 'PUT' && path === '/api/projection') {
      this.projection.saved = body as unknown as ProjectionParameters
      return ok(body)
    }
    if (method === 'DELETE' && path === '/api/projection') {
      this.projection.saved = null
      return new Response(null, { status: 204 })
    }
    if (method === 'PUT' && path.startsWith('/api/categories/') && path.endsWith('/fixed')) {
      const category = this.categories.find((c) => c.id === path.split('/')[3])!
      category.fixed = Boolean(body!.fixed)
      return ok(category)
    }
    if (method === 'POST' && path === '/api/categories') {
      const category: Category = { id: this.id(), kind: body!.kind as Category['kind'], name: String(body!.name), fixed: false }
      this.categories.push(category)
      return ok(category, 201)
    }
    if (method === 'POST' && path === '/api/accounts') {
      const account: Account = {
        id: this.id(),
        kind: body!.kind as Account['kind'],
        name: String(body!.name),
        description: null,
        tag: null,
        initialBalance: Number(body!.initialBalance),
        openedOn: '2026-11-15',
        balance: Number(body!.initialBalance),
        group: groupOf(body!.kind as Account['kind']),
        currency: String(body!.currency),
        displayBalance: null,
      }
      this.accounts.push(account)
      return ok(account, 201)
    }
    if (method === 'POST' && path === '/api/debts') {
      const debt: Debt = {
        id: this.id(),
        kind: body!.kind as Debt['kind'],
        name: String(body!.name),
        description: null,
        tag: null,
        accountId: String(body!.accountId),
        initialResidual: Number(body!.residual),
        residual: Number(body!.residual),
        monthlyPayment: body!.monthlyPayment === undefined ? null : Number(body!.monthlyPayment),
        createdOn: '2026-11-15',
        paidOff: false,
        group: body!.kind === 'Mortgage' ? 'Stable' : 'Everyday',
        currency: String(body!.currency),
        displayResidual: Number(body!.residual),
      }
      this.debts.push(debt)
      return ok(debt, 201)
    }
    if (method === 'GET' && path === '/api/dashboard') {
      const scope = { stable: url.searchParams.get('includeStable') !== 'false', investments: url.searchParams.get('includeInvestments') !== 'false' }
      const netWorth = this.accounts.filter((a) => inScope(a.group, scope)).reduce((sum, a) => sum + a.balance, 0)
      const debtsResidual = this.debts.filter((d) => inScope(d.group, scope)).reduce((sum, d) => sum + d.residual, 0)
      return ok({
        netWorth,
        debtsResidual,
        accounts: this.accounts,
        debts: this.debts,
        recentTransactions: this.transactions,
        limits: this.limits,
        currency: this.preferences.displayCurrency,
      })
    }
    if (method === 'GET' && path === '/api/net-worth') return ok({ period: url.searchParams.get('period'), points: [], change: 0, changePercent: null, currency: this.preferences.displayCurrency })
    if (method === 'GET' && path === '/api/transactions') {
      return ok({ items: this.transactions, page: 1, pageSize: 25, totalCount: this.transactions.length, expenseTotal: 0, incomeTotal: 0, currency: this.preferences.displayCurrency })
    }
    if (method === 'POST' && path === '/api/transactions') {
      // UC16-R7: a linked expense in the month of the charge settles it.
      const paid = this.subscriptions.find((s) => s.id === body!.subscriptionId && s.dueOn?.slice(0, 7) === String(body!.date).slice(0, 7))
      if (paid) paid.dueOn = null
      return ok({ id: this.id(), ...body }, 201)
    }
    if (method === 'PUT' && path.startsWith('/api/accounts/')) {
      const account = this.accounts.find((a) => a.id === path.split('/').pop())!
      Object.assign(account, body)
      return ok(account)
    }
    if (method === 'PUT' && path.startsWith('/api/debts/')) {
      const debt = this.debts.find((d) => d.id === path.split('/').pop())!
      Object.assign(debt, body)
      return ok(debt)
    }
    if (method === 'DELETE' && path.startsWith('/api/transactions/')) {
      this.transactions = this.transactions.filter((t) => t.id !== path.split('/').pop())
      return new Response(null, { status: 204 })
    }
    return ok({ title: `Not faked: ${method} ${path}` }, 404)
  }

  private id() {
    return `00000000-0000-0000-0000-${String(this.nextId++).padStart(12, '0')}`
  }
}

const ok = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
