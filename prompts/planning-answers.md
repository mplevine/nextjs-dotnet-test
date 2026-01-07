# Here are my answers to your Key Questions

1. Use Next.js static export + IIS static hosting.
2. This is strictly an authenticated admin portal. Use CSR/SSG.
3. Use single sign-on with redirects.
4. The ICS API is only called by the ICS Admin Interface.
5. Roles will be assigned to Users **and** to Groups.
6. There will be just one Entra ID tenant.
7. The API will be reachable at a stable URL. The exact deployed base URL for the IIS server will be: `https://nam-pdaparch01.americas.global-net.com`.
8. Make this a "pure example" (in-memory data, no database).
9. Application logs **and** audit events, like "who accessed what and when".
10. Use Entra ID **only** - no IIS/Windows Integrated Auth.
