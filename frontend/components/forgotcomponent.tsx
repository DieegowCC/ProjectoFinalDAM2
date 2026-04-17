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

export function ForgotComponent({
  className,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div className={cn("flex flex-col gap-6", className)} {...props}>
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-white">
            Recupera tu contraseña
          </CardTitle>
          <CardDescription className="text-gray-400">
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="password" className="text-gray-300">
                Nueva contraseña
              </Label>
              <Input
                id="NewPassword"
                type="password"
                required
                className="bg-gray-700 border-gray-600 text-white placeholder:text-gray-500 focus:border-gray-400 focus:ring-gray-400"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="password" className="text-gray-300">
                Repetir Contraseña
              </Label>
              <Input
                id="NewPassword2"
                type="password"
                required
                className="bg-gray-700 border-gray-600 text-white focus:border-gray-400 focus:ring-gray-400"
              />
            </div>
            <Button
              type="submit"
              className="w-full bg-gray-600 text-white hover:bg-gray-500 border-0 transition-colors">

              Cambiar contraseña
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}