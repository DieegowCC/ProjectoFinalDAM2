/** @type {import('next').NextConfig} */
const nextConfig = {
  async rewrites() {
    return [
      {
        source: "/hub/:path*",
        destination: "http://192.168.111.100:5432/dashboardHub*",
      },
    ]
  },
}

module.exports = nextConfig