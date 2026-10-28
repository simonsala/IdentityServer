import { beginLogin } from "@/lib/auth.server"
import type { Route } from "./+types/auth.login"

// Resource route: starts the authorization code + PKCE flow by redirecting to IdentityServer's /connect/authorize.
export function loader({ request }: Route.LoaderArgs) {
  return beginLogin(request)
}
