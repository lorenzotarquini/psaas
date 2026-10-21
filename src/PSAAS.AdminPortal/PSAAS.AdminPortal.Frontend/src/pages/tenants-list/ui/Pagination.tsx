import { faChevronLeft, faChevronRight } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { pageSizes } from '../model/searchParams'

interface PaginationProps {
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
}

export function Pagination({ page, pageSize, totalCount, onPageChange, onPageSizeChange }: PaginationProps) {
  const { t } = useTranslation()
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  return (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <label className="flex items-center gap-2 text-sm">
        {t('pagination.pageSize')}
        <select
          className="select select-sm w-auto"
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
        >
          {pageSizes.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </label>
      <div className="join">
        <button
          type="button"
          className="join-item btn btn-sm"
          aria-label={t('pagination.previous')}
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          <FontAwesomeIcon icon={faChevronLeft} />
        </button>
        <span className="join-item btn btn-sm pointer-events-none">
          {t('pagination.pageOf', { page, totalPages })}
        </span>
        <button
          type="button"
          className="join-item btn btn-sm"
          aria-label={t('pagination.next')}
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          <FontAwesomeIcon icon={faChevronRight} />
        </button>
      </div>
    </div>
  )
}
