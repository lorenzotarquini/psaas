// @vitest-environment jsdom
import { zodResolver } from '@hookform/resolvers/zod'
import { act, renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { FormProvider, useForm, useFormContext } from 'react-hook-form'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { validateTenantField } from '@/entities/tenant'
import { emptyRegistry, registrySchema, type RegistryValues } from './registry'
import { useUniqueFieldCheck } from './useUniqueFieldCheck'

vi.mock('@/entities/tenant', () => ({ validateTenantField: vi.fn() }))
const validate = vi.mocked(validateTenantField)

function setup(values: Partial<RegistryValues>, excludeTenantId?: string) {
  function FormWrapper({ children }: { children: ReactNode }) {
    const form = useForm({ resolver: zodResolver(registrySchema), defaultValues: { ...emptyRegistry, ...values } })
    return <FormProvider {...form}>{children}</FormProvider>
  }
  return renderHook(() => ({ check: useUniqueFieldCheck(excludeTenantId), form: useFormContext<RegistryValues>() }), {
    wrapper: FormWrapper,
  }).result
}

const blur = (result: ReturnType<typeof setup>, field: 'companyName' | 'completeVatNumber' | 'certifiedEmail') =>
  act(async () => {
    await result.current.check.onBlur(field).onBlur?.({} as never)
  })

describe('useUniqueFieldCheck', () => {
  beforeEach(() => validate.mockReset())

  // UC11-S2: the backend code lands on the field.
  it('shows NotUnique from the backend on the field', async () => {
    validate.mockResolvedValue({ errorCode: 'NotUnique', conflictingTenant: { id: 't-1', companyName: 'Alfa Srl' } })
    const result = setup({ companyName: ' ALFA s.r.l. ' })
    await blur(result, 'companyName')
    expect(validate).toHaveBeenCalledWith('companyName', 'ALFA s.r.l.', undefined)
    expect(result.current.form.getFieldState('companyName').error?.message).toBe('NotUnique')
    // UC11-R4: the tenant already using the value, to link to it.
    expect(result.current.check.statusOf('companyName', 'ALFA s.r.l.')).toEqual({
      verified: false,
      conflictingTenant: { id: 't-1', companyName: 'Alfa Srl' },
    })
  })

  // UC11-S3: the tenant being edited is passed along.
  it('excludes the tenant being edited', async () => {
    validate.mockResolvedValue({ errorCode: null, conflictingTenant: null })
    const result = setup({ certifiedEmail: 'alfa@pec.it' }, 'tenant-1')
    await blur(result, 'certifiedEmail')
    expect(validate).toHaveBeenCalledWith('certifiedEmail', 'alfa@pec.it', 'tenant-1')
    expect(result.current.form.getFieldState('certifiedEmail').error).toBeUndefined()
    // Mock 1a: confirmed free while the value stays the same.
    expect(result.current.check.statusOf('certifiedEmail', 'alfa@pec.it').verified).toBe(true)
    expect(result.current.check.statusOf('certifiedEmail', 'beta@pec.it').verified).toBe(false)
  })

  it('does not call the backend for an empty field or a format the client already rejects', async () => {
    const result = setup({ completeVatNumber: '12345' })
    await blur(result, 'companyName')
    await blur(result, 'completeVatNumber')
    expect(validate).not.toHaveBeenCalled()
    expect(result.current.form.getFieldState('completeVatNumber').error?.message).toBe('InvalidFormat')
  })

  it('ignores an answer for a value the user has changed in the meantime', async () => {
    const result = setup({ companyName: 'Alfa Srl' })
    validate.mockImplementation(async () => {
      result.current.form.setValue('companyName', 'Beta Snc')
      return { errorCode: 'NotUnique', conflictingTenant: null }
    })
    await blur(result, 'companyName')
    expect(result.current.form.getFieldState('companyName').error).toBeUndefined()
  })
})
