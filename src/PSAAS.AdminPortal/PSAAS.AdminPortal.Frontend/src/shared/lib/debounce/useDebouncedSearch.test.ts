// @vitest-environment jsdom
import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useDebouncedSearch } from './useDebouncedSearch'

describe('useDebouncedSearch', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  const setup = (initial: string) => {
    const commit = vi.fn()
    const hook = renderHook(({ committed }) => useDebouncedSearch(committed, commit), {
      initialProps: { committed: initial },
    })
    return { commit, hook }
  }

  it('commits the trimmed text only after the pause', () => {
    const { commit, hook } = setup('')
    act(() => hook.result.current[1]('alf'))
    act(() => hook.result.current[1]('alfa '))
    act(() => vi.advanceTimersByTime(299))
    expect(commit).not.toHaveBeenCalled()
    act(() => vi.advanceTimersByTime(1))
    expect(commit).toHaveBeenCalledExactlyOnceWith('alfa')
  })

  it('keeps the trailing space being typed when its own value is committed', () => {
    const { hook } = setup('')
    act(() => hook.result.current[1]('alfa '))
    hook.rerender({ committed: 'alfa' })
    expect(hook.result.current[0]).toBe('alfa ')
  })

  it('shows a value committed from outside, like the back button', () => {
    const { commit, hook } = setup('alfa')
    hook.rerender({ committed: 'beta' })
    expect(hook.result.current[0]).toBe('beta')
    act(() => vi.advanceTimersByTime(1000))
    expect(commit).not.toHaveBeenCalled()
  })
})
