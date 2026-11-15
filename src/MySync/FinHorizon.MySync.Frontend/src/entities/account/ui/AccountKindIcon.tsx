import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBuildingColumns, faCoins, faGem, faMobileScreenButton, faWallet } from '@fortawesome/free-solid-svg-icons'
import type { AccountKind } from '../api/accounts'

const icons = {
  CurrentAccount: faBuildingColumns,
  VirtualBank: faMobileScreenButton,
  Cash: faWallet,
  Valuables: faGem,
  Other: faCoins,
} as const

export function AccountKindIcon({ kind }: { kind: AccountKind }) {
  return (
    <span className="grid size-10 shrink-0 place-items-center rounded-full bg-primary/10 text-primary" aria-hidden>
      <FontAwesomeIcon icon={icons[kind]} />
    </span>
  )
}
