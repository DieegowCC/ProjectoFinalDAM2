import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export default function ForgotPage() {
  return (
    <div className="flex min-h-svh w-full items-center justify-center bg-background p-6 md:p-10">
      <div className="w-full max-w-sm flex flex-col gap-6">
        <div className="flex flex-col items-center gap-2 text-center">
          <div className="flex aspect-square size-10 items-center justify-center rounded-md bg-[var(--accent-active)] text-black font-mono font-semibold">
            S
          </div>
          <span className="text-sm text-muted-foreground font-mono">
            Sett. Vigilance
          </span>
        </div>

        <Card className="bg-card border border-border">
          <CardHeader>
            <CardTitle className="text-foreground text-lg">
              Recupera tu contraseña
            </CardTitle>
            <CardDescription className="text-muted-foreground">
              Introduce una contraseña nueva para tu cuenta.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form className="flex flex-col gap-4">
              <div className="flex flex-col gap-2">
                <Label htmlFor="NewPassword" className="text-foreground text-sm">
                  Nueva contraseña
                </Label>
                <Input
                  id="NewPassword"
                  type="password"
                  required
                  className="bg-muted border-border text-foreground"
                />
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="NewPassword2" className="text-foreground text-sm">
                  Repetir contraseña
                </Label>
                <Input
                  id="NewPassword2"
                  type="password"
                  required
                  className="bg-muted border-border text-foreground"
                />
              </div>
              <Button
                type="submit"
                className="w-full bg-foreground text-background hover:bg-foreground/90"
              >
                Cambiar contraseña
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
