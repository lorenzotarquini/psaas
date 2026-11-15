import type { Account } from '@/entities/account'
import type { Category } from '@/entities/category'
import type { Debt } from '@/entities/debt'
import type { Transaction } from '@/entities/transaction'

type Json = Record<string, unknown>

/** An in-memory MySync backend behind fetch: just enough of the API contract for the UI scenarios. */
export class FakeApi {
  accounts: Account[] = []
  debts: Debt[] = []
  categories: Category[] = []
  transactions: Transaction[] = []
  limits = { maxAccounts: 2, maxDebts: 1 }
  requests: { method: string; path: string; body?: Json }[] = []
  private nextId = 1

  givenAccount(name: string, balance = 1000): Account {
    const account: Account = { id: this.id(), kind: 'CurrentAccount', name, description: null, tag: null, initialBalance: balance, openedOn: '2026-11-01', balance }
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
      categoryId: this.id(),
      categoryName,
      debtId: null,
      debtName: null,
      note: null,
      recordedAt: '2026-11-05T10:00:00Z',
    }
    this.transactions.push(transaction)
    return transaction
  }

  readonly fetch = async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const url = new URL(String(input), 'http://localhost')
    const method = init?.method ?? 'GET'
    const body = init?.body ? (JSON.parse(String(init.body)) as Json) : undefined
    this.requests.push({ method, path: url.pathname, body })
    return this.route(method, url, body)
  }

  private route(method: string, url: URL, body?: Json): Response {
    const path = url.pathname
    if (method === 'GET' && path === '/api/accounts') return ok({ items: this.accounts, max: this.limits.maxAccounts })
    if (method === 'GET' && path === '/api/debts') return ok({ items: this.debts, max: this.limits.maxDebts })
    if (method === 'GET' && path === '/api/tags') return ok([])
    if (method === 'GET' && path === '/api/categories') {
      const term = (url.searchParams.get('term') ?? '').toLowerCase()
      const kind = url.searchParams.get('kind')
      return ok(this.categories.filter((c) => (!kind || c.kind === kind) && c.name.toLowerCase().includes(term)))
    }
    if (method === 'POST' && path === '/api/categories') {
      const category: Category = { id: this.id(), kind: body!.kind as Category['kind'], name: String(body!.name) }
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
        createdOn: '2026-11-15',
        paidOff: false,
      }
      this.debts.push(debt)
      return ok(debt, 201)
    }
    if (method === 'GET' && path === '/api/dashboard') {
      const netWorth = this.accounts.reduce((sum, a) => sum + a.balance, 0)
      const debtsResidual = this.debts.reduce((sum, d) => sum + d.residual, 0)
      return ok({
        netWorth,
        debtsResidual,
        accounts: this.accounts,
        debts: this.debts,
        recentTransactions: this.transactions,
        limits: this.limits,
      })
    }
    if (method === 'GET' && path === '/api/net-worth') return ok({ period: url.searchParams.get('period'), points: [], change: 0, changePercent: null })
    if (method === 'GET' && path === '/api/transactions') {
      return ok({ items: this.transactions, page: 1, pageSize: 25, totalCount: this.transactions.length, expenseTotal: 0, incomeTotal: 0 })
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
