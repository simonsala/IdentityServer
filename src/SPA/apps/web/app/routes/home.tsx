import { buttonVariants } from "@workspace/ui/components/button"
import { getUser } from "@/lib/auth.server"
import type { Route } from "./+types/home"

export async function loader({ request }: Route.LoaderArgs) {
  return { user: await getUser(request) }
}

export default function Home({ loaderData: { user } }: Route.ComponentProps) {
  return (
    <div className="flex min-h-svh p-6">
      <div className="flex max-w-md min-w-0 flex-col gap-4 text-sm leading-loose">
        {user ? (
          <div>
            <h1 className="font-medium">Welcome, {user.givenName ?? user.name}</h1>
            <p>Subject: {user.sub}</p>
            <p>Roles: {user.roles.length > 0 ? user.roles.join(", ") : "none"}</p>
          </div>
        ) : (
          <div>
            <h1 className="font-medium">You are not signed in</h1>
            {/* plain link: /auth/login is a server resource route that redirects to IdentityServer */}
            <a href="/auth/login" className={buttonVariants({ className: "mt-2" })}>
              Sign in
            </a>
          </div>
        )}
      </div>
    </div>
  )
}
