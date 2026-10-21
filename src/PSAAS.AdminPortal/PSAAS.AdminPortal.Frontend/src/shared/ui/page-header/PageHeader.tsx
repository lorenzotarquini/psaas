import { faArrowLeft } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import type { ReactNode } from 'react'
import { Link } from 'react-router'

interface PageHeaderProps {
  title: string
  back?: { to: string; label: string }
  actions?: ReactNode
}

export function PageHeader({ title, back, actions }: PageHeaderProps) {
  return (
    <div className="flex flex-col gap-2">
      {back && (
        <Link to={back.to} className="link link-hover w-fit">
          <FontAwesomeIcon icon={faArrowLeft} className="mr-2" />
          {back.label}
        </Link>
      )}
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-semibold">{title}</h1>
        {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
      </div>
    </div>
  )
}
