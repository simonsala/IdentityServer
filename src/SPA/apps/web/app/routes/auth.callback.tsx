import { buttonVariants } from "@workspace/ui/components/button"
import { completeLogin } from "@/lib/auth.server"
import type { Route } from "./+types/auth.callback"

// IdentityServer redirects here with ?code&state; on success the loader redirects, otherwise the error is rendered.
export function loader({ request }: Route.LoaderArgs) {
  return completeLogin(request)
}

export default function AuthCallback({ loaderData }: Route.ComponentProps) {
  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-4 p-6 text-sm">
      <h1 className="font-medium">Sign-in failed</h1>
      <p>{loaderData.error}</p>
      <a href="/auth/login" className={buttonVariants()}>
        Try again
      </a>
    </div>
  )
}
