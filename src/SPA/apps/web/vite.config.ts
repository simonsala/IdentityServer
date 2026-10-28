import { reactRouter } from "@react-router/dev/vite"
import tailwindcss from "@tailwindcss/vite"
import { defineConfig } from "vite"
import { spawnSync } from "node:child_process"
import fs from "node:fs"
import path from "node:path"
import tls from "node:tls"

const baseFolder = process.env.APPDATA
  ? path.join(process.env.APPDATA, "ASP.NET", "https")
  : path.join(process.env.HOME ?? "", ".aspnet", "https")
const certFilePath = path.join(baseFolder, "spa-web.pem")
const keyFilePath = path.join(baseFolder, "spa-web.key")

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
  fs.mkdirSync(baseFolder, { recursive: true })
  const result = spawnSync(
    "dotnet",
    ["dev-certs", "https", "--export-path", certFilePath, "--format", "Pem", "--no-password"],
    { stdio: "inherit" }
  )
  if (result.status !== 0) {
    throw new Error("Could not create HTTPS certificate via dotnet dev-certs.")
  }
}

// Server-side OIDC calls (discovery, token, userinfo) go to IdentityServer, which uses the same
// ASP.NET Core dev certificate. Node does not read the Windows/macOS trust store, so trust it here.
tls.setDefaultCACertificates([
  ...tls.getCACertificates("default"),
  fs.readFileSync(certFilePath, "utf8"),
])

export default defineConfig({
  resolve: { tsconfigPaths: true },
  plugins: [tailwindcss(), reactRouter()],
  server: {
    // must match the spa client's RedirectUris in IdentityServer
    port: 5173,
    strictPort: true,
    https: {
      cert: fs.readFileSync(certFilePath),
      key: fs.readFileSync(keyFilePath),
    },
  },
})
