import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "export",
  basePath: "/ics-admin",
  assetPrefix: "/ics-admin/",
  trailingSlash: true,
  images: {
    unoptimized: true,
  },
  reactStrictMode: true,
};

export default nextConfig;
