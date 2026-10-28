import { GalleryVerticalEnd } from "lucide-react"
import { redirect, redirectDocument } from "react-router"
import { buttonVariants } from "@workspace/ui/components/button"
import { LoginForm } from "@/components/login-form"
import { identityServerUrl } from "@/lib/identity"
import type { Route } from "./+types/login";

type LoginContext = { clientId: string; clientName: string | null; loginHint: string | null }
type ProblemDetails = { title?: string; errors?: Record<string, string[]> }

// IdentityServer sends the user here with ?returnUrl=<its /connect/authorize/callback URL>.
// The account API is called from the browser so IdentityServer's session cookie is stored by the browser.
export async function clientLoader({ request }: Route.ClientLoaderArgs) {
  const returnUrl = new URL(request.url).searchParams.get("returnUrl")
  if (!returnUrl) {
    // no authorization request in progress: start one through the SPA server
    throw redirectDocument("/auth/login")
  }

  const response = await fetch(
    `${identityServerUrl}/api/account/login-context?returnUrl=${encodeURIComponent(returnUrl)}`
  )
  if (!response.ok) {
    return { returnUrl, loginHint: null, error: "This sign-in request is invalid or has expired." }
  }

  const context = (await response.json()) as LoginContext
  return { returnUrl, loginHint: context.loginHint, error: null }
}
clientLoader.hydrate = true as const

export async function clientAction({ request }: Route.ClientActionArgs) {
  const form = await request.formData()

  const response = await fetch(`${identityServerUrl}/api/account/login`, {
    method: "POST",
    // send and accept IdentityServer cookies on this cross-origin request
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      username: form.get("username"),
      password: form.get("password"),
      returnUrl: form.get("returnUrl"),
    }),
  })

  if (response.ok) {
    const { redirectUrl } = (await response.json()) as { redirectUrl: string }
    // full page navigation back to /connect/authorize/callback, which now sees the session cookie
    return redirect(redirectUrl)
  }

  const problem = (await response.json()) as ProblemDetails
  return {
    formError: problem.title,
    fieldErrors: {
      username: problem.errors?.Username?.[0],
      password: problem.errors?.Password?.[0],
    },
  }
}

function LoginLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-6 bg-muted p-6 md:p-10">
      <div className="flex w-full max-w-sm flex-col gap-6">
        <a href="#" className="flex items-center gap-2 self-center font-medium">
          <div className="flex size-6 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <GalleryVerticalEnd className="size-4" />
          </div>
          Acme Inc.
        </a>
        {children}
      </div>
    </div>
  )
}

export function HydrateFallback() {
  return (
    <LoginLayout>
      <p className="text-center text-sm text-muted-foreground">Loading…</p>
    </LoginLayout>
  )
}

export default function Login({ loaderData }: Route.ComponentProps) {
  return (
    <LoginLayout>
      {loaderData.error ? (
        <div className="flex flex-col items-center gap-4 text-center text-sm">
          <p>{loaderData.error}</p>
          <a href="/auth/login" className={buttonVariants()}>
            Start again
          </a>
        </div>
      ) : (
        <LoginForm returnUrl={loaderData.returnUrl} loginHint={loaderData.loginHint} />
      )}
    </LoginLayout>
  )
}
