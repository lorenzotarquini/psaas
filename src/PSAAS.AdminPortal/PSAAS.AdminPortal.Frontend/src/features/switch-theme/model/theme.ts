// Must match the daisyUI themes defined in app/styles/index.css (psaas-dark is --prefersdark).
export const themes = { light: 'psaas', dark: 'psaas-dark' } as const
export type Mode = keyof typeof themes

export const storageKey = 'psaas-theme'

/** Applies the theme chosen in a previous visit; without one, daisyUI follows the system preference. */
export function applyStoredTheme() {
  try {
    const mode = localStorage.getItem(storageKey) as Mode | null
    if (mode && mode in themes) document.documentElement.dataset.theme = themes[mode]
  } catch {
    // Storage blocked (private mode): the system preference applies.
  }
}
