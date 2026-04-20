'use client'
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import Link from "next/link"
import { useState } from "react"
import { useRouter } from "next/navigation"


export function LoginForm({
  className,
  ...props
}: React.ComponentProps<"div">) {
  const router = useRouter()

  const [user, setUser] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    if (user === 'admin' && password === 'admin') {
      localStorage.setItem('auth', 'true')
      router.push('/dashboard')
    } else {
      setError('Credenciales incorrectas')
    }
  }
  return (
    <div className={cn("flex flex-col gap-6", className)} {...props}>
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-white">
            Login to your account
          </CardTitle>
          <CardDescription className="text-gray-400">
            Enter your user below to login to your account
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="user" className="text-gray-300">
                User
              </Label>
              <Input
                id="user"
                type="user"
                value={user}
                onChange={(e) => setUser(e.target.value)}
                required
                className="bg-gray-700 border-gray-600 text-white placeholder:text-gray-500 focus:border-gray-400 focus:ring-gray-400"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="password" className="text-gray-300">
                Password
              </Label>
              <Input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className="bg-gray-700 border-gray-600 text-white focus:border-gray-400 focus:ring-gray-400"
              />
              
              <Link href= "/forgot"
                className="text-sm text-gray-400 underline-offset-4 hover:underline hover:text-white transition-colors self-end">
                Forgot your password?
              </Link> 
            </div>
            {error && <p className="text-red-400 text-sm">{error}</p>}
            <Button
              type="submit"
              className="w-full bg-gray-600 text-white hover:bg-gray-500 border-0 transition-colors">
              Login
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}