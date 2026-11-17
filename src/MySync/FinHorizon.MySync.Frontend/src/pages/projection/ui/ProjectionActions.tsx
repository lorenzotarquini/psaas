import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { projectionQueryKey, resetProjection, saveProjection, type ProjectionParameters } from '@/entities/projection'

interface ProjectionActionsProps {
  /** The valid parameters to save; null while a field has an error. */
  parameters: ProjectionParameters | null
  /** After "Riparti dai miei dati": the page goes back to the suggested values. */
  onReset: () => void
}

/** UC14 step 10 and 10b: save the projection, or forget it and start again from the suggestions. */
export function ProjectionActions({ parameters, onReset }: ProjectionActionsProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [message, setMessage] = useState<'saved' | 'failed' | null>(null)
  const save = useMutation({
    mutationFn: saveProjection,
    onSuccess: async () => {
      setMessage('saved')
      await queryClient.invalidateQueries({ queryKey: projectionQueryKey })
    },
    onError: () => setMessage('failed'),
  })
  const reset = useMutation({
    mutationFn: resetProjection,
    onSuccess: async () => {
      setMessage(null)
      await queryClient.invalidateQueries({ queryKey: projectionQueryKey })
      onReset()
    },
    onError: () => setMessage('failed'),
  })

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap gap-2">
        <button type="button" className="btn btn-primary" disabled={!parameters || save.isPending} onClick={() => parameters && save.mutate(parameters)}>
          {save.isPending && <span className="loading loading-spinner loading-xs" />}
          {t('projection.save')}
        </button>
        <button type="button" className="btn btn-ghost" disabled={reset.isPending} onClick={() => reset.mutate()}>
          {t('projection.reset')}
        </button>
      </div>
      {message && (
        <p role="status" className={`text-sm ${message === 'saved' ? 'text-success' : 'text-error'}`}>
          {t(message === 'saved' ? 'projection.saved' : 'errors.saveFailed')}
        </p>
      )}
    </div>
  )
}
