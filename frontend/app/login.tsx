import { LoginForm } from "@/components/login-form"

export default function Page() {
  return (
    <div
      className="flex min-h-svh w-full items-center justify-center bg-gray-950 p-6 md:p-10"
      style={{ fontFamily: "Verdana, Geneva, Tahoma, sans-serif" }}
    >
      <div className="w-full max-w-sm">
        <LoginForm />
      </div>
    </div>
  )
}
