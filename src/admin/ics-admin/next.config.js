/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "export",
  basePath: "/ics-admin",
  assetPrefix: "/ics-admin/",
  trailingSlash: true,
  images: {
    unoptimized: true,
  },
  reactStrictMode: true,
};

module.exports = nextConfig;
