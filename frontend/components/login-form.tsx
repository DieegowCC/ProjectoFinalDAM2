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

export function LoginForm({
  className,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div className={cn("flex flex-col gap-6", className)} {...props}>
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-white">
            Login to your account
          </CardTitle>
          <CardDescription className="text-gray-400">
            Enter your email below to login to your account
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="email" className="text-gray-300">
                Email
              </Label>
              <Input
                id="email"
                type="email"
                placeholder="m@example.com"
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
                required
                className="bg-gray-700 border-gray-600 text-white focus:border-gray-400 focus:ring-gray-400"
              />
              <Link
                href="#"
                className="text-sm text-gray-400 underline-offset-4 hover:underline hover:text-white transition-colors self-end"
              >
                Forgot your password?
              </Link>
            </div>
            <Button
              type="submit"
              className="w-full bg-gray-600 text-white hover:bg-gray-500 border-0 transition-colors"
            >
              Login
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}