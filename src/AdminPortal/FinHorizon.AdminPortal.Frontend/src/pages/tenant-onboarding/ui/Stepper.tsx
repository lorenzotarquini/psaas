import { faCheck } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { stepIndex, steps, type Step } from '../model/onboarding'

// Left column of mock 1e: "PASSO 1 DI 5" and the five steps; once a draft exists every step can be reopened.
export function Stepper({ current, linkTo }: { current: Step; linkTo?: (step: Step) => string }) {
  const { t } = useTranslation()
  const currentIndex = stepIndex(current)

  return (
    <nav aria-label={t('onboarding.stepsLabel')} className="flex flex-col gap-1">
      <p className="eyebrow px-2 pb-2 text-base-content/55">{t('onboarding.stepOf', { step: currentIndex + 1, total: steps.length })}</p>
      {steps.map((step, i) => {
        const isCurrent = step === current
        const done = i < currentIndex
        const content = (
          <>
            <span
              className={`grid size-6 shrink-0 place-items-center rounded-full border font-mono text-[11px] font-semibold ${
                isCurrent ? 'border-primary bg-primary text-primary-content' : done ? 'border-primary text-primary' : 'border-base-300 text-base-content/50'
              }`}
            >
              {done ? <FontAwesomeIcon icon={faCheck} className="text-[10px]" /> : i + 1}
            </span>
            <span>
              <span className={`block ${isCurrent ? 'font-semibold' : 'font-medium text-base-content/75'}`}>{t(`onboarding.steps.${step}.title`)}</span>
              <span className="block text-[12px] text-base-content/55">{t(`onboarding.steps.${step}.hint`)}</span>
            </span>
          </>
        )
        const className = `flex items-start gap-2.5 rounded-box px-2 py-2 ${isCurrent ? 'bg-primary/8' : linkTo ? 'hover:bg-base-200' : ''}`
        return linkTo && !isCurrent ? (
          <Link key={step} to={linkTo(step)} className={className}>
            {content}
          </Link>
        ) : (
          <div key={step} className={className} aria-current={isCurrent ? 'step' : undefined}>
            {content}
          </div>
        )
      })}
    </nav>
  )
}
