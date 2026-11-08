import { faChevronLeft, faChevronRight } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'

interface PaginationProps {
  page: number
  pageSize: number
  totalCount: number
  pageSizes: readonly number[]
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
}

// Footer of mock 1c: rows per page on the left, "1–12 of 38" in mono and arrows on the right.
export function Pagination({ page, pageSize, totalCount, pageSizes, onPageChange, onPageSizeChange }: PaginationProps) {
  const { t } = useTranslation()
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const first = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const last = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-base-300 px-3 py-2">
      <label className="flex items-center gap-2 text-base-content/70">
        {t('pagination.pageSize')}
        <select
          className="select select-ghost select-xs w-auto font-semibold"
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
      <div className="flex items-center gap-2">
        <span className="data text-base-content/70">{t('pagination.range', { first, last, total: totalCount })}</span>
        <button
          type="button"
          className="btn btn-square btn-xs"
          aria-label={t('pagination.previous')}
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          <FontAwesomeIcon icon={faChevronLeft} />
        </button>
        <button
          type="button"
          className="btn btn-square btn-xs"
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
