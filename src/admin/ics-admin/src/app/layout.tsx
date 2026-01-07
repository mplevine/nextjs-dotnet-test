import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "ICS Admin Interface",
  description: "ICS admin portal (Entra ID + Next.js static export)",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
