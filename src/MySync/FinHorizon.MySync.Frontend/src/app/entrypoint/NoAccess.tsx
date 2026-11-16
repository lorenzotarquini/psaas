import { session } from '@/shared/auth'

// UC32 (Admin Portal): signed in but without the MySync module, the user does not enter the app.
export function NoAccess() {
  return (
    <div className="m-8 flex max-w-xl flex-col gap-4">
      <div role="alert" className="alert alert-warning">
        Non hai accesso a MySync: chiedi all'amministratore di assegnarti il modulo. / You have no access to MySync: ask the
        administrator to give you the module.
      </div>
      <button type="button" className="btn btn-sm w-fit" onClick={() => void session.logout()}>
        Esci / Sign out
      </button>
    </div>
  )
}
