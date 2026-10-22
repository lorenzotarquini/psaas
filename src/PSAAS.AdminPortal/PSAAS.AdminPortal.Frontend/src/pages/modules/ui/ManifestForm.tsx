import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useRegisterModule, type ModuleManifest } from '@/entities/catalog'
import { ApiError } from '@/shared/api'
import { ErrorAlert } from '@/shared/ui/error-alert'

const example: ModuleManifest = {
  id: 'warehouse',
  name: 'Magazzino',
  version: '1.4.0',
  icon: 'warehouse',
  status: 'Available',
  requires: [],
  permissions: ['warehouse.stock.read', 'warehouse.stock.edit'],
  provisionUrl: 'https://warehouse.example.com/hooks/provision',
}

// UC15: the manifest is pasted as JSON, as the module team publishes it; the backend validates every field.
export function ManifestForm({ onClose }: { onClose: () => void }) {
  const { t } = useTranslation()
  const register = useRegisterModule()
  const [text, setText] = useState(() => JSON.stringify(example, null, 2))
  const [parseError, setParseError] = useState(false)
  const fieldErrors = register.error instanceof ApiError ? Object.entries(register.error.fieldErrors) : []

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    let manifest: ModuleManifest
    try {
      manifest = JSON.parse(text) as ModuleManifest
    } catch {
      return setParseError(true)
    }
    setParseError(false)
    try {
      await register.mutateAsync(manifest)
      onClose()
    } catch {
      // Shown below from register.error.
    }
  }

  return (
    <form onSubmit={onSubmit} className="panel flex flex-col gap-3 px-5 py-4">
      <div>
        <h2 className="panel-title">{t('catalog.registerModule')}</h2>
        <p className="text-base-content/60">{t('catalog.manifestHint')}</p>
      </div>
      <textarea
        className={`textarea data h-64 w-full text-[12.5px] ${parseError || fieldErrors.length ? 'textarea-error' : ''}`}
        aria-label={t('catalog.manifest')}
        spellCheck={false}
        value={text}
        onChange={(e) => setText(e.target.value)}
      />
      {parseError && <p className="text-[12px] text-error">{t('catalog.invalidJson')}</p>}
      {fieldErrors.length > 0 && (
        <ul className="text-[12px] text-error" role="alert">
          {fieldErrors.map(([field, codes]) => (
            <li key={field}>
              <span className="data">{field}</span>: {t(`validation.${codes[0]}`, { defaultValue: codes[0] })}
            </li>
          ))}
        </ul>
      )}
      {register.error && !(register.error instanceof ApiError && register.error.status === 400) && <ErrorAlert error={register.error} action="save" />}
      <div className="flex justify-end gap-2">
        <button type="button" className="btn btn-ghost btn-sm" onClick={onClose}>
          {t('unsaved.discard')}
        </button>
        <button type="submit" className="btn btn-primary btn-sm" disabled={register.isPending}>
          {register.isPending && <span className="loading loading-spinner loading-xs" />}
          {t('catalog.register')}
        </button>
      </div>
    </form>
  )
}
