import { describe, expect, it } from 'vitest'
import type { Module } from '@/entities/catalog'
import { toManifest } from './manifest'

const mysync: Module = {
  id: 'mysync',
  name: 'MySync',
  description: null,
  icon: 'wallet',
  status: 'Available',
  version: '1.0.0',
  requires: [],
  permissions: ['mysync.use'],
  provisionUrl: null,
  deprovisionUrl: 'https://mysync.example.com/hooks/deprovision',
  updatedAt: '2026-11-20T10:00:00Z',
  tenantCount: 3,
  tiers: [{ level: 1, name: 'Base', features: [], limits: { accounts: 2 } }],
}

describe('UC15-R7: editing a module', () => {
  it('starts from the current manifest, tiers and hooks included, without catalog-only fields', () => {
    const manifest = toManifest(mysync)

    expect(manifest).toEqual({
      id: 'mysync',
      name: 'MySync',
      description: null,
      version: '1.0.0',
      icon: 'wallet',
      status: 'Available',
      requires: [],
      permissions: ['mysync.use'],
      provisionUrl: null,
      deprovisionUrl: 'https://mysync.example.com/hooks/deprovision',
      tiers: [
        { level: 1, name: 'Base', features: [], limits: { accounts: 2 } },
      ],
    })
    expect(manifest).not.toHaveProperty('tenantCount')
  })
})
