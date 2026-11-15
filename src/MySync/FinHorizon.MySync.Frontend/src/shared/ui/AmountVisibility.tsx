import { createContext, useContext, type ReactNode } from 'react'

const AmountsHidden = createContext(false)

/** Hides every amount below it (UC12 "eye"): handy when someone else is looking at the screen. */
export function AmountVisibility({ hidden, children }: { hidden: boolean; children: ReactNode }) {
  return <AmountsHidden.Provider value={hidden}>{children}</AmountsHidden.Provider>
}

export const useAmountsHidden = () => useContext(AmountsHidden)

/** What a hidden amount shows instead of the figure. */
export const hiddenAmount = '••••'
